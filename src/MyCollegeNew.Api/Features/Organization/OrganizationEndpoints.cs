using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Organization;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Organization;

/// <summary>
/// 组织架构端点映射
/// </summary>
public static class OrganizationEndpoints
{
    /// <summary>
    /// 映射组织架构相关端点
    /// </summary>
    /// <param name="group">路由组</param>
    public static RouteGroupBuilder MapOrganizationEndpoints(this RouteGroupBuilder group)
    {
        // 院系
        group.MapGet("/departments", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetDepartmentsQuery());
            return Results.Ok(result);
        })
        .WithName("GetDepartments").WithSummary("查询所有院系").RequireAuthorization()
        .Produces<ApiResponse<List<DepartmentResponseDto>>>(StatusCodes.Status200OK);

        group.MapGet("/departments/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetDepartmentByIdQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetDepartmentById").WithSummary("根据Id查询院系").RequireAuthorization()
        .Produces<ApiResponse<DepartmentResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/departments", async (DepartmentCreateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateDepartmentCommand(dto));
            return Results.Ok(result);
        })
        .WithName("CreateDepartment").WithSummary("创建院系").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<DepartmentResponseDto>>(StatusCodes.Status200OK);

        group.MapPut("/departments/{id:long}", async (long id, DepartmentUpdateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateDepartmentCommand(id, dto));
            return Results.Ok(result);
        })
        .WithName("UpdateDepartment").WithSummary("更新院系").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<DepartmentResponseDto>>(StatusCodes.Status200OK);

        group.MapDelete("/departments/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteDepartmentCommand(id));
            return Results.Ok(result);
        })
        .WithName("DeleteDepartment").WithSummary("删除院系").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        // 专业（作为院系子资源）
        group.MapGet("/departments/{departmentId:long}/majors", async (long departmentId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetMajorsByDepartmentQuery(departmentId));
            return Results.Ok(result);
        })
        .WithName("GetMajorsByDepartment").WithSummary("按院系查询专业").RequireAuthorization()
        .Produces<ApiResponse<List<MajorResponseDto>>>(StatusCodes.Status200OK);

        group.MapGet("/majors/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetMajorByIdQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetMajorById").WithSummary("根据Id查询专业").RequireAuthorization()
        .Produces<ApiResponse<MajorResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/majors", async (MajorCreateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateMajorCommand(dto));
            return Results.Ok(result);
        })
        .WithName("CreateMajor").WithSummary("创建专业").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<MajorResponseDto>>(StatusCodes.Status200OK);

        group.MapPut("/majors/{id:long}", async (long id, MajorUpdateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateMajorCommand(id, dto));
            return Results.Ok(result);
        })
        .WithName("UpdateMajor").WithSummary("更新专业").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<MajorResponseDto>>(StatusCodes.Status200OK);

        group.MapDelete("/majors/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteMajorCommand(id));
            return Results.Ok(result);
        })
        .WithName("DeleteMajor").WithSummary("删除专业").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        // 班级（作为专业子资源）
        group.MapGet("/majors/{majorId:long}/classes", async (long majorId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetClassesByMajorQuery(majorId));
            return Results.Ok(result);
        })
        .WithName("GetClassesByMajor").WithSummary("按专业查询班级").RequireAuthorization()
        .Produces<ApiResponse<List<ClassResponseDto>>>(StatusCodes.Status200OK);

        group.MapGet("/classes/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetClassByIdQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetClassById").WithSummary("根据Id查询班级").RequireAuthorization()
        .Produces<ApiResponse<ClassResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/classes", async (ClassCreateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateClassCommand(dto));
            return Results.Ok(result);
        })
        .WithName("CreateClass").WithSummary("创建班级").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<ClassResponseDto>>(StatusCodes.Status200OK);

        group.MapPut("/classes/{id:long}", async (long id, ClassUpdateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateClassCommand(id, dto));
            return Results.Ok(result);
        })
        .WithName("UpdateClass").WithSummary("更新班级").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<ClassResponseDto>>(StatusCodes.Status200OK);

        group.MapDelete("/classes/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteClassCommand(id));
            return Results.Ok(result);
        })
        .WithName("DeleteClass").WithSummary("删除班级").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        // 组织树
        group.MapGet("/organization/tree", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrganizationTreeQuery());
            return Results.Ok(result);
        })
        .WithName("GetOrganizationTree").WithSummary("查询组织树").RequireAuthorization()
        .Produces<ApiResponse<List<OrganizationTreeNodeDto>>>(StatusCodes.Status200OK);

        return group;
    }
}
