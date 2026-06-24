using Campus.Attendance.Shared.Contracts;
using Campus.Attendance.Shared.Features.Courses;
using Campus.Attendance.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Campus.Attendance.Api.Features.Courses;

/// <summary>
/// 课程与课表端点映射
/// </summary>
public static class CourseEndpoints
{
    /// <summary>
    /// 映射课程与课表相关端点
    /// </summary>
    /// <param name="group">路由组</param>
    public static RouteGroupBuilder MapCourseEndpoints(this RouteGroupBuilder group)
    {
        // 课程
        group.MapGet("/courses", async ([AsParameters] GetCoursesQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetCourses").WithSummary("分页查询课程").RequireAuthorization()
        .Produces<ApiResponse<PagedResult<CourseResponseDto>>>(StatusCodes.Status200OK);

        group.MapGet("/courses/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCourseByIdQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetCourseById").WithSummary("根据Id查询课程").RequireAuthorization()
        .Produces<ApiResponse<CourseResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/courses", async (CourseCreateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateCourseCommand(dto));
            return Results.Ok(result);
        })
        .WithName("CreateCourse").WithSummary("创建课程").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<CourseResponseDto>>(StatusCodes.Status200OK);

        group.MapPut("/courses/{id:long}", async (long id, CourseUpdateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateCourseCommand(id, dto));
            return Results.Ok(result);
        })
        .WithName("UpdateCourse").WithSummary("更新课程").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<CourseResponseDto>>(StatusCodes.Status200OK);

        group.MapDelete("/courses/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteCourseCommand(id));
            return Results.Ok(result);
        })
        .WithName("DeleteCourse").WithSummary("删除课程").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        group.MapGet("/courses/by-teacher/{teacherId}", async (string teacherId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCoursesByTeacherQuery(teacherId));
            return Results.Ok(result);
        })
        .WithName("GetCoursesByTeacher").WithSummary("按教师查询课程").RequireAuthorization()
        .Produces<ApiResponse<List<CourseResponseDto>>>(StatusCodes.Status200OK);

        // 课表
        group.MapGet("/schedules", async ([AsParameters] GetSchedulesQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetSchedules").WithSummary("分页查询课表").RequireAuthorization()
        .Produces<ApiResponse<PagedResult<ScheduleResponseDto>>>(StatusCodes.Status200OK);

        group.MapGet("/schedules/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetScheduleByIdQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetScheduleById").WithSummary("根据Id查询课表").RequireAuthorization()
        .Produces<ApiResponse<ScheduleResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/schedules", async (ScheduleCreateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateScheduleCommand(dto));
            return Results.Ok(result);
        })
        .WithName("CreateSchedule").WithSummary("创建课表").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<ScheduleResponseDto>>(StatusCodes.Status200OK);

        group.MapPut("/schedules/{id:long}", async (long id, ScheduleUpdateDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateScheduleCommand(id, dto));
            return Results.Ok(result);
        })
        .WithName("UpdateSchedule").WithSummary("更新课表").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<ScheduleResponseDto>>(StatusCodes.Status200OK);

        group.MapDelete("/schedules/{id:long}", async (long id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteScheduleCommand(id));
            return Results.Ok(result);
        })
        .WithName("DeleteSchedule").WithSummary("删除课表").RequireAuthorization("RequireAdmin")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        // 周课表
        group.MapGet("/schedules/weekly/teacher/{teacherId}", async (string teacherId, [AsParameters] int week, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetScheduleByTeacherQuery(teacherId, week));
            return Results.Ok(result);
        })
        .WithName("GetScheduleByTeacher").WithSummary("按教师查询周课表").RequireAuthorization()
        .Produces<ApiResponse<WeeklyScheduleDto>>(StatusCodes.Status200OK);

        group.MapGet("/schedules/weekly/student/{studentId}", async (string studentId, [AsParameters] int week, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetScheduleByStudentQuery(studentId, week));
            return Results.Ok(result);
        })
        .WithName("GetScheduleByStudent").WithSummary("按学生查询周课表").RequireAuthorization()
        .Produces<ApiResponse<WeeklyScheduleDto>>(StatusCodes.Status200OK);

        group.MapGet("/schedules/weekly/class/{classId:int}", async (int classId, [AsParameters] int week, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetScheduleByClassQuery(classId, week));
            return Results.Ok(result);
        })
        .WithName("GetScheduleByClass").WithSummary("按班级查询周课表").RequireAuthorization()
        .Produces<ApiResponse<WeeklyScheduleDto>>(StatusCodes.Status200OK);

        return group;
    }
}
