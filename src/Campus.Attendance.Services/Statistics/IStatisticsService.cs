using Campus.Attendance.Models.Statistics;

namespace Campus.Attendance.Services.Statistics;

/// <summary>
/// 考勤统计与报表服务接口，提供全局统计、院系排名、出勤趋势、班级/课程/学生/教师维度统计与 Excel 报表导出
/// </summary>
public interface IStatisticsService
{
    /// <summary>管理员全局统计：全校出勤率、总学生数、总教师数、今日会话数、今日出勤率</summary>
    Task<OverviewStatisticsDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>院系出勤率排名</summary>
    Task<List<DepartmentRankingDto>> GetDepartmentRankingAsync(CancellationToken cancellationToken = default);

    /// <summary>异常考勤趋势（按日期分组的出勤率）</summary>
    Task<List<AttendanceTrendDto>> GetAttendanceTrendAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>班级考勤统计</summary>
    Task<ClassStatisticsDto> GetClassStatisticsAsync(long classId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>课程考勤统计</summary>
    Task<ClassStatisticsDto> GetCourseStatisticsAsync(long courseId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>学生个人统计：本学期出勤率、迟到/缺勤/请假次数、课程维度统计列表</summary>
    Task<StudentStatisticsDto> GetStudentStatisticsAsync(string studentId, CancellationToken cancellationToken = default);

    /// <summary>教师统计：总课程数、总会话数、平均出勤率</summary>
    Task<TeacherStatisticsDto> GetTeacherStatisticsAsync(string teacherId, CancellationToken cancellationToken = default);

    /// <summary>导出单个会话的考勤记录为 Excel（返回 byte[]）</summary>
    Task<byte[]> ExportAttendanceRecordsAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>导出班级考勤汇总为 Excel</summary>
    Task<byte[]> ExportClassAttendanceAsync(long classId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>导出班级学生名单为 Excel</summary>
    Task<byte[]> ExportStudentListAsync(long classId, CancellationToken cancellationToken = default);
}
