using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Campus.Attendance.Web.Services;

/// <summary>
/// 前端 Token 服务：使用 HttpOnly Cookie 管理 JWT，禁止将 Token 存入 localStorage
/// 内部缓存机制兼容 Blazor InteractiveServer 模式（HttpContext 不可用时的降级策略）
/// </summary>
public class TokenService
{
    private const string TokenCookieName = "campus_attendance_token";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenService> _logger;

    /// <summary>交互模式下 HttpContext 不可用时的内存缓存</summary>
    private string? _cachedToken;

    /// <summary>构造函数</summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    /// <param name="logger">日志记录器</param>
    public TokenService(IHttpContextAccessor httpContextAccessor, ILogger<TokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>将 JWT 写入 HttpOnly Cookie 并缓存</summary>
    /// <param name="token">JWT Token</param>
    public void SetToken(string token)
    {
        _cachedToken = token;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2),
                Path = "/"
            };
            httpContext.Response.Cookies.Append(TokenCookieName, token, options);
        }
    }

    /// <summary>从 HttpOnly Cookie 读取 JWT；Cookie 不可用时降级到内存缓存</summary>
    /// <returns>Token 字符串；不存在则返回 null</returns>
    public string? GetToken()
    {
        // 优先从 HttpContext 读取（SSR 模式）
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var cookieToken = httpContext.Request.Cookies[TokenCookieName];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                _cachedToken = cookieToken;
                return cookieToken;
            }
        }

        // 降级到内存缓存（InteractiveServer 模式）
        return _cachedToken;
    }

    /// <summary>删除 HttpOnly Cookie 中的 JWT 并清除缓存</summary>
    public void RemoveToken()
    {
        _cachedToken = null;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };
            httpContext.Response.Cookies.Delete(TokenCookieName, options);
        }
    }

    /// <summary>
    /// 解析 Token 返回用户信息（UserId、UserName、Role）
    /// </summary>
    /// <returns>用户信息；Token 无效时返回 null</returns>
    public UserInfo? GetUserInfo()
    {
        var token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var userId = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userName = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name || c.Type == ClaimTypes.Name)?.Value;
            var role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return null;
            }

            return new UserInfo(userId, userName ?? userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 Token 失败");
            return null;
        }
    }
}

/// <summary>
/// 用户信息记录：UserId、UserName、Role
/// </summary>
/// <param name="UserId">用户ID（学号/工号/admin）</param>
/// <param name="UserName">用户名/真实姓名</param>
/// <param name="Role">角色（Admin/Teacher/Counselor/Student）</param>
public record UserInfo(string UserId, string UserName, string Role);
