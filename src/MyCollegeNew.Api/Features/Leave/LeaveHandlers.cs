using System.Linq.Expressions;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Leave;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Leave
{
/// <summary>
/// 请假审批处理器
/// </summary>
public class LeaveHandlers :
    IRequestHandler<CreateLeaveCommand, ApiResponse<LeaveResponseDto>>,
    IRequestHandler<GetLeavesByStudentQuery, ApiResponse<PagedResult<LeaveResponseDto>>>,
    IRequestHandler<GetLeavesByCounselorQuery, ApiResponse<PagedResult<LeaveResponseDto>>>,
    IRequestHandler<GetPendingLeavesCountQuery, ApiResponse<long>>,
    IRequestHandler<GetLeaveByIdQuery, ApiResponse<LeaveResponseDto>>,
    IRequestHandler<ApproveLeaveCommand, ApiResponse<LeaveResponseDto>>,
    IRequestHandler<RejectLeaveCommand, ApiResponse<LeaveResponseDto>>,
    IRequestHandler<GetLeavesByClassQuery, ApiResponse<List<LeaveResponseDto>>>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<LeaveHandlers> _logger;

    /// <summary>请假记录多表联查的 Select 映射表达式</summary>
    private static readonly Expression<Func<LeaveRequest, Student, Teacher, LeaveResponseDto>> LeaveSelector =
        (l, s, t) => new LeaveResponseDto
        {
            Id = l.Id, StudentId = l.StudentId, StudentName = s.Name,
            CounselorId = l.CounselorId, CounselorName = t.Name,
            StartTime = l.StartTime, EndTime = l.EndTime, LeaveType = l.LeaveType,
            Reason = l.Reason, Status = l.Status, ReviewRemark = l.ReviewRemark,
            ReviewTime = l.ReviewTime, CreateTime = l.CreateTime
        };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public LeaveHandlers(IDbContext dbContext, ILogger<LeaveHandlers> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>学生提交请假</summary>
    public async Task<ApiResponse<LeaveResponseDto>> Handle(CreateLeaveCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == command.StudentId && !s.IsDeleted, cancellationToken);
        if (student is null)
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Common.EntityNotFound($"学生 {command.StudentId}"), 404);
        }

        if (command.Dto.EndTime <= command.Dto.StartTime)
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Leave.LeaveEndTimeMustAfterStart, 400);
        }

        var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == student.ClassId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Organization.StudentClassNotFound, 404);
        }

        if (string.IsNullOrEmpty(cls.CounselorId))
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Organization.ClassCounselorNotConfigured, 400);
        }

        var leave = new LeaveRequest
        {
            StudentId = command.StudentId, CounselorId = cls.CounselorId,
            StartTime = command.Dto.StartTime, EndTime = command.Dto.EndTime,
            LeaveType = command.Dto.LeaveType, Reason = command.Dto.Reason,
            Status = LeaveStatus.Pending, CreateTime = DateTime.UtcNow
        };

        var id = await db.Insertable(leave).ExecuteReturnIdentityAsync(cancellationToken);
        _logger.LogInformation("学生 {StudentId} 提交请假申请 {LeaveId}", command.StudentId, id);
        return await Handle(new GetLeaveByIdQuery(id), cancellationToken);
    }

    /// <summary>学生分页查询请假记录</summary>
    public async Task<ApiResponse<PagedResult<LeaveResponseDto>>> Handle(GetLeavesByStudentQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var q = db.Queryable<LeaveRequest, Student, Teacher>((l, s, t) =>
                new JoinQueryInfos(JoinType.Left, l.StudentId == s.Id, JoinType.Left, l.CounselorId == t.Id))
            .Where((l, s, t) => l.StudentId == query.StudentId && !l.IsDeleted);

        var total = await q.CountAsync();
        var rows = await q.Select(LeaveSelector).OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return ApiResponse<PagedResult<LeaveResponseDto>>.Success(
            PagedResult<LeaveResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
    }

    /// <summary>辅导员分页查询请假记录</summary>
    public async Task<ApiResponse<PagedResult<LeaveResponseDto>>> Handle(GetLeavesByCounselorQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var q = db.Queryable<LeaveRequest, Student, Teacher>((l, s, t) =>
                new JoinQueryInfos(JoinType.Left, l.StudentId == s.Id, JoinType.Left, l.CounselorId == t.Id))
            .Where((l, s, t) => l.CounselorId == query.CounselorId && !l.IsDeleted);

        if (query.Status.HasValue)
        {
            q = q.Where((l, s, t) => l.Status == query.Status.Value);
        }

        var total = await q.CountAsync();
        var rows = await q.Select(LeaveSelector).OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return ApiResponse<PagedResult<LeaveResponseDto>>.Success(
            PagedResult<LeaveResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
    }

    /// <summary>辅导员待审批数量</summary>
    public async Task<ApiResponse<long>> Handle(GetPendingLeavesCountQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var count = await db.Queryable<LeaveRequest>()
            .Where(l => l.CounselorId == query.CounselorId && l.Status == LeaveStatus.Pending && !l.IsDeleted)
            .CountAsync();
        return ApiResponse<long>.Success(count);
    }

    /// <summary>查询请假详情</summary>
    public async Task<ApiResponse<LeaveResponseDto>> Handle(GetLeaveByIdQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<LeaveRequest, Student, Teacher>((l, s, t) =>
                new JoinQueryInfos(JoinType.Left, l.StudentId == s.Id, JoinType.Left, l.CounselorId == t.Id))
            .Where((l, s, t) => l.Id == query.Id && !l.IsDeleted)
            .Select(LeaveSelector).FirstAsync();

        if (dto is null)
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Common.EntityNotFound($"请假申请 {query.Id}"), 404);
        }

        return ApiResponse<LeaveResponseDto>.Success(dto);
    }

    /// <summary>审批通过</summary>
    public async Task<ApiResponse<LeaveResponseDto>> Handle(ApproveLeaveCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var leave = await GetLeaveAndVerifyCounselorAsync(db, command.Id, command.CounselorId, cancellationToken);

        if (leave.Status != LeaveStatus.Pending)
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Leave.LeaveAlreadyReviewed, 400);
        }

        var reviewTime = DateTime.UtcNow;
        leave.Status = LeaveStatus.Approved;
        leave.ReviewRemark = command.Dto.ReviewRemark;
        leave.ReviewTime = reviewTime;
        leave.UpdateTime = reviewTime;

        await db.Ado.UseTranAsync(async () =>
        {
            await db.Updateable(leave).UpdateColumns(it => new { it.Status, it.ReviewRemark, it.ReviewTime, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);
            await UpdateAttendanceRecordsToLeaveAsync(db, leave.StudentId, leave.StartTime, leave.EndTime, cancellationToken);
        });

        _logger.LogInformation("辅导员 {CounselorId} 通过请假申请 {LeaveId}", command.CounselorId, command.Id);
        return await Handle(new GetLeaveByIdQuery(command.Id), cancellationToken);
    }

    /// <summary>审批驳回</summary>
    public async Task<ApiResponse<LeaveResponseDto>> Handle(RejectLeaveCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var leave = await GetLeaveAndVerifyCounselorAsync(db, command.Id, command.CounselorId, cancellationToken);

        if (leave.Status != LeaveStatus.Pending)
        {
            return ApiResponse<LeaveResponseDto>.Fail(Msg.Leave.LeaveAlreadyReviewed, 400);
        }

        var reviewTime = DateTime.UtcNow;
        leave.Status = LeaveStatus.Rejected;
        leave.ReviewRemark = command.Dto.ReviewRemark;
        leave.ReviewTime = reviewTime;
        leave.UpdateTime = reviewTime;

        await db.Updateable(leave).UpdateColumns(it => new { it.Status, it.ReviewRemark, it.ReviewTime, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("辅导员 {CounselorId} 驳回请假申请 {LeaveId}", command.CounselorId, command.Id);
        return await Handle(new GetLeaveByIdQuery(command.Id), cancellationToken);
    }

    /// <summary>按班级查询请假记录</summary>
    public async Task<ApiResponse<List<LeaveResponseDto>>> Handle(GetLeavesByClassQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var rows = await db.Queryable<LeaveRequest, Student, Teacher>((l, s, t) =>
                new JoinQueryInfos(JoinType.Left, l.StudentId == s.Id, JoinType.Left, l.CounselorId == t.Id))
            .Where((l, s, t) => s.ClassId == query.ClassId && !l.IsDeleted && !s.IsDeleted
                && l.StartTime <= query.EndDate && l.EndTime >= query.StartDate)
            .Select(LeaveSelector).OrderBy(it => it.StartTime, OrderByType.Desc).ToListAsync();

        return ApiResponse<List<LeaveResponseDto>>.Success(rows);
    }

    /// <summary>获取请假申请并校验归属辅导员</summary>
    private async Task<LeaveRequest> GetLeaveAndVerifyCounselorAsync(ISqlSugarClient db, long id, string counselorId, CancellationToken cancellationToken)
    {
        var leave = await db.Queryable<LeaveRequest>().FirstAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        if (leave is null)
        {
            throw new Shared.Exceptions.BusinessException(Msg.Common.EntityNotFound($"请假申请 {id}"), 404);
        }

        if (leave.CounselorId != counselorId)
        {
            throw new Shared.Exceptions.BusinessException(Msg.Leave.OnlyOwnLeave, 403);
        }

        return leave;
    }

    /// <summary>联动更新考勤记录为 Leave</summary>
    private async Task UpdateAttendanceRecordsToLeaveAsync(ISqlSugarClient db, string studentId, DateTime leaveStart, DateTime leaveEnd, CancellationToken cancellationToken)
    {
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

        await db.Updateable(records).UpdateColumns(it => new { it.Status, it.Remark, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("学生 {StudentId} 请假审批通过，联动更新 {Count} 条考勤记录为 Leave", studentId, records.Count);
    }
}
}