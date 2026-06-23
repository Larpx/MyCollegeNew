using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Courses;

namespace Campus.Attendance.Services.Courses;

/// <summary>
/// 课表管理服务接口，封装课表的增删改查与按教师/学生/班级的周课表查询
/// </summary>
public interface IScheduleService
{
    /// <summary>分页查询课表，支持班级、教师、课程过滤</summary>
    Task<PagedResult<ScheduleResponseDto>> GetSchedulesAsync(
        int pageIndex, int pageSize, long? classId = null, string? teacherId = null, long? courseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>查询课表详情</summary>
    Task<ScheduleResponseDto?> GetScheduleByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>创建排课（班级-课程-教师-周次-节次）</summary>
    Task<ScheduleResponseDto> CreateScheduleAsync(ScheduleCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>更新课表</summary>
    Task<ScheduleResponseDto> UpdateScheduleAsync(long id, ScheduleUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>软删除课表</summary>
    Task DeleteScheduleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按教师查询某周课表（返回按星期分组的课表）</summary>
    Task<WeeklyScheduleDto> GetScheduleByTeacherAsync(string teacherId, int week, CancellationToken cancellationToken = default);

    /// <summary>按学生查询某周课表（通过班级关联）</summary>
    Task<WeeklyScheduleDto> GetScheduleByStudentAsync(string studentId, int week, CancellationToken cancellationToken = default);

    /// <summary>按班级查询某周课表</summary>
    Task<WeeklyScheduleDto> GetScheduleByClassAsync(int classId, int week, CancellationToken cancellationToken = default);
}
