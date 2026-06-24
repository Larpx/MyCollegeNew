using Campus.Attendance.Core.Responses;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Users;
using Campus.Attendance.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 个人资料控制器，提供当前用户修改密码
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserService _userService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="currentUser">当前用户上下文</param>
    /// <param name="userService">用户管理服务</param>
    public ProfileController(ICurrentUser currentUser, IUserService userService)
    {
        _currentUser = currentUser;
        _userService = userService;
    }

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    /// <param name="dto">密码修改 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<object>> ChangePassword([FromBody] PasswordChangeDto dto, CancellationToken cancellationToken)
    {
        await _userService.ChangePasswordAsync(_currentUser.UserId, _currentUser.Role, dto, cancellationToken);
        return ApiResponse<object>.Success(new { }, Msg.Common.PasswordChangeSuccess);
    }
}
