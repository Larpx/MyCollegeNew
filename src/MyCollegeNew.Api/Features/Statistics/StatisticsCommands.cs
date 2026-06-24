using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Statistics;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Statistics
{
    /// <summary>全局统计</summary>
    public record GetOverviewQuery : IRequest<ApiResponse<OverviewStatisticsDto>>;

    /// <summary>院系出勤率排名</summary>
    public record GetDepartmentRankingQuery : IRequest<ApiResponse<List<DepartmentRankingDto>>>;

    /// <summary>出勤趋势</summary>
    public record GetAttendanceTrendQuery(DateTime StartDate, DateTime EndDate) : IRequest<ApiResponse<List<AttendanceTrendDto>>>;

    /// <summary>班级考勤统计</summary>
    public record GetClassStatisticsQuery(long ClassId, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<ApiResponse<ClassStatisticsDto>>;

    /// <summary>课程考勤统计</summary>
    public record GetCourseStatisticsQuery(long CourseId, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<ApiResponse<ClassStatisticsDto>>;

    /// <summary>学生个人统计</summary>
    public record GetStudentStatisticsQuery(string StudentId) : IRequest<ApiResponse<StudentStatisticsDto>>;

    /// <summary>教师统计</summary>
    public record GetTeacherStatisticsQuery(string TeacherId) : IRequest<ApiResponse<TeacherStatisticsDto>>;

    /// <summary>导出会话考勤记录</summary>
    public record ExportSessionRecordsQuery(long SessionId) : IRequest<IResult>;

    /// <summary>导出班级考勤汇总</summary>
    public record ExportClassAttendanceQuery(long ClassId, DateTime StartDate, DateTime EndDate) : IRequest<IResult>;

    /// <summary>导出班级学生名单</summary>
    public record ExportStudentListQuery(long ClassId) : IRequest<IResult>;
}