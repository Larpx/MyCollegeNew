using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Constants;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Attendance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using SqlSugar;

namespace Campus.Attendance.Services.Attendance;

/// <summary>
/// 考勤会话与签到服务实现，封装会话生命周期、二维码生成、学生签到、点名等业务逻辑
/// </summary>
public class AttendanceService : IAttendanceService
{
    /// <summary>会话默认持续时长（分钟），未指定 EndTime 时使用</summary>
    private const int DefaultSessionDurationMinutes = 30;

    /// <summary>二维码图片每像素模块数（影响图片清晰度与体积）</summary>
    private const int QrPixelsPerModule = 20;

    private readonly IDbContext _dbContext;
    private readonly IOptions<JwtConfig> _jwtConfig;
    private readonly ILogger<AttendanceService> _logger;

    /// <summary>随机点名历史记录：sessionId -> 最近被点名学生学号列表（按时间顺序）</summary>
    private static readonly ConcurrentDictionary<long, List<string>> _randomPickHistory = new();

    /// <summary>
    /// 构造函数，注入数据库上下文、JWT 配置与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="jwtConfig">JWT 配置（用于生成与校验二维码短期令牌）</param>
    /// <param name="logger">日志器</param>
    public AttendanceService(IDbContext dbContext, IOptions<JwtConfig> jwtConfig, ILogger<AttendanceService> logger)
    {
        _dbContext = dbContext;
        _jwtConfig = jwtConfig;
        _logger = logger;
    }

    /// <summary>
    /// 创建考勤会话，状态置为 Active，并生成初始 QrToken
    /// </summary>
    public async Task<SessionResponseDto> CreateSessionAsync(SessionCreateDto dto, string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 校验课程存在且属于该教师
        var course = await db.Queryable<Course>().FirstAsync(c => c.Id == dto.CourseId && !c.IsDeleted, cancellationToken);
        if (course is null)
        {
            throw new BusinessException($"课程 {dto.CourseId} 不存在", 404);
        }

        if (course.TeacherId != teacherId)
        {
            throw new BusinessException("仅可为自己负责的课程创建考勤会话", 403);
        }

        // 校验班级存在
        var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == dto.ClassId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            throw new BusinessException($"班级 {dto.ClassId} 不存在", 404);
        }

        var startTime = dto.StartTime;
        var endTime = dto.EndTime ?? startTime.AddMinutes(DefaultSessionDurationMinutes);
        if (endTime <= startTime)
        {
            throw new BusinessException("签到结束时间必须晚于开始时间", 400);
        }

        var session = new AttendanceSession
        {
            CourseId = dto.CourseId,
            ClassId = dto.ClassId,
            TeacherId = teacherId,
            ScheduleId = dto.ScheduleId,
            StartTime = startTime,
            EndTime = endTime,
            Status = SessionStatus.Active,
            QrToken = null,
            CreateTime = DateTime.UtcNow
        };

        var id = await db.Insertable(session).ExecuteReturnIdentityAsync(cancellationToken);

        // 生成初始 QrToken（需使用会话 Id 作为 Claim，因此先插入再更新）
        var initialToken = GenerateQrToken(id, DateTime.UtcNow);
        session.Id = id;
        session.QrToken = initialToken;
        await db.Updateable(session)
            .UpdateColumns(it => new { it.QrToken })
            .ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("教师 {TeacherId} 创建考勤会话 {SessionId}", teacherId, id);

