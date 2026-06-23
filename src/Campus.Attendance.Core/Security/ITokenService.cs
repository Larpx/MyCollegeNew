using Campus.Attendance.Core.Enums;

namespace Campus.Attendance.Core.Security;

/// <summary>
/// JWT 令牌服务接口，负责生成与校验访问令牌
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 根据用户身份信息生成 JWT 令牌
    /// </summary>
    /// <param name="userId">用户ID（学号/工号/admin）</param>
    /// <param name="userName">用户名/真实姓名</param>
    /// <param name="role">用户角色</param>
    /// <returns>已签名的 JWT 字符串</returns>
    string GenerateToken(string userId, string userName, UserRole role);

    /// <summary>
    /// 校验 JWT 令牌的签名与过期时间
    /// </summary>
    /// <param name="token">待校验的 JWT 字符串</param>
    /// <returns>校验成功返回用户身份三元组，失败返回 null</returns>
    (string userId, string userName, UserRole role)? ValidateToken(string token);
}
