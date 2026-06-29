using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemUsers
{
    /// <summary>
    /// 系统用户端点映射
    /// </summary>
    public static class SystemUserEndpoints
    {
        /// <summary>
        /// 映射系统用户相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapSystemUserEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/system-users", async ([AsParameters] GetSystemUsersQuery query, IMediator mediator) =>
            {
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("GetSystemUsers")
            .WithSummary("分页查询系统用户列表")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<PagedResult<SystemUserResponseDto>>>(StatusCodes.Status200OK);

            group.MapGet("/system-users/{id:long}", async (long id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSystemUserByIdQuery(id));
                return Results.Ok(result);
            })
            .WithName("GetSystemUserById")
            .WithSummary("根据 Id 查询系统用户")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<SystemUserResponseDto>>(StatusCodes.Status200OK);

            group.MapPost("/system-users", async (SystemUserCreateDto dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new CreateSystemUserCommand(dto));
                return Results.Ok(result);
            })
            .WithName("CreateSystemUser")
            .WithSummary("创建系统用户")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<SystemUserResponseDto>>(StatusCodes.Status200OK);

            group.MapPut("/system-users/{id:long}", async (long id, SystemUserUpdateDto dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new UpdateSystemUserCommand(id, dto));
                return Results.Ok(result);
            })
            .WithName("UpdateSystemUser")
            .WithSummary("更新系统用户")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<SystemUserResponseDto>>(StatusCodes.Status200OK);

            group.MapDelete("/system-users/{id:long}", async (long id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteSystemUserCommand(id));
                return Results.Ok(result);
            })
            .WithName("DeleteSystemUser")
            .WithSummary("删除系统用户")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            group.MapPost("/system-users/{id:long}/reset-password", async (long id, ResetPasswordDto dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new ResetSystemUserPasswordCommand(id, dto));
                return Results.Ok(result);
            })
            .WithName("ResetSystemUserPassword")
            .WithSummary("重置系统用户密码")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            return group;
        }
    }
}
