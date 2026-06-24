using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Campus.Attendance.Services.Auth;

/// <summary>
/// JWT 令牌服务实现，使用 HMACSHA256 对称签名生成与校验访问令牌
/// </summary>
public class TokenService : ITokenService
{
    /// <summary>JWT 自定义声明：用户ID</summary>
    public const string ClaimUserId = "user_id";

    /// <summary>JWT 自定义声明：用户名</summary>
    public const string ClaimUserName = "user_name";

    /// <summary>JWT 自定义声明：角色</summary>
    public const string ClaimRole = "role";

    private readonly JwtConfig _jwtConfig;
    private readonly ILogger<TokenService> _logger;
    private readonly SymmetricSecurityKey _signingKey;
    private static readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// 构造函数，注入 JWT 配置与日志器
    /// </summary>
    /// <param name="jwtConfig">JWT 配置（通过 IOptions 注入）</param>
    /// <param name="logger">日志器</param>
    public TokenService(IOptions<JwtConfig> jwtConfig, ILogger<TokenService> logger)
    {
        _jwtConfig = jwtConfig.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
        _logger = logger;
    }

    /// <summary>
    /// 根据用户身份信息生成 JWT 令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <returns>已签名的 JWT 字符串</returns>
    public string GenerateToken(string userId, string userName, UserRole role)
    {
        var key = _signingKey;
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(ClaimUserId, userId),
            new Claim(ClaimUserName, userName),
            new Claim(ClaimRole, role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireMinutes),
            signingCredentials: credentials);

        var tokenString = _tokenHandler.WriteToken(token);
        _logger.LogInformation("为用户 {UserId} 生成 JWT 令牌", userId);
        return tokenString;
    }

    /// <summary>
    /// 校验 JWT 令牌的签名与过期时间
    /// </summary>
    /// <param name="token">待校验的 JWT 字符串</param>
    /// <returns>校验成功返回用户身份三元组，失败返回 null</returns>
    public (string userId, string userName, UserRole role)? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var key = _signingKey;
        var tokenHandler = _tokenHandler;

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            var userId = principal.FindFirst(ClaimUserId)?.Value
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = principal.FindFirst(ClaimUserName)?.Value
                           ?? principal.FindFirst(ClaimTypes.Name)?.Value;
            var roleString = principal.FindFirst(ClaimRole)?.Value
                             ?? principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName)
                || !Enum.TryParse<UserRole>(roleString, out var role))
            {
                _logger.LogWarning("JWT 令牌缺少必要声明");
                return null;
            }

            return (userId, userName, role);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT 令牌校验失败");
            return null;
        }
    }
}