        return (await GetSessionByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 查询会话详情（含课程名、班级名、教师名）
    /// </summary>
    public async Task<SessionResponseDto?> GetSessionByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => s.Id == id && !s.IsDeleted)
            .Select((s, c, cls, t) => new SessionResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                ScheduleId = s.ScheduleId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status,
                QrToken = s.QrToken,
                CreateTime = s.CreateTime
            })
            .FirstAsync();

        return dto;
    }

    /// <summary>
    /// 查询教师进行中的会话
    /// </summary>
    public async Task<List<SessionResponseDto>> GetActiveSessionsByTeacherAsync(string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var rows = await db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => s.TeacherId == teacherId && s.Status == SessionStatus.Active && !s.IsDeleted)
            .Select((s, c, cls, t) => new SessionResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                ScheduleId = s.ScheduleId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status,
                QrToken = s.QrToken,
                CreateTime = s.CreateTime
            })
            .OrderBy(it => it.StartTime, OrderByType.Desc)
            .ToListAsync();

        return rows;
    }

    /// <summary>
    /// 分页查询教师历史会话，支持时间区间过滤
    /// </summary>
    public async Task<PagedResult<SessionResponseDto>> GetSessionsByTeacherAsync(
        int pageIndex, int pageSize, string teacherId,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var query = db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => s.TeacherId == teacherId && !s.IsDeleted);

        if (startDate.HasValue)
        {
            query = query.Where((s, c, cls, t) => s.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where((s, c, cls, t) => s.StartTime <= endDate.Value);
        }

        var total = await query.CountAsync();
        var rows = await query
            .Select((s, c, cls, t) => new SessionResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                ScheduleId = s.ScheduleId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status,
                QrToken = s.QrToken,
                CreateTime = s.CreateTime
            })
            .OrderBy(it => it.StartTime, OrderByType.Desc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<SessionResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 关闭会话（Status=Closed），并为未签到学生创建缺勤记录
    /// </summary>
    public async Task CloseSessionAsync(long sessionId, string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var session = await GetSessionAndVerifyTeacherAsync(db, sessionId, teacherId, cancellationToken);
        if (session.Status == SessionStatus.Closed)
        {
            throw new BusinessException("会话已关闭，无需重复操作", 400);
        }

        // 查询该班级所有未删除学生
        var students = await db.Queryable<Student>()
            .Where(s => s.ClassId == session.ClassId && !s.IsDeleted)
            .ToListAsync();

        // 查询已存在签到记录的学生学号集合
        var existingStudentIds = await db.Queryable<AttendanceRecord>()
            .Where(r => r.SessionId == sessionId && !r.IsDeleted)
            .Select(r => r.StudentId)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingStudentIds);

        // 为未签到学生创建缺勤记录
        var absentRecords = students
            .Where(s => !existingSet.Contains(s.Id))
            .Select(s => new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = s.Id,
                StudentName = s.Name,
                Status = AttendanceStatus.Absent,
                CheckInTime = null,
                Remark = "会话关闭自动标记缺勤",
                CreateTime = DateTime.UtcNow
            })
            .ToList();

        session.Status = SessionStatus.Closed;
        session.UpdateTime = DateTime.UtcNow;

        // 事务保证数据一致性
        await db.Ado.UseTranAsync(async () =>
        {
            await db.Updateable(session)
                .UpdateColumns(it => new { it.Status, it.UpdateTime })
                .ExecuteCommandAsync(cancellationToken);
            if (absentRecords.Count > 0)
            {
                await db.Insertable(absentRecords).ExecuteCommandAsync(cancellationToken);
            }
        });

        _logger.LogInformation("关闭会话 {SessionId}，自动生成 {AbsentCount} 条缺勤记录", sessionId, absentRecords.Count);
    }

    /// <summary>
    /// 查询会话的所有签到记录
    /// </summary>
    public async Task<List<AttendanceRecordResponseDto>> GetSessionRecordsAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var rows = await db.Queryable<AttendanceRecord>()
            .Where(r => r.SessionId == sessionId && !r.IsDeleted)
            .OrderBy(r => r.StudentId)
            .Select(r => new AttendanceRecordResponseDto
            {
                Id = r.Id,
                SessionId = r.SessionId,
                StudentId = r.StudentId,
                StudentName = r.StudentName,
                Status = r.Status,
                CheckInTime = r.CheckInTime,
                Remark = r.Remark
            })
            .ToListAsync();

        return rows;
    }

    /// <summary>
    /// 生成短期二维码（30 秒过期），返回 Base64 图片与 token
    /// </summary>
    public async Task<QrCodeResult> GenerateQrCodeAsync(long sessionId, string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var session = await GetSessionAndVerifyTeacherAsync(db, sessionId, teacherId, cancellationToken);
        if (session.Status != SessionStatus.Active)
        {
            throw new BusinessException("会话已关闭，无法生成二维码", 400);
        }

        // 生成短期 JWT token（30 秒过期），以当前时间为签发起点
        var token = GenerateQrToken(sessionId, DateTime.UtcNow);
        session.QrToken = token;
        session.UpdateTime = DateTime.UtcNow;
        await db.Updateable(session)
            .UpdateColumns(it => new { it.QrToken, it.UpdateTime })
            .ExecuteCommandAsync(cancellationToken);

        // 二维码内容为签到 URL
        var qrContent = $"/api/sessions/{sessionId}/checkin?token={token}";
        var base64Image = GenerateQrBase64Image(qrContent);

        _logger.LogInformation("教师 {TeacherId} 为会话 {SessionId} 生成二维码", teacherId, sessionId);

        return new QrCodeResult
        {
            Token = token,
            Base64Image = base64Image,
            ExpireSeconds = AttendanceConstants.QrTokenExpireSeconds,
            GenerateTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 学生扫码签到，根据签到时间判定 Present/Late/Absent
    /// </summary>
    public async Task<CheckInResult> CheckInAsync(long sessionId, string token, string studentId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 1. 校验 token 有效性（未过期、SessionId 匹配）
        var tokenSessionId = ValidateQrToken(token, sessionId);

        // 2. 校验会话为 Active
        var session = await db.Queryable<AttendanceSession>()
            .FirstAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);
        if (session is null)
        {
            throw new BusinessException("考勤会话不存在", 404);
        }

        if (session.Status != SessionStatus.Active)
        {
            throw new BusinessException("会话已关闭，无法签到", 400);
        }

        // 3. 校验学生属于该会话的班级
        var student = await db.Queryable<Student>()
            .FirstAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);
        if (student is null)
        {
            throw new BusinessException("学生不存在", 404);
        }

        if (student.ClassId != session.ClassId)
        {
            throw new BusinessException("学生不属于该考勤班级", 403);
        }

        // 4. 检查是否已签到（避免重复签到）
        var exists = await db.Queryable<AttendanceRecord>()
            .AnyAsync(r => r.SessionId == sessionId && r.StudentId == studentId && !r.IsDeleted, cancellationToken);
        if (exists)
        {
            throw new BusinessException("已签到，请勿重复签到", 400);
        }

        // 5. 判定签到状态
        var checkInTime = DateTime.UtcNow;
        var (status, message) = DetermineCheckInStatus(session.StartTime, checkInTime);

        // 6. 写入签到记录
        var record = new AttendanceRecord
        {
            SessionId = sessionId,
            StudentId = studentId,
            StudentName = student.Name,
            Status = status,
            CheckInTime = checkInTime,
            Remark = null,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("学生 {StudentId} 在会话 {SessionId} 签到，状态 {Status}", studentId, sessionId, status);

        return new CheckInResult
        {
            Status = status,
            CheckInTime = checkInTime,
            Message = message
        };
    }

    /// <summary>
    /// 一键点名：将所有未签到学生标记为 Present，批量插入记录
    /// </summary>
    public async Task<int> RollCallAllPresentAsync(long sessionId, string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var session = await GetSessionAndVerifyTeacherAsync(db, sessionId, teacherId, cancellationToken);

        // 查询班级所有未删除学生
        var students = await db.Queryable<Student>()
            .Where(s => s.ClassId == session.ClassId && !s.IsDeleted)
            .ToListAsync();

        // 查询已签到学生学号集合
        var existingStudentIds = await db.Queryable<AttendanceRecord>()
            .Where(r => r.SessionId == sessionId && !r.IsDeleted)
            .Select(r => r.StudentId)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingStudentIds);

        var checkInTime = DateTime.UtcNow;
        var newRecords = students
            .Where(s => !existingSet.Contains(s.Id))
            .Select(s => new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = s.Id,
                StudentName = s.Name,
                Status = AttendanceStatus.Present,
                CheckInTime = checkInTime,
                Remark = "教师一键点名",
                CreateTime = DateTime.UtcNow
            })
            .ToList();

        if (newRecords.Count > 0)
        {
            await db.Insertable(newRecords).ExecuteCommandAsync(cancellationToken);
        }

        _logger.LogInformation("会话 {SessionId} 一键点名，标记 {Count} 名学生为 Present", sessionId, newRecords.Count);
        return newRecords.Count;
    }

    /// <summary>
    /// 修改单条考勤记录状态（校验记录所属会话属于该教师）
    /// </summary>
    public async Task UpdateRecordStatusAsync(long recordId, AttendanceStatus status, string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var record = await db.Queryable<AttendanceRecord>()
            .FirstAsync(r => r.Id == recordId && !r.IsDeleted, cancellationToken);
        if (record is null)
        {
            throw new BusinessException($"考勤记录 {recordId} 不存在", 404);
        }

        // 校验记录所属会话属于该教师
        var session = await db.Queryable<AttendanceSession>()
            .FirstAsync(s => s.Id == record.SessionId && !s.IsDeleted, cancellationToken);
        if (session is null)
        {
            throw new BusinessException("考勤会话不存在", 404);
        }

        if (session.TeacherId != teacherId)
        {
            throw new BusinessException("仅可修改自己发起的考勤记录", 403);
        }

        record.Status = status;
        record.UpdateTime = DateTime.UtcNow;
        await db.Updateable(record)
            .UpdateColumns(it => new { it.Status, it.UpdateTime })
            .ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("教师 {TeacherId} 修改记录 {RecordId} 状态为 {Status}", teacherId, recordId, status);
    }

    /// <summary>
    /// 教师手动补签
    /// </summary>
    public async Task<AttendanceRecordResponseDto> ManualCheckInAsync(long sessionId, string studentId, AttendanceStatus status, string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var session = await GetSessionAndVerifyTeacherAsync(db, sessionId, teacherId, cancellationToken);

        // 校验学生存在且属于该班级
        var student = await db.Queryable<Student>()
            .FirstAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);
        if (student is null)
        {
            throw new BusinessException($"学生 {studentId} 不存在", 404);
        }

        if (student.ClassId != session.ClassId)
        {
            throw new BusinessException("学生不属于该考勤班级", 403);
        }

        // 检查是否已存在记录，若存在则更新，否则插入
        var existing = await db.Queryable<AttendanceRecord>()
            .FirstAsync(r => r.SessionId == sessionId && r.StudentId == studentId && !r.IsDeleted, cancellationToken);

        if (existing is not null)
        {
            existing.Status = status;
            existing.CheckInTime = DateTime.UtcNow;
            existing.Remark = "教师手动补签";
            existing.UpdateTime = DateTime.UtcNow;
            await db.Updateable(existing).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("教师 {TeacherId} 为学生 {StudentId} 手动补签，更新记录 {RecordId}", teacherId, studentId, existing.Id);
            return ToRecordDto(existing);
        }

        var record = new AttendanceRecord
        {
            SessionId = sessionId,
            StudentId = studentId,
            StudentName = student.Name,
            Status = status,
            CheckInTime = DateTime.UtcNow,
            Remark = "教师手动补签",
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(record).ExecuteReturnIdentityAsync(cancellationToken);
        record.Id = id;
        _logger.LogInformation("教师 {TeacherId} 为学生 {StudentId} 手动补签，新增记录 {RecordId}", teacherId, studentId, id);
        return ToRecordDto(record);
    }

    /// <summary>
    /// 随机点名：从班级学生中随机抽取一名，可避免连续回答
    /// </summary>
    public async Task<RandomPickResult> RandomPickAsync(long classId, long? sessionId = null, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 校验班级存在
        var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == classId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            throw new BusinessException($"班级 {classId} 不存在", 404);
        }

        var students = await db.Queryable<Student>()
            .Where(s => s.ClassId == classId && !s.IsDeleted)
            .ToListAsync();

        if (students.Count == 0)
        {
            throw new BusinessException("班级中暂无学生", 400);
        }

        // 若提供 sessionId，优先选择未被连续点名的学生
        List<string>? recentPicks = null;
        if (sessionId.HasValue)
        {
            recentPicks = _randomPickHistory.GetValueOrDefault(sessionId.Value, new List<string>());
        }

        // 候选学生：排除最近被点名的学生，若全部被点名过则使用全部学生
        var candidates = students;
        if (recentPicks is { Count: > 0 })
        {
            var recentSet = new HashSet<string>(recentPicks);
            var filtered = students.Where(s => !recentSet.Contains(s.Id)).ToList();
            if (filtered.Count > 0)
            {
                candidates = filtered;
            }
        }

        // 随机抽取一名
        var random = new Random();
        var picked = candidates[random.Next(candidates.Count)];

        return new RandomPickResult
        {
            StudentId = picked.Id,
            StudentName = picked.Name,
            ClassId = classId,
            ClassName = cls.Name
        };
    }

    /// <summary>
    /// 标记随机点名结果（已回答/未回答），记录在内存历史中
    /// </summary>
    public async Task MarkRandomPickResultAsync(long sessionId, string studentId, bool answered, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (!answered)
        {
            // 未回答不记录历史，允许下次继续被点名
            return;
        }

        // 已回答的学生加入历史，避免连续点名
        var history = _randomPickHistory.GetOrAdd(sessionId, _ => new List<string>());
        lock (history)
        {
            history.Add(studentId);
            // 控制内存上限，超出则移除最早的记录
            if (history.Count > AttendanceConstants.RandomPickHistoryLimit)
            {
                history.RemoveAt(0);
            }
        }

        _logger.LogInformation("会话 {SessionId} 标记学生 {StudentId} 已回答", sessionId, studentId);
    }

    /// <summary>
    /// 获取会话并校验归属教师，返回会话实体
    /// </summary>
    private async Task<AttendanceSession> GetSessionAndVerifyTeacherAsync(ISqlSugarClient db, long sessionId, string teacherId, CancellationToken cancellationToken)
    {
        var session = await db.Queryable<AttendanceSession>()
            .FirstAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);
        if (session is null)
        {
            throw new BusinessException($"考勤会话 {sessionId} 不存在", 404);
        }

        if (session.TeacherId != teacherId)
        {
            throw new BusinessException("仅可操作自己发起的考勤会话", 403);
        }

        return session;
    }

    /// <summary>
    /// 根据会话开始时间与签到时间判定考勤状态
    /// </summary>
    /// <param name="sessionStartTime">会话开始时间</param>
    /// <param name="checkInTime">签到时间</param>
    /// <returns>考勤状态与提示信息</returns>
    private static (AttendanceStatus status, string message) DetermineCheckInStatus(DateTime sessionStartTime, DateTime checkInTime)
    {
        var elapsed = checkInTime - sessionStartTime;

        if (elapsed.TotalMinutes <= AttendanceConstants.PresentThresholdMinutes)
        {
            return (AttendanceStatus.Present, "签到成功");
        }

        if (elapsed.TotalMinutes <= AttendanceConstants.LateThresholdMinutes)
        {
            return (AttendanceStatus.Late, "签到成功（迟到）");
        }

        return (AttendanceStatus.Absent, "签到成功（已超时，记为缺勤）");
    }

    /// <summary>
    /// 生成二维码短期 JWT token（30 秒过期），Claims 包含 SessionId 和 checkin 标识
    /// </summary>
    /// <param name="sessionId">会话 Id</param>
    /// <param name="issuedAt">签发时间（用于测试可注入）</param>
    /// <returns>已签名的 JWT 字符串</returns>
    private string GenerateQrToken(long sessionId, DateTime issuedAt)
    {
        var config = _jwtConfig.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(AttendanceConstants.ClaimSessionId, sessionId.ToString()),
            new Claim(AttendanceConstants.ClaimPurpose, AttendanceConstants.PurposeCheckIn)
        };

        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims,
            notBefore: issuedAt.AddSeconds(-1),
            expires: issuedAt.AddSeconds(AttendanceConstants.QrTokenExpireSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 校验二维码短期 token 有效性，返回 token 中的 SessionId
    /// </summary>
    /// <param name="token">待校验的 JWT 字符串</param>
    /// <param name="expectedSessionId">期望的会话 Id</param>
    /// <returns>token 中的会话 Id</returns>
    /// <exception cref="BusinessException">token 无效或会话 Id 不匹配</exception>
    private long ValidateQrToken(string token, long expectedSessionId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new BusinessException("签到令牌不能为空", 400);
        }

        var config = _jwtConfig.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config.Issuer,
                ValidAudience = config.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromSeconds(1)
            }, out _);

            var sessionIdString = principal.FindFirst(AttendanceConstants.ClaimSessionId)?.Value;
            var purpose = principal.FindFirst(AttendanceConstants.ClaimPurpose)?.Value;

            if (string.IsNullOrEmpty(sessionIdString)
                || !long.TryParse(sessionIdString, out var sessionId)
                || purpose != AttendanceConstants.PurposeCheckIn)
            {
                throw new BusinessException("签到令牌无效", 400);
            }

            if (sessionId != expectedSessionId)
            {
                throw new BusinessException("签到令牌与会话不匹配", 400);
            }

            return sessionId;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "二维码签到令牌校验失败");
            throw new BusinessException("签到令牌已过期或无效", 400);
        }
    }

    /// <summary>
    /// 使用 QRCoder 生成二维码图片并返回 Base64 字符串
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <returns>Base64 编码的 PNG 图片</returns>
    private static string GenerateQrBase64Image(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        var bytes = pngQrCode.GetGraphic(QrPixelsPerModule);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 将考勤记录实体转换为响应 DTO
    /// </summary>
    private static AttendanceRecordResponseDto ToRecordDto(AttendanceRecord record)
        => new()
        {
            Id = record.Id,
            SessionId = record.SessionId,
            StudentId = record.StudentId,
            StudentName = record.StudentName,
            Status = record.Status,
            CheckInTime = record.CheckInTime,
            Remark = record.Remark
        };
}
