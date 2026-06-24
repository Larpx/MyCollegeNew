using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses
{
    /// <summary>分页查询课程</summary>
    public record GetCoursesQuery : PagedQuery, IRequest<ApiResponse<PagedResult<CourseResponseDto>>>
    {
        /// <summary>搜索关键字</summary>
        public string? Keyword { get; init; }

        /// <summary>教师工号</summary>
        public string? TeacherId { get; init; }
    }

    /// <summary>根据Id查询课程</summary>
    public record GetCourseByIdQuery(long Id) : IRequest<ApiResponse<CourseResponseDto>>;

    /// <summary>创建课程</summary>
    public record CreateCourseCommand(CourseCreateDto Dto) : IRequest<ApiResponse<CourseResponseDto>>;

    /// <summary>更新课程</summary>
    public record UpdateCourseCommand(long Id, CourseUpdateDto Dto) : IRequest<ApiResponse<CourseResponseDto>>;

    /// <summary>删除课程</summary>
    public record DeleteCourseCommand(long Id) : IRequest<ApiResponse<object>>;

    /// <summary>按教师查询课程</summary>
    public record GetCoursesByTeacherQuery(string TeacherId) : IRequest<ApiResponse<List<CourseResponseDto>>>;

    /// <summary>分页查询课表</summary>
    public record GetSchedulesQuery : PagedQuery, IRequest<ApiResponse<PagedResult<ScheduleResponseDto>>>
    {
        /// <summary>班级ID</summary>
        public long? ClassId { get; init; }

        /// <summary>教师工号</summary>
        public string? TeacherId { get; init; }

        /// <summary>课程ID</summary>
        public long? CourseId { get; init; }
    }

    /// <summary>根据Id查询课表</summary>
    public record GetScheduleByIdQuery(long Id) : IRequest<ApiResponse<ScheduleResponseDto>>;

    /// <summary>创建课表</summary>
    public record CreateScheduleCommand(ScheduleCreateDto Dto) : IRequest<ApiResponse<ScheduleResponseDto>>;

    /// <summary>更新课表</summary>
    public record UpdateScheduleCommand(long Id, ScheduleUpdateDto Dto) : IRequest<ApiResponse<ScheduleResponseDto>>;

    /// <summary>删除课表</summary>
    public record DeleteScheduleCommand(long Id) : IRequest<ApiResponse<object>>;

    /// <summary>按教师查询周课表</summary>
    public record GetScheduleByTeacherQuery(string TeacherId, int Week) : IRequest<ApiResponse<WeeklyScheduleDto>>;

    /// <summary>按学生查询周课表</summary>
    public record GetScheduleByStudentQuery(string StudentId, int Week) : IRequest<ApiResponse<WeeklyScheduleDto>>;

    /// <summary>按班级查询周课表</summary>
    public record GetScheduleByClassQuery(int ClassId, int Week) : IRequest<ApiResponse<WeeklyScheduleDto>>;
}