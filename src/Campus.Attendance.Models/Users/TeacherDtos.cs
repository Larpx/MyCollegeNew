using System.ComponentModel.DataAnnotations;
using Campus.Attendance.Core.Enums;

namespace Campus.Attendance.Models.Users;

/// <summary>
/// 教师创建 DTO
/// </summary>
public class TeacherCreateDto
{
    /// <summary>工号（主键）</summary>
    [Required(ErrorMessage = "工号不能为空")]
    [StringLength(32, ErrorMessage = "工号长度不能超过 32 个字符")]
    public string Id { get; set; } = string.Empty;

    /// <summary>教师姓名</summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>登录密码（明文，服务端会进行 BCrypt 哈希）</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度需在 6-128 个字符之间")]
    public string Password { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [Required(ErrorMessage = "性别不能为空")]
    [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
    public string Gender { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
    public long DepartmentId { get; set; }

    /// <summary>所属专业 Id（辅导员可为空）</summary>
    public long? MajorId { get; set; }

    /// <summary>教师角色（Teacher=任课教师, Counselor=辅导员）</summary>
    [Required(ErrorMessage = "教师角色不能为空")]
    public TeacherRole Role { get; set; }
}

/// <summary>
/// 教师更新 DTO
/// </summary>
public class TeacherUpdateDto
{
    /// <summary>教师姓名</summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [Required(ErrorMessage = "性别不能为空")]
    [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
    public string Gender { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
    public long DepartmentId { get; set; }

    /// <summary>所属专业 Id（辅导员可为空）</summary>
    public long? MajorId { get; set; }

    /// <summary>教师角色</summary>
    [Required(ErrorMessage = "教师角色不能为空")]
    public TeacherRole Role { get; set; }

    /// <summary>备注</summary>
    [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
    public string? Remark { get; set; }
}

/// <summary>
/// 教师响应 DTO
/// </summary>
public class TeacherResponseDto
{
    /// <summary>工号</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>所属院系名称</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>所属专业名称</summary>
    public string? MajorName { get; set; }

    /// <summary>教师角色</summary>
    public TeacherRole Role { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
