using System.ComponentModel.DataAnnotations;

namespace Campus.Attendance.Models.Courses;

/// <summary>
/// 课表创建 DTO，描述某课程在某班级的周次与节次安排
/// </summary>
public class ScheduleCreateDto
{
    /// <summary>课程 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
    public long CourseId { get; set; }

    /// <summary>班级 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    /// <summary>任课教师工号（关联 Teacher.Id）</summary>
    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>星期几（1=周一, 7=周日）</summary>
    [Range(1, 7, ErrorMessage = "星期几范围无效（1-7）")]
    public int DayOfWeek { get; set; }

    /// <summary>起始节次</summary>
    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int StartSection { get; set; }

    /// <summary>结束节次</summary>
    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int EndSection { get; set; }

    /// <summary>起始周次</summary>
    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int StartWeek { get; set; }

    /// <summary>结束周次</summary>
    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int EndWeek { get; set; }

    /// <summary>教室</summary>
    [Required(ErrorMessage = "教室不能为空")]
    [StringLength(64, ErrorMessage = "教室长度不能超过 64 个字符")]
    public string Classroom { get; set; } = string.Empty;
}

/// <summary>
/// 课表更新 DTO
/// </summary>
public class ScheduleUpdateDto
{
    /// <summary>课程 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
    public long CourseId { get; set; }

    /// <summary>班级 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    /// <summary>任课教师工号</summary>
    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>星期几（1=周一, 7=周日）</summary>
    [Range(1, 7, ErrorMessage = "星期几范围无效（1-7）")]
    public int DayOfWeek { get; set; }

    /// <summary>起始节次</summary>
    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int StartSection { get; set; }

    /// <summary>结束节次</summary>
    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int EndSection { get; set; }

    /// <summary>起始周次</summary>
    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int StartWeek { get; set; }

    /// <summary>结束周次</summary>
    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int EndWeek { get; set; }

    /// <summary>教室</summary>
    [Required(ErrorMessage = "教室不能为空")]
    [StringLength(64, ErrorMessage = "教室长度不能超过 64 个字符")]
    public string Classroom { get; set; } = string.Empty;
}

/// <summary>
/// 课表响应 DTO，包含课程名称、班级名称与教师姓名等关联信息
/// </summary>
public class ScheduleResponseDto
{
    /// <summary>课表 Id</summary>
    public long Id { get; set; }

    /// <summary>课程 Id</summary>
    public long CourseId { get; set; }

    /// <summary>课程名称</summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>班级 Id</summary>
    public long ClassId { get; set; }

    /// <summary>班级名称</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>任课教师工号</summary>
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>任课教师姓名</summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>星期几（1=周一, 7=周日）</summary>
    public int DayOfWeek { get; set; }

    /// <summary>起始节次</summary>
    public int StartSection { get; set; }

    /// <summary>结束节次</summary>
    public int EndSection { get; set; }

    /// <summary>起始周次</summary>
    public int StartWeek { get; set; }

    /// <summary>结束周次</summary>
    public int EndWeek { get; set; }

    /// <summary>教室</summary>
    public string Classroom { get; set; } = string.Empty;
}

/// <summary>
/// 周课表 DTO，按星期几分组的课表列表
/// </summary>
public class WeeklyScheduleDto
{
    /// <summary>查询的周次</summary>
    public int Week { get; set; }

    /// <summary>按星期几分组的课表列表（键为 DayOfWeek 1-7）</summary>
    public Dictionary<int, List<ScheduleResponseDto>> Days { get; set; } = new();
}
