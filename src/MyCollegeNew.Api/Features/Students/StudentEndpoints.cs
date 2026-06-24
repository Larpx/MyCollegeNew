using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Students
{
    /// <summary>
    /// 学生端点映射
    /// </summary>
    public static class StudentEndpoints
    {
        /// <summary>
        /// 映射学生相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapStudentEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/students", async ([AsParameters] GetStudentsQuery query, IMediator mediator) =>
            {
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("GetStudents")
            .WithSummary("分页查询学生列表")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<PagedResult<StudentResponseDto>>>(StatusCodes.Status200OK);

            group.MapGet("/students/{id}", async (string id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetStudentByIdQuery(id));
                return Results.Ok(result);
            })
            .WithName("GetStudentById")
            .WithSummary("根据学号查询学生")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<StudentResponseDto>>(StatusCodes.Status200OK);

            group.MapPost("/students", async (StudentCreateDto dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new CreateStudentCommand(dto));
                return Results.Ok(result);
            })
            .WithName("CreateStudent")
            .WithSummary("创建学生")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<StudentResponseDto>>(StatusCodes.Status200OK);

            group.MapPut("/students/{id}", async (string id, StudentUpdateDto dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new UpdateStudentCommand(id, dto));
                return Results.Ok(result);
            })
            .WithName("UpdateStudent")
            .WithSummary("更新学生")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<StudentResponseDto>>(StatusCodes.Status200OK);

            group.MapDelete("/students/{id}", async (string id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteStudentCommand(id));
                return Results.Ok(result);
            })
            .WithName("DeleteStudent")
            .WithSummary("删除学生")
            .RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            group.MapPost("/students/import", async (IFormFile file, IMediator mediator) =>
            {
                using var stream = file.OpenReadStream();
                var result = await mediator.Send(new BatchImportStudentsCommand(stream));
                return Results.Ok(result);
            })
            .WithName("BatchImportStudents")
            .WithSummary("批量导入学生（CSV）")
            .RequireAuthorization("RequireAdmin")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ApiResponse<BatchImportResultDto>>(StatusCodes.Status200OK);

            return group;
        }
    }
}