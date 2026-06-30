using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Security
{
    /// <summary>
    /// 审计日志服务接口，负责记录敏感操作的不可篡改审计痕迹
    /// </summary>
    /// <remarks>
    /// 适用场景（M-5 修复）：登录成败、密码修改/重置、2FA 绑定/重置、用户增删、批量导入等敏感操作
    /// </remarks>
    public interface IAuditService
    {
        /// <summary>
        /// 记录审计日志（已认证场景，用户信息从 ICurrentUser 读取）
        /// </summary>
        /// <param name="action">操作动作描述（如 "修改密码"）</param>
        /// <param name="target">操作目标对象标识（如被删除用户的 Id）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        Task LogAsync(string action, string? target = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 记录审计日志（未认证场景如登录/2FA，需显式传入用户信息）
        /// </summary>
        /// <param name="action">操作动作描述</param>
        /// <param name="userId">操作用户标识（学号/工号/用户名）</param>
        /// <param name="role">用户角色；未知角色传 <c>null</c></param>
        /// <param name="target">操作目标对象标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        Task LogAsync(string action, string userId, UserRole? role, string? target = null, CancellationToken cancellationToken = default);
    }
}
