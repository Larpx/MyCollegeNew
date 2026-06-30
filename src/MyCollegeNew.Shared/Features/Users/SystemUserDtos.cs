using System.ComponentModel.DataAnnotations;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users
{
    /// <summary>
    /// 系统用户（管理员）相关 DTO
    /// </summary>
    public class SystemUserResponseDto
    {
        /// <summary>系统用户主键</summary>
        public long Id { get; set; }

        /// <summary>登录用户名</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        public UserRole Role { get; set; }

        /// <summary>是否已绑定二次验证</summary>
        public bool HasTwoFactor { get; set; }

        /// <summary>创建时间（UTC）</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>最后更新时间（UTC）</summary>
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 创建系统用户 DTO
    /// </summary>
    public class SystemUserCreateDto
    {
        /// <summary>登录用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(64, ErrorMessage = "用户名长度不能超过 64 个字符")]
        public string Username { get; set; } = string.Empty;

        /// <summary>登录密码（明文，后端会做 BCrypt 哈希）</summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "密码长度需在 8-128 个字符之间")]
        public string Password { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(32, ErrorMessage = "真实姓名长度不能超过 32 个字符")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色（默认管理员）</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        public UserRole Role { get; set; } = UserRole.Admin;
    }

    /// <summary>
    /// 更新系统用户 DTO（不允许修改用户名）
    /// </summary>
    public class SystemUserUpdateDto
    {
        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(32, ErrorMessage = "真实姓名长度不能超过 32 个字符")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        public UserRole Role { get; set; } = UserRole.Admin;
    }

    /// <summary>
    /// 重置密码 DTO
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>新密码（明文，后端会做 BCrypt 哈希）</summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "新密码长度需在 8-128 个字符之间")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
