using Campus.Attendance.Shared.Features.Auth;
using Campus.Attendance.Shared.Responses;
using Campus.Attendance.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Campus.Attendance.Api.Features.Auth.Login;

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
        .Produces<ApiResponse<LoginResult>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        return group;
    }
}
