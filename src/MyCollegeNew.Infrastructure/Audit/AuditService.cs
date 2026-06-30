using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Audit
{
    /// <summary>
    /// 审计日志服务实现，将敏感操作持久化到 audit_log 表用于合规取证（M-5 修复）
    /// </summary>
    /// <remarks>
    /// 故障容忍策略：审计日志写入失败仅记录日志不抛异常，避免影响主业务流程
    /// </remarks>
    public class AuditService : IAuditService
    {
        private readonly IDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<AuditService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="httpContextAccessor">HTTP 上下文访问器，用于解析客户端 IP</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="logger">日志器</param>
        public AuditService(
            IDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ICurrentUser currentUser,
            ILogger<AuditService> logger)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task LogAsync(string action, string? target = null, CancellationToken cancellationToken = default)
        {
            // 已认证场景：从 ICurrentUser 读取用户信息
            var userId = _currentUser.IsAuthenticated ? _currentUser.UserId : "anonymous";
            var role = _currentUser.IsAuthenticated ? _currentUser.Role : (UserRole?)null;
            return LogAsync(action, userId, role, target, cancellationToken);
        }

        /// <inheritdoc />
        public async Task LogAsync(string action, string userId, UserRole? role, string? target = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var ipAddress = ResolveClientIpAddress();

                var auditLog = new AuditLog
                {
                    UserId = Truncate(userId, 32),
                    UserRole = role ?? default,
                    Action = Truncate(action, 64),
                    Target = target is null ? null : Truncate(target, 128),
                    IpAddress = ipAddress is null ? null : Truncate(ipAddress, 64),
                    CreateTime = DateTime.UtcNow
                };

                var db = _dbContext.Client;
                await db.Insertable(auditLog).ExecuteCommandAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // 审计日志写入失败不应中断主业务流程，仅记录错误日志便于运维排查
                _logger.LogError(ex, "审计日志写入失败：Action={Action}, UserId={UserId}, Target={Target}", action, userId, target);
            }
        }

        /// <summary>
        /// 解析客户端真实 IP 地址，优先取 X-Forwarded-For 头（反向代理场景）
        /// </summary>
        /// <returns>IP 地址字符串；无法解析时返回 null</returns>
        private string? ResolveClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            // 反向代理场景下 X-Forwarded-For 第一段为真实客户端 IP
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(firstIp))
                {
                    return firstIp;
                }
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// 截断字符串到指定最大长度，避免数据库列长度溢出
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>截断后的字符串</returns>
        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
