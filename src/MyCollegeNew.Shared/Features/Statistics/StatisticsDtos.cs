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

    /// <summary>系主任本系教师考勤汇总 DTO</summary>
    public class DepartmentTeacherAttendanceSummaryDto
    {
        /// <summary>教师工号</summary>
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>教师姓名</summary>
        public string TeacherName { get; set; } = string.Empty;

        /// <summary>发起会话数</summary>
        public int SessionCount { get; set; }

        /// <summary>应到人次</summary>
        public int ExpectedCount { get; set; }

        /// <summary>实到人次（含迟到）</summary>
        public int PresentCount { get; set; }

        /// <summary>请假人次</summary>
        public int LeaveCount { get; set; }

        /// <summary>缺勤人次</summary>
        public int AbsentCount { get; set; }

        /// <summary>出勤率（百分比，0~100）</summary>
        public double AttendanceRate { get; set; }
    }

    /// <summary>系主任本系调换课统计 DTO</summary>
    public class DepartmentSwapSummaryDto
    {
        /// <summary>总申请数</summary>
        public int TotalCount { get; set; }

        /// <summary>待确认数</summary>
        public int PendingCount { get; set; }

        /// <summary>已生效数</summary>
        public int AcceptedCount { get; set; }

        /// <summary>已拒绝数</summary>
        public int RejectedCount { get; set; }

        /// <summary>已撤销数</summary>
        public int CancelledCount { get; set; }

        /// <summary>已逾期撤销数（Pending 且超过 SLA 截止时间）</summary>
        public int ExpiredCount { get; set; }

        /// <summary>涉及教师明细列表</summary>
        public List<TeacherSwapStatDto> TeacherStats { get; set; } = new();
    }

    /// <summary>系主任本系教师调换课明细 DTO</summary>
    public class TeacherSwapStatDto
    {
        /// <summary>教师工号</summary>
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>教师姓名</summary>
        public string TeacherName { get; set; } = string.Empty;

        /// <summary>作为原任课教师发起的调换课数</summary>
        public int InitiatedCount { get; set; }

        /// <summary>作为代课教师被委托的调换课数</summary>
        public int SubstitutedCount { get; set; }
    }

    /// <summary>系主任本系课程开课率 DTO</summary>
    public class DepartmentCourseCoverageDto
    {
        /// <summary>总课程数（系主任所辖院系下教师承接的课程数）</summary>
        public int TotalCourseCount { get; set; }

        /// <summary>已排课课程数</summary>
        public int ScheduledCourseCount { get; set; }

        /// <summary>未排课课程数</summary>
        public int UnscheduledCourseCount { get; set; }

        /// <summary>开课率（百分比，0~100）</summary>
        public double CoverageRate { get; set; }

        /// <summary>班级维度开课明细列表</summary>
        public List<ClassCoverageDto> ClassCoverage { get; set; } = new();
    }

    /// <summary>系主任本系班级开课明细 DTO</summary>
    public class ClassCoverageDto
    {
        /// <summary>班级 Id</summary>
        public long ClassId { get; set; }

        /// <summary>班级名称</summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>已排课程数</summary>
        public int ScheduledCourseCount { get; set; }

        /// <summary>周课时数（按周次范围内排课条目折算的总节次）</summary>
        public int WeeklySessionCount { get; set; }
    }
}