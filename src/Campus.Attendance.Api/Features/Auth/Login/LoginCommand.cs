using Campus.Attendance.Shared.Features.Auth;
using Campus.Attendance.Shared.Responses;
using MediatR;

namespace Campus.Attendance.Api.Features.Auth.Login;

/// <summary>
/// 登录命令
/// </summary>
/// <param name="Request">登录请求</param>
public record LoginCommand(LoginRequest Request) : IRequest<ApiResponse<LoginResult>>;
