using System.Security.Claims;
using Campus.Attendance.Shared.Enums;
using Campus.Attendance.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Campus.Attendance.Infrastructure.Auth;

/// <summary>
/// 当前用户上下文实现，从 HttpContext 的 JWT Claims 中解析用户身份信息
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    /// <summary>
    /// 构造函数，注入 HttpContext 访问器与日志器
    /// </summary>
    /// <param name="httpContextAccessor">HttpContext 访问器</param>
    /// <param name="logger">日志器</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>用户ID（学号/工号/admin）</summary>
    public string UserId => GetClaimValue(TokenService.ClaimUserId)
                            ?? GetClaimValue(ClaimTypes.NameIdentifier)
                            ?? string.Empty;

    /// <summary>用户名/真实姓名</summary>
    public string UserName => GetClaimValue(TokenService.ClaimUserName)
                             ?? GetClaimValue(ClaimTypes.Name)
                             ?? string.Empty;

    /// <summary>用户角色</summary>
    public UserRole Role
    {
        get
        {
            var roleString = GetClaimValue(TokenService.ClaimRole)
                             ?? GetClaimValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleString, out var role) ? role : default;
        }
    }

    /// <summary>是否已认证</summary>
    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    /// <summary>
    /// 从当前 ClaimsPrincipal 中获取指定声明值
    /// </summary>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明值，未找到返回 null</returns>
    private string? GetClaimValue(string claimType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return null;
        }

        var value = user.FindFirst(claimType)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogDebug("未在 Claims 中找到声明 {ClaimType}", claimType);
        }

        return value;
    }
}
