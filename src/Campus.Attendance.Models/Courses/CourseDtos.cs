using System.ComponentModel.DataAnnotations;

namespace Campus.Attendance.Models.Courses;

/// <summary>
/// 课程创建 DTO
/// </summary>
public class CourseCreateDto
{
    /// <summary>课程名称</summary>
    [Required(ErrorMessage = "课程名称不能为空")]
    [StringLength(64, ErrorMessage = "课程名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>任课教师工号（关联 Teacher.Id）</summary>
    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>学分</summary>
    [Range(0.5, 10, ErrorMessage = "学分范围无效（0.5-10）")]
    public decimal Credit { get; set; }

    /// <summary>备注</summary>
    [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
    public string? Remark { get; set; }
}

/// <summary>
/// 课程更新 DTO
/// </summary>
public class CourseUpdateDto
{
    /// <summary>课程名称</summary>
    [Required(ErrorMessage = "课程名称不能为空")]
    [StringLength(64, ErrorMessage = "课程名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>任课教师工号</summary>
    [Required(ErrorMessage = "任课教师工号不能为空")]
    [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>学分</summary>
    [Range(0.5, 10, ErrorMessage = "学分范围无效（0.5-10）")]
    public decimal Credit { get; set; }

    /// <summary>备注</summary>
    [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
    public string? Remark { get; set; }
}

/// <summary>
/// 课程响应 DTO，包含任课教师姓名
/// </summary>
public class CourseResponseDto
{
    /// <summary>课程 Id</summary>
    public long Id { get; set; }

    /// <summary>课程名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>任课教师工号</summary>
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>任课教师姓名</summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>学分</summary>
    public decimal Credit { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
