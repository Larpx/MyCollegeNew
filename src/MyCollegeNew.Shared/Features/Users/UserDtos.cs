using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users
{
    /// <summary>
    /// 学生创建 DTO
    /// </summary>
    public class StudentCreateDto
    {
        [Required(ErrorMessage = "学号不能为空")]
        [StringLength(32, ErrorMessage = "学号长度不能超过 32 个字符")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度需在 6-128 个字符之间")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "性别不能为空")]
        [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
        public string Gender { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
        public long DepartmentId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
        public long MajorId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
        public long ClassId { get; set; }

        [Range(1900, 2100, ErrorMessage = "年级范围无效")]
        public int Grade { get; set; }
    }

    /// <summary>
    /// 学生更新 DTO
    /// </summary>
    public class StudentUpdateDto
    {
        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "性别不能为空")]
        [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
        public string Gender { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
        public long DepartmentId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
        public long MajorId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
        public long ClassId { get; set; }

        [Range(1900, 2100, ErrorMessage = "年级范围无效")]
        public int Grade { get; set; }

        [Range(0, 2, ErrorMessage = "在读状态无效")]
        public int Status { get; set; }

        [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 学生响应 DTO
    /// </summary>
    public class StudentResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        /// <summary>院系 Id（用于编辑表单回显）</summary>
        public long DepartmentId { get; set; }
        /// <summary>专业 Id（用于编辑表单回显）</summary>
        public long MajorId { get; set; }
        /// <summary>班级 Id（用于编辑表单回显）</summary>
        public long ClassId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string MajorName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int Grade { get; set; }
        public int Status { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 教师创建 DTO
    /// </summary>
    public class TeacherCreateDto
    {
        [Required(ErrorMessage = "工号不能为空")]
        [StringLength(32, ErrorMessage = "工号长度不能超过 32 个字符")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度需在 6-128 个字符之间")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "性别不能为空")]
        [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
        public string Gender { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
        public long DepartmentId { get; set; }

        public long? MajorId { get; set; }

        [Required(ErrorMessage = "教师角色不能为空")]
        public TeacherRole Role { get; set; }
    }

    /// <summary>
    /// 教师更新 DTO
    /// </summary>
    public class TeacherUpdateDto
    {
        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(32, ErrorMessage = "姓名长度不能超过 32 个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "性别不能为空")]
        [StringLength(8, ErrorMessage = "性别长度不能超过 8 个字符")]
        public string Gender { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
        public long DepartmentId { get; set; }

        public long? MajorId { get; set; }

        [Required(ErrorMessage = "教师角色不能为空")]
        public TeacherRole Role { get; set; }

        [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 教师响应 DTO
    /// </summary>
    public class TeacherResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        /// <summary>院系 Id（用于编辑表单回显）</summary>
        public long DepartmentId { get; set; }
        /// <summary>专业 Id（用于编辑表单回显，null 表示未关联）</summary>
        public long? MajorId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? MajorName { get; set; }
        public TeacherRole Role { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 修改密码 DTO
    /// </summary>
    public class PasswordChangeDto
    {
        [Required(ErrorMessage = "旧密码不能为空")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "新密码长度需在 6-128 个字符之间")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批量导入结果 DTO
    /// </summary>
    public class BatchImportResultDto
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<BatchImportFailureItem> Failures { get; set; } = new();
    }

    /// <summary>
    /// 批量导入失败明细项
    /// </summary>
    public class BatchImportFailureItem
    {
        public int Row { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}