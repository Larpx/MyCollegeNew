using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses
{
    /// <summary>
    /// 接课申请端点映射
    /// </summary>
    public static class AssignmentEndpoints
    {
        /// <summary>
        /// 映射接课申请相关端点（教师接课、系主任查询待审批、系主任审批）
        /// </summary>
        /// <param name="group">路由组</param>
        /// <returns>路由组</returns>
        public static RouteGroupBuilder MapAssignmentEndpoints(this RouteGroupBuilder group)
        {
            // 教师接课申请：仅教师身份可调用，路径 POST /api/v1/courses/assignments
            group.MapPost("/courses/assignments", AssignmentHandlers.ApplyAsync)
                .WithName("ApplyAssignment").WithSummary("教师接课申请")
                .RequireAuthorization("RequireTeacher")
                .Produces<ApiResponse<AssignmentResponseDto>>(StatusCodes.Status200OK);

            // 查询待审批接课列表：仅系主任可调用，路径 GET /api/v1/courses/assignments/pending
            group.MapGet("/courses/assignments/pending", AssignmentHandlers.GetPendingAsync)
                .WithName("GetPendingAssignments").WithSummary("查询待审批接课列表")
                .RequireAuthorization("RequireDepartmentHead")
                .Produces<ApiResponse<List<AssignmentResponseDto>>>(StatusCodes.Status200OK);

            // 系主任审批接课申请：路径 POST /api/v1/courses/assignments/{assignmentId}/review
            group.MapPost("/courses/assignments/{assignmentId:long}/review", AssignmentHandlers.ReviewAsync)
                .WithName("ReviewAssignment").WithSummary("审批接课申请")
                .RequireAuthorization("RequireDepartmentHead")
                .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            return group;
        }
    }
}
