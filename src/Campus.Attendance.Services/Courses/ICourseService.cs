using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Courses;

namespace Campus.Attendance.Services.Courses;

/// <summary>
/// 课程管理服务接口，封装课程的增删改查与按教师/班级查询
/// </summary>
public interface ICourseService
{
    /// <summary>分页查询课程，支持关键字与教师过滤</summary>
    Task<PagedResult<CourseResponseDto>> GetCoursesAsync(
        int pageIndex, int pageSize, string? keyword = null, string? teacherId = null,
        CancellationToken cancellationToken = default);

    /// <summary>查询课程详情（含教师姓名）</summary>
    Task<CourseResponseDto?> GetCourseByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>创建课程</summary>
    Task<CourseResponseDto> CreateCourseAsync(CourseCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>更新课程</summary>
    Task<CourseResponseDto> UpdateCourseAsync(long id, CourseUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>软删除课程</summary>
    Task DeleteCourseAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按教师查询课程</summary>
    Task<List<CourseResponseDto>> GetCoursesByTeacherAsync(string teacherId, CancellationToken cancellationToken = default);

    /// <summary>按班级查询课程（通过课表关联）</summary>
    Task<List<CourseResponseDto>> GetCoursesByClassAsync(int classId, CancellationToken cancellationToken = default);
}
