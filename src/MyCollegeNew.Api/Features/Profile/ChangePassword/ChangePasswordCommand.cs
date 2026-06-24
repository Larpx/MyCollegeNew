using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Profile.ChangePassword
{
/// <summary>
/// 修改密码命令
/// </summary>
/// <param name="Dto">密码修改请求</param>
/// <param name="UserId">当前用户ID</param>
/// <param name="Role">当前用户角色</param>
public record ChangePasswordCommand(PasswordChangeDto Dto, string UserId, UserRole Role) : IRequest<ApiResponse<object>>;
}