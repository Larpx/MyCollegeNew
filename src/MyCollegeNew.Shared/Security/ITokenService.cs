using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Security
{
    /// <summary>
    /// JWT 令牌服务接口
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// 生成 JWT 令牌
        /// </summary>
        /// <param name="userId">用户ID（写入 user_id claim）</param>
        /// <param name="userName">用户名（写入 user_name claim）</param>
        /// <param name="role">用户角色（写入 role claim）</param>
        /// <param name="systemUserId">
        /// 系统用户主键（仅 Admin 角色有值，写入 system_user_id claim）；
        /// 用于 ICurrentUser.SystemUserId 解析，避免与 UserId 类型混淆
        /// </param>
        string GenerateToken(string userId, string userName, UserRole role, long? systemUserId = null);

        /// <summary>
        /// 校验 JWT 令牌
        /// </summary>
        (string userId, string userName, UserRole role)? ValidateToken(string token);
    }
}