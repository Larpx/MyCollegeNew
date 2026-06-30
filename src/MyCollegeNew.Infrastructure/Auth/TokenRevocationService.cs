using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth
{
    /// <summary>
    /// JWT 撤销服务抽象接口，用于服务端维护令牌黑名单（M-3 修复）
    /// </summary>
    public interface ITokenRevocationService
    {
        /// <summary>
        /// 将指定 JWT 加入黑名单，TTL 为令牌剩余有效期
        /// </summary>
        /// <param name="jti">JWT ID（jti claim）</param>
        /// <param name="ttl">黑名单缓存存活时长（应等于 JWT 剩余有效期）</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task RevokeAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);

        /// <summary>
        /// 判断指定 JWT 是否已被撤销
        /// </summary>
        /// <param name="jti">JWT ID（jti claim）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>已在黑名单返回 true</returns>
        Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 基于 IDistributedCache 的 JWT 黑名单实现。
    /// 黑名单 TTL 与 JWT 剩余有效期一致，令牌过期后黑名单条目自动清除，避免无限增长。
    /// 分布式缓存底层为 Redis（生产）或内存缓存（开发），多实例部署时共享黑名单。
    /// </summary>
    public class TokenRevocationService : ITokenRevocationService
    {
        /// <summary>黑名单缓存键前缀</summary>
        private const string CacheKeyPrefix = "jwt:revoked:";

        private readonly IDistributedCache _cache;
        private readonly ILogger<TokenRevocationService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cache">分布式缓存</param>
        /// <param name="logger">日志器</param>
        public TokenRevocationService(IDistributedCache cache, ILogger<TokenRevocationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task RevokeAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(jti) || ttl <= TimeSpan.Zero)
            {
                return;
            }

            var cacheKey = CacheKeyPrefix + jti;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            await _cache.SetStringAsync(cacheKey, "1", options, cancellationToken);
            _logger.LogInformation("JWT {Jti} 已加入撤销黑名单，TTL {Ttl}s", jti, (int)ttl.TotalSeconds);
        }

        /// <inheritdoc />
        public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(jti))
            {
                return false;
            }

            var cacheKey = CacheKeyPrefix + jti;
            var value = await _cache.GetStringAsync(cacheKey, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
    }
}
