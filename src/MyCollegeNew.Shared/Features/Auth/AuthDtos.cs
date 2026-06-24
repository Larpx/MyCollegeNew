using System.ComponentModel.DataAnnotations;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth
{
/// <summary>
/// 登录请求 DTO
/// </summary>
public class LoginRequest
{
    /// <summary>用户名</summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(64, ErrorMessage = "用户名长度不能超过 64 个字符")]
    public string Username { get; set; } = string.Empty;

    /// <summary>密码</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度需在 6-128 个字符之间")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录结果 DTO
/// </summary>
public class LoginResult
{
    /// <summary>JWT 令牌</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>用户ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>用户名</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>用户角色</summary>
    public string Role { get; set; } = string.Empty;
}
}