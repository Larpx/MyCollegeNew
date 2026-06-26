namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth
{
    /// <summary>
    /// 滑块验证码响应
    /// </summary>
    public class SliderCaptchaResponse
    {
        /// <summary>验证码唯一标识</summary>
        public string CaptchaId { get; set; } = string.Empty;

        /// <summary>背景图 Base64</summary>
        public string BackgroundImage { get; set; } = string.Empty;

        /// <summary>滑块图 Base64（带透明背景）</summary>
        public string SliderImage { get; set; } = string.Empty;

        /// <summary>滑块初始 X 位置</summary>
        public int SliderX { get; set; }
    }

    /// <summary>
    /// 滑块验证码校验请求
    /// </summary>
    public class SliderCaptchaVerifyRequest
    {
        /// <summary>验证码唯一标识</summary>
        public string CaptchaId { get; set; } = string.Empty;

        /// <summary>用户拖动后的 X 坐标</summary>
        public int SliderX { get; set; }
    }

    /// <summary>
    /// 滑块验证码校验响应
    /// </summary>
    public class SliderCaptchaVerifyResponse
    {
        /// <summary>校验是否通过</summary>
        public bool Success { get; set; }

        /// <summary>验证通过后颁发的一次性 token</summary>
        public string? Token { get; set; }

        /// <summary>错误信息</summary>
        public string? ErrorMessage { get; set; }
    }
}
