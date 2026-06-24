using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Organization;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Organization;

/// <summary>查询所有院系</summary>
public record GetDepartmentsQuery : IRequest<ApiResponse<List<DepartmentResponseDto>>>;

/// <summary>根据Id查询院系</summary>
public record GetDepartmentByIdQuery(long Id) : IRequest<ApiResponse<DepartmentResponseDto>>;

/// <summary>创建院系</summary>
public record CreateDepartmentCommand(DepartmentCreateDto Dto) : IRequest<ApiResponse<DepartmentResponseDto>>;

/// <summary>更新院系</summary>
public record UpdateDepartmentCommand(long Id, DepartmentUpdateDto Dto) : IRequest<ApiResponse<DepartmentResponseDto>>;

/// <summary>删除院系</summary>
public record DeleteDepartmentCommand(long Id) : IRequest<ApiResponse<object>>;

/// <summary>按院系查询专业</summary>
public record GetMajorsByDepartmentQuery(long DepartmentId) : IRequest<ApiResponse<List<MajorResponseDto>>>;

/// <summary>根据Id查询专业</summary>
public record GetMajorByIdQuery(long Id) : IRequest<ApiResponse<MajorResponseDto>>;

/// <summary>创建专业</summary>
public record CreateMajorCommand(MajorCreateDto Dto) : IRequest<ApiResponse<MajorResponseDto>>;

/// <summary>更新专业</summary>
public record UpdateMajorCommand(long Id, MajorUpdateDto Dto) : IRequest<ApiResponse<MajorResponseDto>>;

/// <summary>删除专业</summary>
public record DeleteMajorCommand(long Id) : IRequest<ApiResponse<object>>;

/// <summary>按专业查询班级</summary>
public record GetClassesByMajorQuery(long MajorId) : IRequest<ApiResponse<List<ClassResponseDto>>>;

/// <summary>根据Id查询班级</summary>
public record GetClassByIdQuery(long Id) : IRequest<ApiResponse<ClassResponseDto>>;

/// <summary>创建班级</summary>
public record CreateClassCommand(ClassCreateDto Dto) : IRequest<ApiResponse<ClassResponseDto>>;

/// <summary>更新班级</summary>
public record UpdateClassCommand(long Id, ClassUpdateDto Dto) : IRequest<ApiResponse<ClassResponseDto>>;

/// <summary>删除班级</summary>
public record DeleteClassCommand(long Id) : IRequest<ApiResponse<object>>;

/// <summary>查询组织树</summary>
public record GetOrganizationTreeQuery : IRequest<ApiResponse<List<OrganizationTreeNodeDto>>>;
