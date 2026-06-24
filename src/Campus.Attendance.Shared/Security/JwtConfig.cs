namespace Campus.Attendance.Shared.Security;

/// <summary>
/// JWT 配置类
/// </summary>
public class JwtConfig
{
    /// <summary>签发者</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>受众</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>签名密钥</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>令牌过期时间（分钟）</summary>
    public int ExpireMinutes { get; set; } = 120;
}
