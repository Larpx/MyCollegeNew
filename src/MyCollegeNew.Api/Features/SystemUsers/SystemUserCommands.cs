using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemUsers
{
    /// <summary>分页查询系统用户列表</summary>
    public record GetSystemUsersQuery : PagedQuery, IRequest<ApiResponse<PagedResult<SystemUserResponseDto>>>
    {
        /// <summary>搜索关键字（匹配用户名或真实姓名）</summary>
        public string? Keyword { get; init; }
    }

    /// <summary>根据 Id 查询系统用户</summary>
    public record GetSystemUserByIdQuery(long Id) : IRequest<ApiResponse<SystemUserResponseDto>>;

    /// <summary>创建系统用户</summary>
    public record CreateSystemUserCommand(SystemUserCreateDto Dto) : IRequest<ApiResponse<SystemUserResponseDto>>;

    /// <summary>更新系统用户</summary>
    public record UpdateSystemUserCommand(long Id, SystemUserUpdateDto Dto) : IRequest<ApiResponse<SystemUserResponseDto>>;

    /// <summary>删除系统用户（软删除）</summary>
    public record DeleteSystemUserCommand(long Id) : IRequest<ApiResponse<object>>;

    /// <summary>重置系统用户密码</summary>
    public record ResetSystemUserPasswordCommand(long Id, ResetPasswordDto Dto) : IRequest<ApiResponse<object>>;
}
