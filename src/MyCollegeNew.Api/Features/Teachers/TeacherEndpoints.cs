using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers
{
/// <summary>
/// 教师端点映射
/// </summary>
public static class TeacherEndpoints
{
    /// <summary>
    /// 映射教师相关端点
    /// </summary>
    /// <param name="group">路由组</param>
    public static RouteGroupBuilder MapTeacherEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/teachers", async ([AsParameters] GetTeachersQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetTeachers")
        .WithSummary("分页查询教师列表")
        .RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<PagedResult<TeacherResponseDto>>>(StatusCodes.Status200OK);

        group.MapGet("/teachers/{id}", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetTeacherByIdQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetTeacherById")
        .WithSummary("根据工号查询教师")
        .RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<TeacherResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/teachers", async (TeacherCreateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateTeacherCommand(dto));
            return Results.Ok(result);
        })
        .WithName("CreateTeacher")
        .WithSummary("创建教师")
        .RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<TeacherResponseDto>>(StatusCodes.Status200OK);

        group.MapPut("/teachers/{id}", async (string id, TeacherUpdateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateTeacherCommand(id, dto));
            return Results.Ok(result);
        })
        .WithName("UpdateTeacher")
        .WithSummary("更新教师")
        .RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<TeacherResponseDto>>(StatusCodes.Status200OK);

        group.MapDelete("/teachers/{id}", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteTeacherCommand(id));
            return Results.Ok(result);
        })
        .WithName("DeleteTeacher")
        .WithSummary("删除教师")
        .RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        return group;
    }
}
}