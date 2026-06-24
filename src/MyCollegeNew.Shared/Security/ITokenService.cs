using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Security;

/// <summary>
/// JWT 令牌服务接口
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 生成 JWT 令牌
    /// </summary>
    string GenerateToken(string userId, string userName, UserRole role);

    /// <summary>
    /// 校验 JWT 令牌
    /// </summary>
    (string userId, string userName, UserRole role)? ValidateToken(string token);
}
