using System.Linq.Expressions;
using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Leave;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Services.Leave;

/// <summary>
/// 请假审批流服务实现，封装学生请假申请、辅导员审批、审批通过后联动更新考勤记录为 Leave 状态
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<LeaveService> _logger;

    /// <summary>
    /// 请假记录多表联查的 Select 映射表达式，供所有查询方法复用
    /// </summary>
    private static readonly Expression<Func<LeaveRequest, Student, Teacher, LeaveResponseDto>> LeaveResponseSelector =
        (l, s, t) => new LeaveResponseDto
        {
            Id = l.Id,
            StudentId = l.StudentId,
            StudentName = s.Name,
            CounselorId = l.CounselorId,
            CounselorName = t.Name,
            StartTime = l.StartTime,
            EndTime = l.EndTime,
            LeaveType = l.LeaveType,
            Reason = l.Reason,
            Status = l.Status,
            ReviewRemark = l.ReviewRemark,
            ReviewTime = l.ReviewTime,
            CreateTime = l.CreateTime
        };

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public LeaveService(IDbContext dbContext, ILogger<LeaveService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 构建请假记录三表联查基础查询（LeaveRequest + Student + Teacher）
    /// </summary>
    /// <returns>包含 Join 配置的查询对象</returns>
    private ISugarQueryable<LeaveRequest, Student, Teacher> BuildLeaveJoinQuery()
    {
        return _dbContext.Client.Queryable<LeaveRequest, Student, Teacher>((l, s, t) =>
            new JoinQueryInfos(
                JoinType.Left, l.StudentId == s.Id,
                JoinType.Left, l.CounselorId == t.Id));
    }

    /// <summary>
    /// 学生提交请假申请，CounselorId 从学生所属班级关联获取
    /// </summary>
    public async Task<LeaveResponseDto> CreateLeaveAsync(LeaveCreateDto dto, string studentId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 校验学生存在
        var student = await db.Queryable<Student>()
            .FirstAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);
        if (student is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"学生 {studentId}"), 404);
        }

        // 校验请假时间区间有效
        if (dto.EndTime <= dto.StartTime)
        {
            throw new BusinessException(Msg.Leave.LeaveEndTimeMustAfterStart, 400);
        }

        // 从学生所属班级获取辅导员 Id
        var cls = await db.Queryable<Class>()
            .FirstAsync(c => c.Id == student.ClassId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            throw new BusinessException(Msg.Organization.StudentClassNotFound, 404);
        }

        if (string.IsNullOrEmpty(cls.CounselorId))
        {
            throw new BusinessException(Msg.Organization.ClassCounselorNotConfigured, 400);
        }

        var leave = new LeaveRequest
        {
            StudentId = studentId,
            CounselorId = cls.CounselorId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            LeaveType = dto.LeaveType,
            Reason = dto.Reason,
            Status = LeaveStatus.Pending,
            CreateTime = DateTime.UtcNow
        };

        var id = await db.Insertable(leave).ExecuteReturnIdentityAsync(cancellationToken);
        _logger.LogInformation("学生 {StudentId} 提交请假申请 {LeaveId}，辅导员 {CounselorId}", studentId, id, cls.CounselorId);

        return (await GetLeaveByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 学生分页查询自己的请假记录
    /// </summary>
    public async Task<PagedResult<LeaveResponseDto>> GetLeavesByStudentAsync(string studentId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = BuildLeaveJoinQuery()
            .Where((l, s, t) => l.StudentId == studentId && !l.IsDeleted);

        var total = await query.CountAsync();
        var rows = await query
            .Select(LeaveResponseSelector)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<LeaveResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 辅导员分页查询请假记录，支持状态过滤
    /// </summary>
    public async Task<PagedResult<LeaveResponseDto>> GetLeavesByCounselorAsync(string counselorId, LeaveStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = BuildLeaveJoinQuery()
            .Where((l, s, t) => l.CounselorId == counselorId && !l.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where((l, s, t) => l.Status == status.Value);
        }

        var total = await query.CountAsync();
        var rows = await query
            .Select(LeaveResponseSelector)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<LeaveResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 辅导员待审批数量（用于首页提醒）
    /// </summary>
    public async Task<long> GetPendingLeavesCountAsync(string counselorId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        return await db.Queryable<LeaveRequest>()
            .Where(l => l.CounselorId == counselorId && l.Status == LeaveStatus.Pending && !l.IsDeleted)
            .CountAsync();
    }

    /// <summary>
    /// 查询请假详情
    /// </summary>
    public async Task<LeaveResponseDto?> GetLeaveByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var dto = await BuildLeaveJoinQuery()
            .Where((l, s, t) => l.Id == id && !l.IsDeleted)
            .Select(LeaveResponseSelector)
            .FirstAsync();

        return dto;
    }

    /// <summary>
    /// 审批通过（Status=Approved，记录 ReviewRemark 和 ReviewTime，并联动更新考勤记录为 Leave）
    /// </summary>
    public async Task<LeaveResponseDto> ApproveLeaveAsync(long id, string counselorId, LeaveReviewDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var leave = await GetLeaveAndVerifyCounselorAsync(db, id, counselorId, cancellationToken);

        if (leave.Status != LeaveStatus.Pending)
        {
            throw new BusinessException(Msg.Leave.LeaveAlreadyReviewed, 400);
        }

        var reviewTime = DateTime.UtcNow;
        leave.Status = LeaveStatus.Approved;
        leave.ReviewRemark = dto.ReviewRemark;
        leave.ReviewTime = reviewTime;
        leave.UpdateTime = reviewTime;

        // 事务保证审批状态与考勤记录联动更新的一致性
        await db.Ado.UseTranAsync(async () =>
        {
            await db.Updateable(leave)
                .UpdateColumns(it => new { it.Status, it.ReviewRemark, it.ReviewTime, it.UpdateTime })
                .ExecuteCommandAsync(cancellationToken);

            // 联动更新：将请假时间段内该学生的考勤记录状态更新为 Leave
            await UpdateAttendanceRecordsToLeaveAsync(db, leave.StudentId, leave.StartTime, leave.EndTime, cancellationToken);
        });

        _logger.LogInformation("辅导员 {CounselorId} 通过请假申请 {LeaveId}", counselorId, id);
        return (await GetLeaveByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 审批驳回（Status=Rejected）
    /// </summary>
    public async Task<LeaveResponseDto> RejectLeaveAsync(long id, string counselorId, LeaveReviewDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var leave = await GetLeaveAndVerifyCounselorAsync(db, id, counselorId, cancellationToken);

        if (leave.Status != LeaveStatus.Pending)
        {
            throw new BusinessException(Msg.Leave.LeaveAlreadyReviewed, 400);
        }

        var reviewTime = DateTime.UtcNow;
        leave.Status = LeaveStatus.Rejected;
        leave.ReviewRemark = dto.ReviewRemark;
        leave.ReviewTime = reviewTime;
        leave.UpdateTime = reviewTime;

        await db.Updateable(leave)
            .UpdateColumns(it => new { it.Status, it.ReviewRemark, it.ReviewTime, it.UpdateTime })
            .ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("辅导员 {CounselorId} 驳回请假申请 {LeaveId}", counselorId, id);
        return (await GetLeaveByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 按班级查询请假记录（教师/辅导员查看班级请假情况）
    /// </summary>
    public async Task<List<LeaveResponseDto>> GetLeavesByClassAsync(long classId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // 通过学生关联班级，查询请假区间与查询区间有交集的记录
        var rows = await BuildLeaveJoinQuery()
            .Where((l, s, t) => s.ClassId == classId && !l.IsDeleted && !s.IsDeleted
                && l.StartTime <= endDate && l.EndTime >= startDate)
            .Select(LeaveResponseSelector)
            .OrderBy(it => it.StartTime, OrderByType.Desc)
            .ToListAsync();

        return rows;
    }

    /// <summary>
    /// 获取请假申请并校验归属辅导员
    /// </summary>
    private async Task<LeaveRequest> GetLeaveAndVerifyCounselorAsync(ISqlSugarClient db, long id, string counselorId, CancellationToken cancellationToken)
    {
        var leave = await db.Queryable<LeaveRequest>()
            .FirstAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        if (leave is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"请假申请 {id}"), 404);
        }

        if (leave.CounselorId != counselorId)
        {
            throw new BusinessException(Msg.Leave.OnlyOwnLeave, 403);
        }

        return leave;
    }

    /// <summary>
    /// 联动更新：将请假时间段内该学生的考勤记录状态更新为 Leave
    /// </summary>
    /// <param name="db">数据库客户端</param>
    /// <param name="studentId">学生学号</param>
    /// <param name="leaveStart">请假开始时间</param>
    /// <param name="leaveEnd">请假结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task UpdateAttendanceRecordsToLeaveAsync(ISqlSugarClient db, string studentId, DateTime leaveStart, DateTime leaveEnd, CancellationToken cancellationToken)
    {
        // 查询请假时间段内该学生的所有考勤记录（会话开始时间落在请假区间内）
        var records = await db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
            .Where((r, s) => r.StudentId == studentId && !r.IsDeleted && !s.IsDeleted
                && s.StartTime >= leaveStart && s.StartTime <= leaveEnd)
            .ToListAsync();

        if (records.Count == 0)
        {
            return;
        }

        var updateTime = DateTime.UtcNow;
        foreach (var record in records)
        {
            record.Status = AttendanceStatus.Leave;
            record.Remark = Msg.Attendance.LeaveApprovedRemark;
            record.UpdateTime = updateTime;
        }

        await db.Updateable(records)
            .UpdateColumns(it => new { it.Status, it.Remark, it.UpdateTime })
            .ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("学生 {StudentId} 请假审批通过，联动更新 {Count} 条考勤记录为 Leave", studentId, records.Count);
    }
}
