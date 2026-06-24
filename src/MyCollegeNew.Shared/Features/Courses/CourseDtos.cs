using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;

/// <summary>课程创建 DTO</summary>
public class CourseCreateDto
{
    [Required(ErrorMessage = "课程名称不能为空")]
    [StringLength(64, ErrorMessage = "课程名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    [Range(0.5, 10, ErrorMessage = "学分范围无效（0.5-10）")]
    public decimal Credit { get; set; }

    [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
    public string? Remark { get; set; }
}

/// <summary>课程更新 DTO</summary>
public class CourseUpdateDto
{
    [Required(ErrorMessage = "课程名称不能为空")]
    [StringLength(64, ErrorMessage = "课程名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    [Range(0.5, 10, ErrorMessage = "学分范围无效（0.5-10）")]
    public decimal Credit { get; set; }

    [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
    public string? Remark { get; set; }
}

/// <summary>课程响应 DTO</summary>
public class CourseResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public decimal Credit { get; set; }
    public string? Remark { get; set; }
}

/// <summary>课表创建 DTO</summary>
public class ScheduleCreateDto
{
    [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
    public long CourseId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    [Range(1, 7, ErrorMessage = "星期几范围无效（1-7）")]
    public int DayOfWeek { get; set; }

    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int StartSection { get; set; }

    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int EndSection { get; set; }

    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int StartWeek { get; set; }

    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int EndWeek { get; set; }

    [Required(ErrorMessage = "教室不能为空")]
    [StringLength(64, ErrorMessage = "教室长度不能超过 64 个字符")]
    public string Classroom { get; set; } = string.Empty;
}

/// <summary>课表更新 DTO</summary>
public class ScheduleUpdateDto
{
    [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
    public long CourseId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    [Range(1, 7, ErrorMessage = "星期几范围无效（1-7）")]
    public int DayOfWeek { get; set; }

    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int StartSection { get; set; }

    [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
    public int EndSection { get; set; }

    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int StartWeek { get; set; }

    [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
    public int EndWeek { get; set; }

    [Required(ErrorMessage = "教室不能为空")]
    [StringLength(64, ErrorMessage = "教室长度不能超过 64 个字符")]
    public string Classroom { get; set; } = string.Empty;
}

/// <summary>课表响应 DTO</summary>
public class ScheduleResponseDto
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public int StartSection { get; set; }
    public int EndSection { get; set; }
    public int StartWeek { get; set; }
    public int EndWeek { get; set; }
    public string Classroom { get; set; } = string.Empty;
}

/// <summary>周课表 DTO</summary>
public class WeeklyScheduleDto
{
    public int Week { get; set; }
    public Dictionary<int, List<ScheduleResponseDto>> Days { get; set; } = new();
}
