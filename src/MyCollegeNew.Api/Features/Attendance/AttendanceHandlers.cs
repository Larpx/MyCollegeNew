using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Constants;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Attendance;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using SqlSugar;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Attendance
{
    /// <summary>
    /// 考勤会话与签到处理器
    /// </summary>
    public class AttendanceHandlers :
        IRequestHandler<CreateSessionCommand, ApiResponse<SessionResponseDto>>,
        IRequestHandler<GetSessionByIdQuery, ApiResponse<SessionResponseDto>>,
        IRequestHandler<GetActiveSessionsQuery, ApiResponse<List<SessionResponseDto>>>,
        IRequestHandler<GetSessionsByTeacherQuery, ApiResponse<PagedResult<SessionResponseDto>>>,
        IRequestHandler<CloseSessionCommand, ApiResponse<object>>,
        IRequestHandler<GetSessionRecordsQuery, ApiResponse<List<AttendanceRecordResponseDto>>>,
        IRequestHandler<GenerateQrCodeCommand, ApiResponse<QrCodeResult>>,
        IRequestHandler<CheckInCommand, ApiResponse<CheckInResult>>,
        IRequestHandler<RollCallCommand, ApiResponse<int>>,
        IRequestHandler<UpdateRecordStatusCommand, ApiResponse<object>>,
        IRequestHandler<ManualCheckInCommand, ApiResponse<AttendanceRecordResponseDto>>,
        IRequestHandler<RandomPickQuery, ApiResponse<RandomPickResult>>,
        IRequestHandler<MarkRandomPickCommand, ApiResponse<object>>
    {
        /// <summary>会话默认持续时长（分钟）</summary>
        private const int DefaultSessionDurationMinutes = 30;

        /// <summary>二维码图片每像素模块数</summary>
        private const int QrPixelsPerModule = 20;

        private readonly IDbContext _dbContext;
        private readonly IOptions<JwtConfig> _jwtConfig;
        private readonly ILogger<AttendanceHandlers> _logger;
        private readonly SymmetricSecurityKey _signingKey;
        private static readonly JwtSecurityTokenHandler _tokenHandler = new();

        /// <summary>随机点名历史记录</summary>
        private static readonly ConcurrentDictionary<long, List<string>> _randomPickHistory = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="jwtConfig">JWT 配置</param>
        /// <param name="logger">日志器</param>
        public AttendanceHandlers(IDbContext dbContext, IOptions<JwtConfig> jwtConfig, ILogger<AttendanceHandlers> logger)
        {
            _dbContext = dbContext;
            _jwtConfig = jwtConfig;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Value.SecretKey));
            _logger = logger;
        }

        /// <summary>创建考勤会话</summary>
        public async Task<ApiResponse<SessionResponseDto>> Handle(CreateSessionCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var course = await db.Queryable<Course>().FirstAsync(c => c.Id == command.Dto.CourseId && !c.IsDeleted, cancellationToken);
            if (course is null)
            {
                return ApiResponse<SessionResponseDto>.Fail(Msg.Common.EntityNotFound($"课程 {command.Dto.CourseId}"), 404);
            }

            if (course.TeacherId != command.TeacherId)
            {
                return ApiResponse<SessionResponseDto>.Fail(Msg.Attendance.OnlyOwnCourse, 403);
            }

            var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == command.Dto.ClassId && !c.IsDeleted, cancellationToken);
            if (cls is null)
            {
                return ApiResponse<SessionResponseDto>.Fail(Msg.Common.EntityNotFound($"班级 {command.Dto.ClassId}"), 404);
            }

            var startTime = command.Dto.StartTime;
            var endTime = command.Dto.EndTime ?? startTime.AddMinutes(DefaultSessionDurationMinutes);
            if (endTime <= startTime)
            {
                return ApiResponse<SessionResponseDto>.Fail(Msg.Attendance.EndTimeMustAfterStart, 400);
            }

            var session = new AttendanceSession
            {
                CourseId = command.Dto.CourseId,
                ClassId = command.Dto.ClassId,
                TeacherId = command.TeacherId,
                ScheduleId = command.Dto.ScheduleId,
                StartTime = startTime,
                EndTime = endTime,
                Status = SessionStatus.Active,
                QrToken = null,
                CreateTime = DateTime.UtcNow
            };

            var id = await db.Insertable(session).ExecuteReturnIdentityAsync(cancellationToken);
            var initialToken = GenerateQrToken(id, DateTime.UtcNow);
            session.Id = id;
            session.QrToken = initialToken;
            await db.Updateable(session).UpdateColumns(it => new { it.QrToken }).ExecuteCommandAsync(cancellationToken);

            _logger.LogInformation("教师 {TeacherId} 创建考勤会话 {SessionId}", command.TeacherId, id);
            return await Handle(new GetSessionByIdQuery(id), cancellationToken);
        }

        /// <summary>查询会话详情</summary>
        public async Task<ApiResponse<SessionResponseDto>> Handle(GetSessionByIdQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var dto = await db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                    new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
                .Where((s, c, cls, t) => s.Id == query.Id && !s.IsDeleted)
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
                }).FirstAsync();

            if (dto is null)
            {
                return ApiResponse<SessionResponseDto>.Fail(Msg.Common.EntityNotFound("考勤会话"), 404);
            }

            return ApiResponse<SessionResponseDto>.Success(dto);
        }

        /// <summary>查询教师进行中的会话</summary>
        public async Task<ApiResponse<List<SessionResponseDto>>> Handle(GetActiveSessionsQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var rows = await db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                    new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
                .Where((s, c, cls, t) => s.TeacherId == query.TeacherId && s.Status == SessionStatus.Active && !s.IsDeleted)
                .OrderBy((s, c, cls, t) => s.StartTime, OrderByType.Desc)
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
                }).ToListAsync();

            return ApiResponse<List<SessionResponseDto>>.Success(rows);
        }

        /// <summary>分页查询教师历史会话</summary>
        public async Task<ApiResponse<PagedResult<SessionResponseDto>>> Handle(GetSessionsByTeacherQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var q = db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                    new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
                .Where((s, c, cls, t) => s.TeacherId == query.TeacherId && !s.IsDeleted);

            if (query.StartDate.HasValue)
            {
                q = q.Where((s, c, cls, t) => s.StartTime >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                q = q.Where((s, c, cls, t) => s.StartTime <= query.EndDate.Value);
            }

            var total = await q.CountAsync();
            var rows = await q
                .OrderBy((s, c, cls, t) => s.StartTime, OrderByType.Desc)
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
              .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

            return ApiResponse<PagedResult<SessionResponseDto>>.Success(
                PagedResult<SessionResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
        }

        /// <summary>关闭会话</summary>
        public async Task<ApiResponse<object>> Handle(CloseSessionCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var session = await GetSessionAndVerifyTeacherAsync(db, command.SessionId, command.TeacherId, cancellationToken);
            if (session.Status == SessionStatus.Closed)
            {
                return ApiResponse<object>.Fail(Msg.Attendance.SessionAlreadyClosed, 400);
            }

            var students = await db.Queryable<Student>().Where(s => s.ClassId == session.ClassId && !s.IsDeleted).ToListAsync();
            var existingStudentIds = await db.Queryable<AttendanceRecord>()
                .Where(r => r.SessionId == command.SessionId && !r.IsDeleted).Select(r => r.StudentId).ToListAsync();
            var existingSet = new HashSet<string>(existingStudentIds);

            var absentRecords = students.Where(s => !existingSet.Contains(s.Id)).Select(s => new AttendanceRecord
            {
                SessionId = command.SessionId,
                StudentId = s.Id,
                StudentName = s.Name,
                Status = AttendanceStatus.Absent,
                CheckInTime = null,
                Remark = Msg.Attendance.AutoAbsentRemark,
                CreateTime = DateTime.UtcNow
            }).ToList();

            session.Status = SessionStatus.Closed;
            session.UpdateTime = DateTime.UtcNow;

            await db.Ado.UseTranAsync(async () =>
            {
                await db.Updateable(session).UpdateColumns(it => new { it.Status, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);
                if (absentRecords.Count > 0)
                {
                    await db.Insertable(absentRecords).ExecuteCommandAsync(cancellationToken);
                }
            });

            _logger.LogInformation("关闭会话 {SessionId}，自动生成 {AbsentCount} 条缺勤记录", command.SessionId, absentRecords.Count);
            return ApiResponse<object>.Success("会话已关闭");
        }

        /// <summary>查询会话签到记录</summary>
        public async Task<ApiResponse<List<AttendanceRecordResponseDto>>> Handle(GetSessionRecordsQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var rows = await db.Queryable<AttendanceRecord>()
                .Where(r => r.SessionId == query.SessionId && !r.IsDeleted)
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
                }).ToListAsync();

            return ApiResponse<List<AttendanceRecordResponseDto>>.Success(rows);
        }

        /// <summary>生成二维码</summary>
        public async Task<ApiResponse<QrCodeResult>> Handle(GenerateQrCodeCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var session = await GetSessionAndVerifyTeacherAsync(db, command.SessionId, command.TeacherId, cancellationToken);
            if (session.Status != SessionStatus.Active)
            {
                return ApiResponse<QrCodeResult>.Fail(Msg.Attendance.SessionClosed, 400);
            }

            var token = GenerateQrToken(command.SessionId, DateTime.UtcNow);
            session.QrToken = token;
            session.UpdateTime = DateTime.UtcNow;
            await db.Updateable(session).UpdateColumns(it => new { it.QrToken, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);

            var qrContent = $"/api/sessions/{command.SessionId}/checkin?token={token}";
            var base64Image = GenerateQrBase64Image(qrContent);

            _logger.LogInformation("教师 {TeacherId} 为会话 {SessionId} 生成二维码", command.TeacherId, command.SessionId);

            return ApiResponse<QrCodeResult>.Success(new QrCodeResult
            {
                Token = token,
                Base64Image = base64Image,
                ExpireSeconds = AttendanceConstants.QrTokenExpireSeconds,
                GenerateTime = DateTime.UtcNow
            });
        }

        /// <summary>学生签到</summary>
        public async Task<ApiResponse<CheckInResult>> Handle(CheckInCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;

            // 校验 token
            var tokenValidation = ValidateQrToken(command.Token, command.SessionId);
            if (tokenValidation.ErrorMessage is not null)
            {
                return ApiResponse<CheckInResult>.Fail(tokenValidation.ErrorMessage, 400);
            }

            var session = await db.Queryable<AttendanceSession>().FirstAsync(s => s.Id == command.SessionId && !s.IsDeleted, cancellationToken);
            if (session is null)
            {
                return ApiResponse<CheckInResult>.Fail(Msg.Common.EntityNotFound("考勤会话"), 404);
            }

            if (session.Status != SessionStatus.Active)
            {
                return ApiResponse<CheckInResult>.Fail(Msg.Attendance.SessionClosedCheckIn, 400);
            }

            var student = await db.Queryable<Student>().FirstAsync(s => s.Id == command.StudentId && !s.IsDeleted, cancellationToken);
            if (student is null)
            {
                return ApiResponse<CheckInResult>.Fail(Msg.Common.EntityNotFound("学生"), 404);
            }

            if (student.ClassId != session.ClassId)
            {
                return ApiResponse<CheckInResult>.Fail(Msg.Attendance.StudentNotInClass, 403);
            }

            var exists = await db.Queryable<AttendanceRecord>()
                .AnyAsync(r => r.SessionId == command.SessionId && r.StudentId == command.StudentId && !r.IsDeleted, cancellationToken);
            if (exists)
            {
                return ApiResponse<CheckInResult>.Fail(Msg.Attendance.DuplicateCheckIn, 400);
            }

            var checkInTime = DateTime.UtcNow;
            var (status, message) = DetermineCheckInStatus(session.StartTime, checkInTime);

            var record = new AttendanceRecord
            {
                SessionId = command.SessionId,
                StudentId = command.StudentId,
                StudentName = student.Name,
                Status = status,
                CheckInTime = checkInTime,
                Remark = null,
                CreateTime = DateTime.UtcNow
            };
            await db.Insertable(record).ExecuteCommandAsync(cancellationToken);

            _logger.LogInformation("学生 {StudentId} 在会话 {SessionId} 签到，状态 {Status}", command.StudentId, command.SessionId, status);

            return ApiResponse<CheckInResult>.Success(new CheckInResult { Status = status, CheckInTime = checkInTime, Message = message });
        }

        /// <summary>一键点名</summary>
        public async Task<ApiResponse<int>> Handle(RollCallCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var session = await GetSessionAndVerifyTeacherAsync(db, command.SessionId, command.TeacherId, cancellationToken);

            var students = await db.Queryable<Student>().Where(s => s.ClassId == session.ClassId && !s.IsDeleted).ToListAsync();
            var existingStudentIds = await db.Queryable<AttendanceRecord>()
                .Where(r => r.SessionId == command.SessionId && !r.IsDeleted).Select(r => r.StudentId).ToListAsync();
            var existingSet = new HashSet<string>(existingStudentIds);

            var checkInTime = DateTime.UtcNow;
            var newRecords = students.Where(s => !existingSet.Contains(s.Id)).Select(s => new AttendanceRecord
            {
                SessionId = command.SessionId,
                StudentId = s.Id,
                StudentName = s.Name,
                Status = AttendanceStatus.Present,
                CheckInTime = checkInTime,
                Remark = Msg.Attendance.ManualCheckInRemark,
                CreateTime = DateTime.UtcNow
            }).ToList();

            if (newRecords.Count > 0)
            {
                await db.Insertable(newRecords).ExecuteCommandAsync(cancellationToken);
            }

            _logger.LogInformation("会话 {SessionId} 一键点名，标记 {Count} 名学生为 Present", command.SessionId, newRecords.Count);
            return ApiResponse<int>.Success(newRecords.Count);
        }

        /// <summary>修改考勤记录状态</summary>
        public async Task<ApiResponse<object>> Handle(UpdateRecordStatusCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var record = await db.Queryable<AttendanceRecord>().FirstAsync(r => r.Id == command.RecordId && !r.IsDeleted, cancellationToken);
            if (record is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"考勤记录 {command.RecordId}"), 404);
            }

            var session = await db.Queryable<AttendanceSession>().FirstAsync(s => s.Id == record.SessionId && !s.IsDeleted, cancellationToken);
            if (session is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound("考勤会话"), 404);
            }

            if (session.TeacherId != command.TeacherId)
            {
                return ApiResponse<object>.Fail(Msg.Attendance.OnlyOwnRecord, 403);
            }

            record.Status = command.Status;
            record.UpdateTime = DateTime.UtcNow;
            await db.Updateable(record).UpdateColumns(it => new { it.Status, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);

            _logger.LogInformation("教师 {TeacherId} 修改记录 {RecordId} 状态为 {Status}", command.TeacherId, command.RecordId, command.Status);
            return ApiResponse<object>.Success("修改成功");
        }

        /// <summary>手动补签</summary>
        public async Task<ApiResponse<AttendanceRecordResponseDto>> Handle(ManualCheckInCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var session = await GetSessionAndVerifyTeacherAsync(db, command.SessionId, command.TeacherId, cancellationToken);

            var student = await db.Queryable<Student>().FirstAsync(s => s.Id == command.StudentId && !s.IsDeleted, cancellationToken);
            if (student is null)
            {
                return ApiResponse<AttendanceRecordResponseDto>.Fail(Msg.Common.EntityNotFound($"学生 {command.StudentId}"), 404);
            }

            if (student.ClassId != session.ClassId)
            {
                return ApiResponse<AttendanceRecordResponseDto>.Fail(Msg.Attendance.StudentNotInClass, 403);
            }

            var existing = await db.Queryable<AttendanceRecord>()
                .FirstAsync(r => r.SessionId == command.SessionId && r.StudentId == command.StudentId && !r.IsDeleted, cancellationToken);

            if (existing is not null)
            {
                existing.Status = command.Status;
                existing.CheckInTime = DateTime.UtcNow;
                existing.Remark = Msg.Attendance.ManualCheckIn;
                existing.UpdateTime = DateTime.UtcNow;
                await db.Updateable(existing).ExecuteCommandAsync(cancellationToken);
                _logger.LogInformation("教师 {TeacherId} 为学生 {StudentId} 手动补签，更新记录 {RecordId}", command.TeacherId, command.StudentId, existing.Id);
                return ApiResponse<AttendanceRecordResponseDto>.Success(ToRecordDto(existing));
            }

            var record = new AttendanceRecord
            {
                SessionId = command.SessionId,
                StudentId = command.StudentId,
                StudentName = student.Name,
                Status = command.Status,
                CheckInTime = DateTime.UtcNow,
                Remark = Msg.Attendance.ManualCheckIn,
                CreateTime = DateTime.UtcNow
            };
            var id = await db.Insertable(record).ExecuteReturnIdentityAsync(cancellationToken);
            record.Id = id;
            _logger.LogInformation("教师 {TeacherId} 为学生 {StudentId} 手动补签，新增记录 {RecordId}", command.TeacherId, command.StudentId, id);
            return ApiResponse<AttendanceRecordResponseDto>.Success(ToRecordDto(record));
        }

        /// <summary>随机点名</summary>
        public async Task<ApiResponse<RandomPickResult>> Handle(RandomPickQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == query.ClassId && !c.IsDeleted, cancellationToken);
            if (cls is null)
            {
                return ApiResponse<RandomPickResult>.Fail(Msg.Common.EntityNotFound($"班级 {query.ClassId}"), 404);
            }

            var students = await db.Queryable<Student>().Where(s => s.ClassId == query.ClassId && !s.IsDeleted).ToListAsync();
            if (students.Count == 0)
            {
                return ApiResponse<RandomPickResult>.Fail(Msg.Attendance.ClassNoStudents, 400);
            }

            List<string>? recentPicks = null;
            if (query.SessionId.HasValue)
            {
                recentPicks = _randomPickHistory.GetValueOrDefault(query.SessionId.Value, new List<string>());
            }

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

            var picked = candidates[Random.Shared.Next(candidates.Count)];
            return ApiResponse<RandomPickResult>.Success(new RandomPickResult
            {
                StudentId = picked.Id,
                StudentName = picked.Name,
                ClassId = query.ClassId,
                ClassName = cls.Name
            });
        }

        /// <summary>标记随机点名结果</summary>
        public Task<ApiResponse<object>> Handle(MarkRandomPickCommand command, CancellationToken cancellationToken)
        {
            if (command.Answered)
            {
                var history = _randomPickHistory.GetOrAdd(command.SessionId, _ => new List<string>());
                lock (history)
                {
                    history.Add(command.StudentId);
                    if (history.Count > AttendanceConstants.RandomPickHistoryLimit)
                    {
                        history.RemoveAt(0);
                    }
                }

                _logger.LogInformation("会话 {SessionId} 标记学生 {StudentId} 已回答", command.SessionId, command.StudentId);
            }

            return Task.FromResult(ApiResponse<object>.Success("标记成功"));
        }

        /// <summary>获取会话并校验归属教师</summary>
        private async Task<AttendanceSession> GetSessionAndVerifyTeacherAsync(ISqlSugarClient db, long sessionId, string teacherId, CancellationToken cancellationToken)
        {
            var session = await db.Queryable<AttendanceSession>().FirstAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);
            if (session is null)
            {
                throw new Shared.Exceptions.BusinessException($"考勤会话 {sessionId} 不存在", 404);
            }

            if (session.TeacherId != teacherId)
            {
                throw new Shared.Exceptions.BusinessException(Msg.Attendance.OnlyOwnSession, 403);
            }

            return session;
        }

        /// <summary>判定签到状态</summary>
        private static (AttendanceStatus status, string message) DetermineCheckInStatus(DateTime sessionStartTime, DateTime checkInTime)
        {
            var elapsed = checkInTime - sessionStartTime;
            if (elapsed.TotalMinutes <= AttendanceConstants.PresentThresholdMinutes)
            {
                return (AttendanceStatus.Present, Msg.Attendance.CheckInSuccess);
            }

            if (elapsed.TotalMinutes <= AttendanceConstants.LateThresholdMinutes)
            {
                return (AttendanceStatus.Late, Msg.Attendance.CheckInSuccessLate);
            }

            return (AttendanceStatus.Absent, Msg.Attendance.CheckInSuccessTimeout);
        }

        /// <summary>生成二维码短期 JWT token</summary>
        private string GenerateQrToken(long sessionId, DateTime issuedAt)
        {
            var config = _jwtConfig.Value;
            var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
            new Claim(AttendanceConstants.ClaimSessionId, sessionId.ToString()),
            new Claim(AttendanceConstants.ClaimPurpose, AttendanceConstants.PurposeCheckIn)
        };

            var token = new JwtSecurityToken(
                issuer: config.Issuer, audience: config.Audience, claims: claims,
                notBefore: issuedAt.AddSeconds(-1),
                expires: issuedAt.AddSeconds(AttendanceConstants.QrTokenExpireSeconds),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }

        /// <summary>校验二维码短期 token</summary>
        private (long SessionId, string? ErrorMessage) ValidateQrToken(string token, long expectedSessionId)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (0, Msg.Attendance.QrTokenEmpty);
            }

            try
            {
                var config = _jwtConfig.Value;
                var principal = _tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config.Issuer,
                    ValidAudience = config.Audience,
                    IssuerSigningKey = _signingKey,
                    ClockSkew = TimeSpan.FromSeconds(1)
                }, out _);

                var sessionIdString = principal.FindFirst(AttendanceConstants.ClaimSessionId)?.Value;
                var purpose = principal.FindFirst(AttendanceConstants.ClaimPurpose)?.Value;

                if (string.IsNullOrEmpty(sessionIdString) || !long.TryParse(sessionIdString, out var sessionId) || purpose != AttendanceConstants.PurposeCheckIn)
                {
                    return (0, Msg.Attendance.QrTokenInvalid);
                }

                if (sessionId != expectedSessionId)
                {
                    return (0, "签到令牌与会话不匹配");
                }

                return (sessionId, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "二维码签到令牌校验失败");
                return (0, Msg.Attendance.QrTokenExpired);
            }
        }

        /// <summary>生成二维码 Base64 图片</summary>
        private static string GenerateQrBase64Image(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(qrCodeData);
            var bytes = pngQrCode.GetGraphic(QrPixelsPerModule);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>将考勤记录转换为 DTO</summary>
        private static AttendanceRecordResponseDto ToRecordDto(AttendanceRecord record) => new()
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
}