using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login
{
    /// <summary>
    /// 登录端点映射
    /// </summary>
    public static class LoginEndpoint
    {
        /// <summary>
        /// 映射登录相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/login", async (LoginRequest request, IMediator mediator) =>
            {
                var result = await mediator.Send(new LoginCommand(request));
                return Results.Ok(result);
            })
            .WithName("Login")
            .WithSummary("用户登录")
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .Produces<ApiResponse<LoginResult>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);

            return group;
        }
    }
}