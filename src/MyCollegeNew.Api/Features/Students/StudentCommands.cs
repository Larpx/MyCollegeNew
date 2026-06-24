using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Students
{
/// <summary>分页查询学生列表</summary>
public record GetStudentsQuery : PagedQuery, IRequest<ApiResponse<PagedResult<StudentResponseDto>>>
{
    /// <summary>搜索关键字</summary>
    public string? Keyword { get; init; }

    /// <summary>班级ID</summary>
    public long? ClassId { get; init; }

    /// <summary>专业ID</summary>
    public long? MajorId { get; init; }

    /// <summary>院系ID</summary>
    public long? DepartmentId { get; init; }
}

/// <summary>根据学号查询学生</summary>
public record GetStudentByIdQuery(string Id) : IRequest<ApiResponse<StudentResponseDto>>;

/// <summary>创建学生</summary>
public record CreateStudentCommand(StudentCreateDto Dto) : IRequest<ApiResponse<StudentResponseDto>>;

/// <summary>更新学生</summary>
public record UpdateStudentCommand(string Id, StudentUpdateDto Dto) : IRequest<ApiResponse<StudentResponseDto>>;

/// <summary>删除学生</summary>
public record DeleteStudentCommand(string Id) : IRequest<ApiResponse<object>>;

/// <summary>批量导入学生</summary>
public record BatchImportStudentsCommand(Stream CsvStream) : IRequest<ApiResponse<BatchImportResultDto>>;
}