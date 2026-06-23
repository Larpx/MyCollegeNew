using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Campus.Attendance.Web.Services;

/// <summary>
/// 自定义认证状态提供器：从 localStorage 读取 Token，解析 Claims 构建认证状态
/// 用于 Blazor Server 的 AuthorizeView 与 AuthorizeRouteView
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenService _tokenService;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<CustomAuthStateProvider> _logger;

    /// <summary>构造函数</summary>
    /// <param name="tokenService">前端 Token 服务</param>
    /// <param name="navigationManager">导航管理器</param>
    /// <param name="logger">日志记录器</param>
    public CustomAuthStateProvider(TokenService tokenService, NavigationManager navigationManager, ILogger<CustomAuthStateProvider> logger)
    {
        _tokenService = tokenService;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前认证状态：从 localStorage 读取 Token 并解析 Claims
    /// </summary>
    /// <returns>认证状态（已登录返回已认证用户，未登录返回匿名用户）</returns>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var userInfo = await _tokenService.GetUserInfoAsync();
        if (userInfo is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.UserName),
            new Claim(ClaimTypes.Role, userInfo.Role)
        };

        var identity = new ClaimsIdentity(claims, "CampusAuth");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    /// <summary>
    /// 登录成功后通知认证状态变更
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public void NotifyUserLogin(UserInfo userInfo)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.UserName),
            new Claim(ClaimTypes.Role, userInfo.Role)
        };

        var identity = new ClaimsIdentity(claims, "CampusAuth");
        var principal = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    /// <summary>
    /// 登出后通知认证状态变更（变为匿名用户）
    /// </summary>
    public void NotifyUserLogout()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }
}
