using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Larpx.PersonalTools.MyCollegeNew.Admin.Services
{
    /// <summary>
    /// 自定义认证状态提供器：优先从 ASP.NET Core 认证 Cookie 读取用户信息，
    /// 降级到从 HttpOnly Cookie 中的 JWT 解析用户信息
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly TokenService _tokenService;
        private readonly ITokenService _jwtTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>构造函数</summary>
        /// <param name="tokenService">前端 Cookie-based Token 服务</param>
        /// <param name="jwtTokenService">JWT 校验服务（Shared.Security）</param>
        /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
        public CustomAuthStateProvider(TokenService tokenService, ITokenService jwtTokenService, IHttpContextAccessor httpContextAccessor)
        {
            _tokenService = tokenService;
            _jwtTokenService = jwtTokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取当前认证状态：优先从 ASP.NET Core 认证 Cookie 读取，降级到 JWT Token 解析
        /// </summary>
        /// <returns>认证状态（已登录返回已认证用户，未登录返回匿名用户）</returns>
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // 优先从 ASP.NET Core 认证中间件获取已认证的用户
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(new AuthenticationState(httpContext.User));
            }

            // 降级：从 JWT Token 解析用户信息（InteractiveServer 电路内）
            var token = _tokenService.GetToken();

            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            try
            {
                var validationResult = _jwtTokenService.ValidateToken(token);
                if (validationResult is null)
                {
                    _tokenService.RemoveToken();
                    return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
                }

                var (userId, userName, role) = validationResult.Value;
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, role.ToString())
                };

                var identity = new ClaimsIdentity(claims, "CampusAuth");
                var principal = new ClaimsPrincipal(identity);
                return Task.FromResult(new AuthenticationState(principal));
            }
            catch
            {
                _tokenService.RemoveToken();
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
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
}