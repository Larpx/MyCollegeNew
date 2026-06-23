using System.ComponentModel.DataAnnotations;

namespace Campus.Attendance.Models.Users;

/// <summary>
/// 学生创建 DTO
/// </summary>
public class StudentCreateDto
{
    /// <summary>学号（主键）</summary>
    [Required(ErrorMessage = "学号不能为空")]
    [StringLength(32, ErrorMessage = "学号长度不能超过 32 个字符")]
    public string Id { get; set; } = string.Empty;

    /// <summary>学生姓名</summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>登录密码（明文，服务端会进行 BCrypt 哈希）</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度需在 6-128 个字符之间")]
    public string Password { get; set; } = string.Empty;

    /// <summary>性别（"男" 或 "女"）</summary>
    [Required(ErrorMessage = "性别不能为空")]
    [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
    public string Gender { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
    public long DepartmentId { get; set; }

    /// <summary>所属专业 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
    public long MajorId { get; set; }

    /// <summary>所属班级 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    /// <summary>年级（入学年份，如 2022）</summary>
    [Range(1900, 2100, ErrorMessage = "年级范围无效")]
    public int Grade { get; set; }
}

/// <summary>
/// 学生更新 DTO
/// </summary>
public class StudentUpdateDto
{
    /// <summary>学生姓名</summary>
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

    /// <summary>所属专业 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
    public long MajorId { get; set; }

    /// <summary>所属班级 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    /// <summary>年级</summary>
    [Range(1900, 2100, ErrorMessage = "年级范围无效")]
    public int Grade { get; set; }

    /// <summary>在读状态（0=在读, 1=休学, 2=毕业）</summary>
    [Range(0, 2, ErrorMessage = "在读状态无效")]
    public int Status { get; set; }

    /// <summary>备注</summary>
    [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
    public string? Remark { get; set; }
}

/// <summary>
/// 学生响应 DTO
/// </summary>
public class StudentResponseDto
{
    /// <summary>学号</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>所属院系名称</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>所属专业名称</summary>
    public string MajorName { get; set; } = string.Empty;

    /// <summary>所属班级名称</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>年级</summary>
    public int Grade { get; set; }

    /// <summary>在读状态</summary>
    public int Status { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
