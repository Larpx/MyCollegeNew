using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth
{
    /// <summary>
    /// 二次验证设置请求 DTO（未绑定用户获取 TOTP 绑定信息）
    /// </summary>
    public class TwoFactorSetupRequest
    {
        /// <summary>二次验证临时令牌</summary>
        [Required(ErrorMessage = "临时令牌不能为空")]
        public string TwoFactorToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 二次验证设置结果 DTO
    /// </summary>
    public class TwoFactorSetupResult
    {
        /// <summary>TOTP 密钥（Base32 编码）</summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>OTPAuth URI（用于二维码生成）</summary>
        public string OtpAuthUri { get; set; } = string.Empty;

        /// <summary>二维码 Base64 图片</summary>
        public string QrCodeBase64 { get; set; } = string.Empty;
    }

    /// <summary>
    /// 二次验证码校验请求 DTO（已绑定用户验证码校验）
    /// </summary>
    public class TwoFactorVerifyRequest
    {
        /// <summary>二次验证临时令牌</summary>
        [Required(ErrorMessage = "临时令牌不能为空")]
        public string TwoFactorToken { get; set; } = string.Empty;

        /// <summary>6 位 TOTP 验证码</summary>
        [Required(ErrorMessage = "验证码不能为空")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码必须为 6 位")]
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// 二次验证绑定请求 DTO（未绑定用户首次绑定 TOTP）
    /// </summary>
    public class TwoFactorBindRequest
    {
        /// <summary>二次验证临时令牌</summary>
        [Required(ErrorMessage = "临时令牌不能为空")]
        public string TwoFactorToken { get; set; } = string.Empty;

        /// <summary>6 位 TOTP 验证码</summary>
        [Required(ErrorMessage = "验证码不能为空")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码必须为 6 位")]
        public string Code { get; set; } = string.Empty;

        /// <summary>TOTP 密钥（Base32 编码，由 setup 接口返回）</summary>
        [Required(ErrorMessage = "密钥不能为空")]
        public string Secret { get; set; } = string.Empty;
    }
}
