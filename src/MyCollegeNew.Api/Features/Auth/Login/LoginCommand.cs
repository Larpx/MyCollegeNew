using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login
{
    /// <summary>
    /// 登录命令
    /// </summary>
    /// <param name="Request">登录请求</param>
    public record LoginCommand(LoginRequest Request) : IRequest<ApiResponse<LoginResult>>;
}