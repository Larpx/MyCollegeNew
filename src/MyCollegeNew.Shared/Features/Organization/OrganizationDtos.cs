using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Organization
{
    /// <summary>院系创建 DTO</summary>
    public class DepartmentCreateDto
    {
        [Required(ErrorMessage = "院系名称不能为空")]
        [StringLength(64, ErrorMessage = "院系名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>院系更新 DTO</summary>
    public class DepartmentUpdateDto
    {
        [Required(ErrorMessage = "院系名称不能为空")]
        [StringLength(64, ErrorMessage = "院系名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>院系响应 DTO</summary>
    public class DepartmentResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MajorCount { get; set; }
        public int StudentCount { get; set; }
    }

    /// <summary>专业创建 DTO</summary>
    public class MajorCreateDto
    {
        [Required(ErrorMessage = "专业名称不能为空")]
        [StringLength(64, ErrorMessage = "专业名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
        public long DepartmentId { get; set; }
    }

    /// <summary>专业更新 DTO</summary>
    public class MajorUpdateDto
    {
        [Required(ErrorMessage = "专业名称不能为空")]
        [StringLength(64, ErrorMessage = "专业名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
        public long DepartmentId { get; set; }
    }

    /// <summary>专业响应 DTO</summary>
    public class MajorResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int ClassCount { get; set; }
    }

    /// <summary>班级创建 DTO</summary>
    public class ClassCreateDto
    {
        [Required(ErrorMessage = "班级名称不能为空")]
        [StringLength(64, ErrorMessage = "班级名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
        public long MajorId { get; set; }

        [Range(1900, 2100, ErrorMessage = "年级范围无效")]
        public int Grade { get; set; }

        [Required(ErrorMessage = "辅导员工号不能为空")]
        [StringLength(32, ErrorMessage = "辅导员工号长度不能超过 32 个字符")]
        public string CounselorId { get; set; } = string.Empty;
    }

    /// <summary>班级更新 DTO</summary>
    public class ClassUpdateDto
    {
        [Required(ErrorMessage = "班级名称不能为空")]
        [StringLength(64, ErrorMessage = "班级名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
        public long MajorId { get; set; }

        [Range(1900, 2100, ErrorMessage = "年级范围无效")]
        public int Grade { get; set; }

        [Required(ErrorMessage = "辅导员工号不能为空")]
        [StringLength(32, ErrorMessage = "辅导员工号长度不能超过 32 个字符")]
        public string CounselorId { get; set; } = string.Empty;
    }

    /// <summary>班级响应 DTO</summary>
    public class ClassResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long MajorId { get; set; }
        public string MajorName { get; set; } = string.Empty;
        public int Grade { get; set; }
        public string CounselorId { get; set; } = string.Empty;
        public string CounselorName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    /// <summary>组织架构树节点 DTO</summary>
    public class OrganizationTreeNodeDto
    {
        public DepartmentResponseDto Department { get; set; } = new();
        public List<MajorTreeNodeDto> Majors { get; set; } = new();
    }

    /// <summary>专业树节点 DTO</summary>
    public class MajorTreeNodeDto
    {
        public MajorResponseDto Major { get; set; } = new();
        public List<ClassResponseDto> Classes { get; set; } = new();
    }
}