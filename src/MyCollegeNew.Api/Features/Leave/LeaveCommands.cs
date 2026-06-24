using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Leave;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Leave
{
/// <summary>学生提交请假</summary>
public record CreateLeaveCommand(LeaveCreateDto Dto, string StudentId) : IRequest<ApiResponse<LeaveResponseDto>>;

/// <summary>学生分页查询请假记录</summary>
public record GetLeavesByStudentQuery : PagedQuery, IRequest<ApiResponse<PagedResult<LeaveResponseDto>>>
{
    /// <summary>学生学号</summary>
    public string StudentId { get; init; } = string.Empty;
}

/// <summary>辅导员分页查询请假记录</summary>
public record GetLeavesByCounselorQuery : PagedQuery, IRequest<ApiResponse<PagedResult<LeaveResponseDto>>>
{
    /// <summary>辅导员工号</summary>
    public string CounselorId { get; init; } = string.Empty;

    /// <summary>请假状态</summary>
    public LeaveStatus? Status { get; init; }
}

/// <summary>辅导员待审批数量</summary>
public record GetPendingLeavesCountQuery(string CounselorId) : IRequest<ApiResponse<long>>;

/// <summary>查询请假详情</summary>
public record GetLeaveByIdQuery(long Id) : IRequest<ApiResponse<LeaveResponseDto>>;

/// <summary>审批通过</summary>
public record ApproveLeaveCommand(long Id, string CounselorId, LeaveReviewDto Dto) : IRequest<ApiResponse<LeaveResponseDto>>;

/// <summary>审批驳回</summary>
public record RejectLeaveCommand(long Id, string CounselorId, LeaveReviewDto Dto) : IRequest<ApiResponse<LeaveResponseDto>>;

/// <summary>按班级查询请假记录</summary>
public record GetLeavesByClassQuery(long ClassId, DateTime StartDate, DateTime EndDate) : IRequest<ApiResponse<List<LeaveResponseDto>>>;
}