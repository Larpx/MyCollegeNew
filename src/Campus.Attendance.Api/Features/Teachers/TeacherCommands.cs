using Campus.Attendance.Shared.Contracts;
using Campus.Attendance.Shared.Entities;
using Campus.Attendance.Shared.Enums;
using Campus.Attendance.Shared.Features.Users;
using Campus.Attendance.Shared.Responses;
using MediatR;

namespace Campus.Attendance.Api.Features.Teachers;

/// <summary>分页查询教师列表</summary>
public record GetTeachersQuery : PagedQuery, IRequest<ApiResponse<PagedResult<TeacherResponseDto>>>
{
    /// <summary>搜索关键字</summary>
    public string? Keyword { get; init; }

    /// <summary>教师角色</summary>
    public TeacherRole? Role { get; init; }

    /// <summary>院系ID</summary>
    public long? DepartmentId { get; init; }
}

/// <summary>根据工号查询教师</summary>
public record GetTeacherByIdQuery(string Id) : IRequest<ApiResponse<TeacherResponseDto>>;

/// <summary>创建教师</summary>
public record CreateTeacherCommand(TeacherCreateDto Dto) : IRequest<ApiResponse<TeacherResponseDto>>;

/// <summary>更新教师</summary>
public record UpdateTeacherCommand(string Id, TeacherUpdateDto Dto) : IRequest<ApiResponse<TeacherResponseDto>>;

/// <summary>删除教师</summary>
public record DeleteTeacherCommand(string Id) : IRequest<ApiResponse<object>>;
