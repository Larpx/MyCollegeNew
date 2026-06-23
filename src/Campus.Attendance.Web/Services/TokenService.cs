using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.JSInterop;

namespace Campus.Attendance.Web.Services;

/// <summary>
/// 前端 Token 服务：负责在 localStorage 中存取 JWT Token，并解析用户信息
/// </summary>
public class TokenService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TokenService> _logger;

    // localStorage 中存储 Token 的键名
    private const string TokenKey = "campus_token";

    /// <summary>构造函数</summary>
    /// <param name="jsRuntime">JS 运行时</param>
    /// <param name="logger">日志记录器</param>
    public TokenService(IJSRuntime jsRuntime, ILogger<TokenService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>从 localStorage 读取 Token</summary>
    /// <returns>Token 字符串；不存在则返回 null</returns>
    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
    }

    /// <summary>存储 Token 到 localStorage</summary>
    /// <param name="token">JWT Token</param>
    public async Task SetTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    /// <summary>清除 localStorage 中的 Token</summary>
    public async Task RemoveTokenAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }

    /// <summary>
    /// 解析 Token 返回用户信息（UserId、UserName、Role）
    /// </summary>
    /// <returns>用户信息元组；Token 无效时返回 null</returns>
    public async Task<UserInfo?> GetUserInfoAsync()
    {
        var token = await GetTokenAsync();
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
