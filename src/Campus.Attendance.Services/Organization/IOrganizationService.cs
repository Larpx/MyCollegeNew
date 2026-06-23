using Campus.Attendance.Models.Organization;

namespace Campus.Attendance.Services.Organization;

/// <summary>
/// 组织架构管理服务接口，封装院系、专业、班级的增删改查与树形结构查询
/// </summary>
public interface IOrganizationService
{
    /// <summary>查询所有院系（过滤软删除）</summary>
    Task<List<DepartmentResponseDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>根据 Id 查询单个院系</summary>
    Task<DepartmentResponseDto?> GetDepartmentByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>创建院系</summary>
    Task<DepartmentResponseDto> CreateDepartmentAsync(DepartmentCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>更新院系</summary>
    Task<DepartmentResponseDto> UpdateDepartmentAsync(long id, DepartmentUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>软删除院系（需检查是否有专业关联）</summary>
    Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按院系查询专业列表</summary>
    Task<List<MajorResponseDto>> GetMajorsByDepartmentAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>根据 Id 查询单个专业</summary>
    Task<MajorResponseDto?> GetMajorByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>创建专业</summary>
    Task<MajorResponseDto> CreateMajorAsync(MajorCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>更新专业</summary>
    Task<MajorResponseDto> UpdateMajorAsync(long id, MajorUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>软删除专业（需检查是否有班级关联）</summary>
    Task DeleteMajorAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按专业查询班级列表</summary>
    Task<List<ClassResponseDto>> GetClassesByMajorAsync(long majorId, CancellationToken cancellationToken = default);

    /// <summary>根据 Id 查询单个班级</summary>
    Task<ClassResponseDto?> GetClassByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>创建班级</summary>
    Task<ClassResponseDto> CreateClassAsync(ClassCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>更新班级</summary>
    Task<ClassResponseDto> UpdateClassAsync(long id, ClassUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>软删除班级</summary>
    Task DeleteClassAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>返回院系→专业→班级三级树形结构</summary>
    Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync(CancellationToken cancellationToken = default);
}
