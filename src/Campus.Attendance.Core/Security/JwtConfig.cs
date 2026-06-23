namespace Campus.Attendance.Core.Security;

/// <summary>
/// JWT 配置类，通过 IOptions&lt;JwtConfig&gt; 注入，对应 appsettings.json 的 Jwt 节点
/// </summary>
public class JwtConfig
{
    /// <summary>签发者（Issuer），写入 JWT 的 iss 声明</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>受众（Audience），写入 JWT 的 aud 声明</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>签名密钥，至少 32 字符，建议通过环境变量 Jwt__SecretKey 注入</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>令牌过期时间（分钟），默认 120 分钟</summary>
    public int ExpireMinutes { get; set; } = 120;
}
