using Campus.Attendance.Shared.Features.Users;
using Campus.Attendance.Shared.Responses;
using Campus.Attendance.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Campus.Attendance.Api.Features.Profile.ChangePassword;

/// <summary>
/// 修改密码端点映射
/// </summary>
public static class ChangePasswordEndpoint
{
    /// <summary>
    /// 映射修改密码端点
    /// </summary>
    /// <param name="group">路由组</param>
    public static RouteGroupBuilder MapChangePasswordEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/profile/password", async (PasswordChangeDto dto, IMediator mediator, ICurrentUser currentUser) =>
        {
            var result = await mediator.Send(new ChangePasswordCommand(dto, currentUser.UserId, currentUser.Role));
            return Results.Ok(result);
        })
        .WithName("ChangePassword")
        .WithSummary("修改密码")
        .RequireAuthorization()
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        return group;
    }
}
