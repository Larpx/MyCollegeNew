namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Statistics
{
    /// <summary>全局统计概览 DTO</summary>
    public class OverviewStatisticsDto
    {
        public long TotalStudents { get; set; }
        public long TotalTeachers { get; set; }
        public long TodaySessions { get; set; }
        public double OverallAttendanceRate { get; set; }
        public double TodayAttendanceRate { get; set; }
    }

    /// <summary>院系出勤率排名 DTO</summary>
    public class DepartmentRankingDto
    {
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public double AttendanceRate { get; set; }
        public long StudentCount { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>出勤趋势 DTO</summary>
    public class AttendanceTrendDto
    {
        public DateTime Date { get; set; }
        public double AttendanceRate { get; set; }
        public long LateCount { get; set; }
        public long AbsentCount { get; set; }
        public long LeaveCount { get; set; }
    }

    /// <summary>班级考勤统计 DTO</summary>
    public class ClassStatisticsDto
    {
        public long ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public long TotalSessions { get; set; }
        public double AttendanceRate { get; set; }
        public long LateCount { get; set; }
        public long AbsentCount { get; set; }
        public long LeaveCount { get; set; }
    }

    /// <summary>学生个人考勤统计 DTO</summary>
    public class StudentStatisticsDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public long TotalSessions { get; set; }
        public long PresentCount { get; set; }
        public long LateCount { get; set; }
        public long AbsentCount { get; set; }
        public long LeaveCount { get; set; }
        public double AttendanceRate { get; set; }
        public List<CourseStatisticsItemDto> CourseStatistics { get; set; } = new();
    }

    /// <summary>课程维度考勤统计项 DTO</summary>
    public class CourseStatisticsItemDto
    {
        public long CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public long TotalSessions { get; set; }
        public double AttendanceRate { get; set; }
    }

    /// <summary>教师考勤统计 DTO</summary>
    public class TeacherStatisticsDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public long TotalCourses { get; set; }
        public long TotalSessions { get; set; }
        public double AverageAttendanceRate { get; set; }
    }
}