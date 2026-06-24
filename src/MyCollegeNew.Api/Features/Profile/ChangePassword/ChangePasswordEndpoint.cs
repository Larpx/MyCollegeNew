using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Profile.ChangePassword
{
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
}