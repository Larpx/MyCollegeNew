using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Leave;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Leave
{
    /// <summary>
    /// 请假端点映射
    /// </summary>
    public static class LeaveEndpoints
    {
        /// <summary>
        /// 映射请假相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapLeaveEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/leaves", async (LeaveCreateDto dto, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new CreateLeaveCommand(dto, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("CreateLeave").WithSummary("学生提交请假").RequireAuthorization("RequireStudent")
            .Produces<ApiResponse<LeaveResponseDto>>(StatusCodes.Status200OK);

            group.MapGet("/leaves/my", async ([AsParameters] GetLeavesByStudentQuery query, IMediator mediator, ICurrentUser currentUser) =>
            {
                var actualQuery = query with { StudentId = currentUser.UserId };
                var result = await mediator.Send(actualQuery);
                return Results.Ok(result);
            })
            .WithName("GetMyLeaves").WithSummary("学生查询自己的请假记录").RequireAuthorization("RequireStudent")
            .Produces<ApiResponse<PagedResult<LeaveResponseDto>>>(StatusCodes.Status200OK);

            group.MapGet("/leaves/counselor", async ([AsParameters] GetLeavesByCounselorQuery query, IMediator mediator, ICurrentUser currentUser) =>
            {
                var actualQuery = query with { CounselorId = currentUser.UserId };
                var result = await mediator.Send(actualQuery);
                return Results.Ok(result);
            })
            .WithName("GetCounselorLeaves").WithSummary("辅导员查询请假记录").RequireAuthorization("RequireCounselor")
            .Produces<ApiResponse<PagedResult<LeaveResponseDto>>>(StatusCodes.Status200OK);

            group.MapGet("/leaves/pending-count", async (IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new GetPendingLeavesCountQuery(currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("GetPendingLeavesCount").WithSummary("辅导员待审批数量").RequireAuthorization("RequireCounselor")
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK);

            group.MapGet("/leaves/{id:long}", async (long id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetLeaveByIdQuery(id));
                return Results.Ok(result);
            })
            .WithName("GetLeaveById").WithSummary("查询请假详情").RequireAuthorization()
            .Produces<ApiResponse<LeaveResponseDto>>(StatusCodes.Status200OK);

            group.MapPost("/leaves/{id:long}/approve", async (long id, LeaveReviewDto dto, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new ApproveLeaveCommand(id, currentUser.UserId, dto));
                return Results.Ok(result);
            })
            .WithName("ApproveLeave").WithSummary("审批通过").RequireAuthorization("RequireCounselor")
            .Produces<ApiResponse<LeaveResponseDto>>(StatusCodes.Status200OK);

            group.MapPost("/leaves/{id:long}/reject", async (long id, LeaveReviewDto dto, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new RejectLeaveCommand(id, currentUser.UserId, dto));
                return Results.Ok(result);
            })
            .WithName("RejectLeave").WithSummary("审批驳回").RequireAuthorization("RequireCounselor")
            .Produces<ApiResponse<LeaveResponseDto>>(StatusCodes.Status200OK);

            group.MapGet("/classes/{classId:long}/leaves", async (long classId, DateTime startDate, DateTime endDate, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetLeavesByClassQuery(classId, startDate, endDate));
                return Results.Ok(result);
            })
            .WithName("GetLeavesByClass").WithSummary("按班级查询请假记录").RequireAuthorization()
            .Produces<ApiResponse<List<LeaveResponseDto>>>(StatusCodes.Status200OK);

            return group;
        }
    }
}