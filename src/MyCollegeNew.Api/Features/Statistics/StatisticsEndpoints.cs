using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Statistics;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Statistics
{
    /// <summary>
    /// 统计端点映射
    /// </summary>
    public static class StatisticsEndpoints
    {
        /// <summary>
        /// 映射统计相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapStatisticsEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/statistics/overview", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetOverviewQuery());
                return Results.Ok(result);
            })
            .WithName("GetOverview").WithSummary("全局统计").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<OverviewStatisticsDto>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/department-ranking", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetDepartmentRankingQuery());
                return Results.Ok(result);
            })
            .WithName("GetDepartmentRanking").WithSummary("院系出勤率排名").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<List<DepartmentRankingDto>>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/attendance-trend", async (DateTime startDate, DateTime endDate, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAttendanceTrendQuery(startDate, endDate));
                return Results.Ok(result);
            })
            .WithName("GetAttendanceTrend").WithSummary("出勤趋势").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<List<AttendanceTrendDto>>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/class/{classId:long}", async (long classId, DateTime? startDate, DateTime? endDate, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetClassStatisticsQuery(classId, startDate, endDate));
                return Results.Ok(result);
            })
            .WithName("GetClassStatistics").WithSummary("班级考勤统计").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<ClassStatisticsDto>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/course/{courseId:long}", async (long courseId, DateTime? startDate, DateTime? endDate, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCourseStatisticsQuery(courseId, startDate, endDate));
                return Results.Ok(result);
            })
            .WithName("GetCourseStatistics").WithSummary("课程考勤统计").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<ClassStatisticsDto>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/student/{studentId}", async (string studentId, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetStudentStatisticsQuery(studentId));
                return Results.Ok(result);
            })
            .WithName("GetStudentStatistics").WithSummary("学生个人统计").RequireAuthorization()
            .Produces<ApiResponse<StudentStatisticsDto>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/teacher/{teacherId}", async (string teacherId, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetTeacherStatisticsQuery(teacherId));
                return Results.Ok(result);
            })
            .WithName("GetTeacherStatistics").WithSummary("教师统计").RequireAuthorization()
            .Produces<ApiResponse<TeacherStatisticsDto>>(StatusCodes.Status200OK);

            group.MapGet("/statistics/export/session/{sessionId:long}", async (long sessionId, IMediator mediator) =>
            {
                return await mediator.Send(new ExportSessionRecordsQuery(sessionId));
            })
            .WithName("ExportSessionRecords").WithSummary("导出会话考勤记录").RequireAuthorization("RequireTeacher")
            .Produces(StatusCodes.Status200OK, typeof(FileContentResult));

            group.MapGet("/statistics/export/class/{classId:long}", async (long classId, DateTime startDate, DateTime endDate, IMediator mediator) =>
            {
                return await mediator.Send(new ExportClassAttendanceQuery(classId, startDate, endDate));
            })
            .WithName("ExportClassAttendance").WithSummary("导出班级考勤汇总").RequireAuthorization("RequireTeacher")
            .Produces(StatusCodes.Status200OK, typeof(FileContentResult));

            group.MapGet("/statistics/export/students/{classId:long}", async (long classId, IMediator mediator) =>
            {
                return await mediator.Send(new ExportStudentListQuery(classId));
            })
            .WithName("ExportStudentList").WithSummary("导出班级学生名单").RequireAuthorization("RequireTeacher")
            .Produces(StatusCodes.Status200OK, typeof(FileContentResult));

            // ===== 系主任本系统计报表 =====

            // 系主任本系教师考勤汇总
            group.MapGet("/statistics/department/{departmentId:long}/teachers/attendance-summary",
                async (long departmentId, DateTime? startDate, DateTime? endDate, IMediator mediator) =>
                {
                    var result = await mediator.Send(new GetDepartmentTeacherAttendanceSummaryQuery(departmentId, startDate, endDate));
                    return Results.Ok(result);
                })
            .WithName("GetDepartmentTeacherAttendanceSummary").WithSummary("系主任本系教师考勤汇总")
            .RequireAuthorization("RequireDepartmentHead")
            .Produces<ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>>(StatusCodes.Status200OK);

            // 系主任本系调换课统计
            group.MapGet("/statistics/department/{departmentId:long}/swaps/summary",
                async (long departmentId, DateTime? startDate, DateTime? endDate, IMediator mediator) =>
                {
                    var result = await mediator.Send(new GetDepartmentSwapSummaryQuery(departmentId, startDate, endDate));
                    return Results.Ok(result);
                })
            .WithName("GetDepartmentSwapSummary").WithSummary("系主任本系调换课统计")
            .RequireAuthorization("RequireDepartmentHead")
            .Produces<ApiResponse<DepartmentSwapSummaryDto>>(StatusCodes.Status200OK);

            // 系主任本系课程开课率
            group.MapGet("/statistics/department/{departmentId:long}/courses/coverage",
                async (long departmentId, IMediator mediator) =>
                {
                    var result = await mediator.Send(new GetDepartmentCourseCoverageQuery(departmentId));
                    return Results.Ok(result);
                })
            .WithName("GetDepartmentCourseCoverage").WithSummary("系主任本系课程开课率")
            .RequireAuthorization("RequireDepartmentHead")
            .Produces<ApiResponse<DepartmentCourseCoverageDto>>(StatusCodes.Status200OK);

            return group;
        }
    }
}