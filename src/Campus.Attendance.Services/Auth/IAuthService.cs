using Campus.Attendance.Core.Enums;
using Campus.Attendance.Models.Auth;

namespace Campus.Attendance.Services.Auth;

/// <summary>
/// 认证服务接口，封装登录认证逻辑
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 异步登录认证：依次按管理员/教师/学生身份校验
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录成功返回 LoginResult，失败返回 null</returns>
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
