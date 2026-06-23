using Campus.Attendance.Core.Responses;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Auth;
using Campus.Attendance.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 认证控制器，提供登录、登出与当前用户信息查询
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="authService">认证服务</param>
    /// <param name="currentUser">当前用户上下文</param>
    public AuthController(IAuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录结果</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return ApiResponse<LoginResult>.Fail("用户名或密码错误", 400);
        }

        return ApiResponse<LoginResult>.Success(result, "登录成功");
    }

    /// <summary>
    /// 用户登出（JWT 无状态，客户端清除令牌即可）
    /// </summary>
    /// <returns>操作结果</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public ApiResponse<object> Logout()
    {
        return ApiResponse<object>.Success(new { }, "登出成功");
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public ApiResponse<object> GetProfile()
    {
        return ApiResponse<object>.Success(new
        {
            _currentUser.UserId,
            _currentUser.UserName,
            Role = _currentUser.Role.ToString()
        });
    }
}
