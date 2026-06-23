using System.ComponentModel.DataAnnotations;

namespace Campus.Attendance.Models.Organization;

/// <summary>
/// 院系创建 DTO
/// </summary>
public class DepartmentCreateDto
{
    /// <summary>院系名称</summary>
    [Required(ErrorMessage = "院系名称不能为空")]
    [StringLength(64, ErrorMessage = "院系名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 院系更新 DTO
/// </summary>
public class DepartmentUpdateDto
{
    /// <summary>院系名称</summary>
    [Required(ErrorMessage = "院系名称不能为空")]
    [StringLength(64, ErrorMessage = "院系名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 院系响应 DTO，包含专业数与学生数统计
/// </summary>
public class DepartmentResponseDto
{
    /// <summary>院系 Id</summary>
    public long Id { get; set; }

    /// <summary>院系名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>该院系下的专业数量</summary>
    public int MajorCount { get; set; }

    /// <summary>该院系下的学生数量</summary>
    public int StudentCount { get; set; }
}

/// <summary>
/// 专业创建 DTO
/// </summary>
public class MajorCreateDto
{
    /// <summary>专业名称</summary>
    [Required(ErrorMessage = "专业名称不能为空")]
    [StringLength(64, ErrorMessage = "专业名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
    public long DepartmentId { get; set; }
}

/// <summary>
/// 专业更新 DTO
/// </summary>
public class MajorUpdateDto
{
    /// <summary>专业名称</summary>
    [Required(ErrorMessage = "专业名称不能为空")]
    [StringLength(64, ErrorMessage = "专业名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "院系 Id 无效")]
    public long DepartmentId { get; set; }
}

/// <summary>
/// 专业响应 DTO，包含院系名称与班级数统计
/// </summary>
public class MajorResponseDto
{
    /// <summary>专业 Id</summary>
    public long Id { get; set; }

    /// <summary>专业名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    public long DepartmentId { get; set; }

    /// <summary>所属院系名称</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>该专业下的班级数量</summary>
    public int ClassCount { get; set; }
}

/// <summary>
/// 班级创建 DTO
/// </summary>
public class ClassCreateDto
{
    /// <summary>班级名称</summary>
    [Required(ErrorMessage = "班级名称不能为空")]
    [StringLength(64, ErrorMessage = "班级名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>所属专业 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
    public long MajorId { get; set; }

    /// <summary>年级（入学年份，如 2022）</summary>
    [Range(1900, 2100, ErrorMessage = "年级范围无效")]
    public int Grade { get; set; }

    /// <summary>辅导员工号（关联 Teacher.Id）</summary>
    [Required(ErrorMessage = "辅导员工号不能为空")]
    [StringLength(32, ErrorMessage = "辅导员工号长度不能超过 32 个字符")]
    public string CounselorId { get; set; } = string.Empty;
}

/// <summary>
/// 班级更新 DTO
/// </summary>
public class ClassUpdateDto
{
    /// <summary>班级名称</summary>
    [Required(ErrorMessage = "班级名称不能为空")]
    [StringLength(64, ErrorMessage = "班级名称长度不能超过 64 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>所属专业 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "专业 Id 无效")]
    public long MajorId { get; set; }

    /// <summary>年级</summary>
    [Range(1900, 2100, ErrorMessage = "年级范围无效")]
    public int Grade { get; set; }

    /// <summary>辅导员工号</summary>
    [Required(ErrorMessage = "辅导员工号不能为空")]
    [StringLength(32, ErrorMessage = "辅导员工号长度不能超过 32 个字符")]
    public string CounselorId { get; set; } = string.Empty;
}

/// <summary>
/// 班级响应 DTO，包含专业名称、辅导员姓名与学生数统计
/// </summary>
public class ClassResponseDto
{
    /// <summary>班级 Id</summary>
    public long Id { get; set; }

    /// <summary>班级名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属专业 Id</summary>
    public long MajorId { get; set; }

    /// <summary>所属专业名称</summary>
    public string MajorName { get; set; } = string.Empty;

    /// <summary>年级</summary>
    public int Grade { get; set; }

    /// <summary>辅导员工号</summary>
    public string CounselorId { get; set; } = string.Empty;

    /// <summary>辅导员姓名</summary>
    public string CounselorName { get; set; } = string.Empty;

    /// <summary>该班级下的学生数量</summary>
    public int StudentCount { get; set; }
}

/// <summary>
/// 组织架构树节点 DTO，描述院系→专业→班级三级层级结构
/// </summary>
public class OrganizationTreeNodeDto
{
    /// <summary>院系信息</summary>
    public DepartmentResponseDto Department { get; set; } = new();

    /// <summary>该院系下的专业列表（每个专业含其班级列表）</summary>
    public List<MajorTreeNodeDto> Majors { get; set; } = new();
}

/// <summary>
/// 专业树节点 DTO，包含专业信息与其下班级列表
/// </summary>
public class MajorTreeNodeDto
{
    /// <summary>专业信息</summary>
    public MajorResponseDto Major { get; set; } = new();

    /// <summary>该专业下的班级列表</summary>
    public List<ClassResponseDto> Classes { get; set; } = new();
}
