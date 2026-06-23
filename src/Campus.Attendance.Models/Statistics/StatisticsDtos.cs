namespace Campus.Attendance.Models.Statistics;

/// <summary>
/// 全局统计概览 DTO，供管理员首页展示
/// </summary>
public class OverviewStatisticsDto
{
    /// <summary>全校学生总数</summary>
    public long TotalStudents { get; set; }

    /// <summary>全校教师总数</summary>
    public long TotalTeachers { get; set; }

    /// <summary>今日考勤会话数</summary>
    public long TodaySessions { get; set; }

    /// <summary>全校历史出勤率（百分比，0-100）</summary>
    public double OverallAttendanceRate { get; set; }

    /// <summary>今日出勤率（百分比，0-100）</summary>
    public double TodayAttendanceRate { get; set; }
}

/// <summary>
/// 院系出勤率排名 DTO
/// </summary>
public class DepartmentRankingDto
{
    /// <summary>院系 Id</summary>
    public long DepartmentId { get; set; }

    /// <summary>院系名称</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>出勤率（百分比，0-100）</summary>
    public double AttendanceRate { get; set; }

    /// <summary>该院系学生总数</summary>
    public long StudentCount { get; set; }

    /// <summary>排名（从 1 开始）</summary>
    public int Rank { get; set; }
}

/// <summary>
/// 出勤趋势单日数据 DTO
/// </summary>
public class AttendanceTrendDto
{
    /// <summary>日期（UTC 日期）</summary>
    public DateTime Date { get; set; }

    /// <summary>当日出勤率（百分比，0-100）</summary>
    public double AttendanceRate { get; set; }

    /// <summary>迟到人数</summary>
    public long LateCount { get; set; }

    /// <summary>缺勤人数</summary>
    public long AbsentCount { get; set; }

    /// <summary>请假人数</summary>
    public long LeaveCount { get; set; }
}

/// <summary>
/// 班级考勤统计 DTO
/// </summary>
public class ClassStatisticsDto
{
    /// <summary>班级 Id</summary>
    public long ClassId { get; set; }

    /// <summary>班级名称</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>统计区间内会话总数</summary>
    public long TotalSessions { get; set; }

    /// <summary>出勤率（百分比，0-100）</summary>
    public double AttendanceRate { get; set; }

    /// <summary>迟到次数</summary>
    public long LateCount { get; set; }

    /// <summary>缺勤次数</summary>
    public long AbsentCount { get; set; }

    /// <summary>请假次数</summary>
    public long LeaveCount { get; set; }
}

/// <summary>
/// 学生个人考勤统计 DTO
/// </summary>
public class StudentStatisticsDto
{
    /// <summary>学生学号</summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>学生姓名</summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>统计区间内会话总数</summary>
    public long TotalSessions { get; set; }

    /// <summary>正常出勤次数</summary>
    public long PresentCount { get; set; }

    /// <summary>迟到次数</summary>
    public long LateCount { get; set; }

    /// <summary>缺勤次数</summary>
    public long AbsentCount { get; set; }

    /// <summary>请假次数</summary>
    public long LeaveCount { get; set; }

    /// <summary>出勤率（百分比，0-100）</summary>
    public double AttendanceRate { get; set; }

    /// <summary>课程维度统计列表</summary>
    public List<CourseStatisticsItemDto> CourseStatistics { get; set; } = new();
}

/// <summary>
/// 课程维度考勤统计项 DTO
/// </summary>
public class CourseStatisticsItemDto
{
    /// <summary>课程 Id</summary>
    public long CourseId { get; set; }

    /// <summary>课程名称</summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>该课程会话总数</summary>
    public long TotalSessions { get; set; }

    /// <summary>出勤率（百分比，0-100）</summary>
    public double AttendanceRate { get; set; }
}

/// <summary>
/// 教师考勤统计 DTO
/// </summary>
public class TeacherStatisticsDto
{
    /// <summary>教师工号</summary>
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>教师姓名</summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>总课程数</summary>
    public long TotalCourses { get; set; }

    /// <summary>总会话数</summary>
    public long TotalSessions { get; set; }

    /// <summary>平均出勤率（百分比，0-100）</summary>
    public double AverageAttendanceRate { get; set; }
}
