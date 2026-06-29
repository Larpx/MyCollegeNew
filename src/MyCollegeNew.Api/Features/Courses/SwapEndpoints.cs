using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses
{
    /// <summary>
    /// 调换课申请端点映射
    /// </summary>
    public static class SwapEndpoints
    {
        /// <summary>
        /// 映射调换课申请相关端点（发起调换、查询个人申请、代课确认、撤销）
        /// </summary>
        /// <param name="group">路由组</param>
        /// <returns>路由组</returns>
        public static RouteGroupBuilder MapSwapEndpoints(this RouteGroupBuilder group)
        {
            // 使用子路由组统一打标签，路径前缀 /api/v1/courses/swaps
            var swap = group.MapGroup("/courses/swaps").WithTags("调换课");

            // 发起调换课申请：仅教师可调用，路径 POST /api/v1/courses/swaps
            swap.MapPost("/", SwapHandlers.CreateAsync)
                .WithName("CreateSwapRequest").WithSummary("发起调换课申请")
                .RequireAuthorization("RequireTeacher")
                .Produces<ApiResponse<SwapRequestResponseDto>>(StatusCodes.Status200OK);

            // 查询我相关的调换课申请：仅教师可调用，路径 GET /api/v1/courses/swaps?role=initiator|substitute
            swap.MapGet("/", SwapHandlers.GetMyAsync)
                .WithName("GetMySwapRequests").WithSummary("查询我相关的调换课申请")
                .RequireAuthorization("RequireTeacher")
                .Produces<ApiResponse<List<SwapRequestResponseDto>>>(StatusCodes.Status200OK);

            // 代课教师确认或拒绝调换课申请：路径 POST /api/v1/courses/swaps/{swapId}/confirm
            swap.MapPost("/{swapId:long}/confirm", SwapHandlers.ConfirmAsync)
                .WithName("ConfirmSwapRequest").WithSummary("代课教师确认或拒绝调换课")
                .RequireAuthorization("RequireTeacher")
                .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            // 原任课教师撤销调换课申请：路径 DELETE /api/v1/courses/swaps/{swapId}
            swap.MapDelete("/{swapId:long}", SwapHandlers.CancelAsync)
                .WithName("CancelSwapRequest").WithSummary("撤销调换课申请")
                .RequireAuthorization("RequireTeacher")
                .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            return group;
        }
    }
}
