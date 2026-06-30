using Microsoft.Extensions.Caching.Distributed;
using OtpNet;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth
{
    /// <summary>
    /// TOTP 二次验证服务：生成密钥、生成二维码 URI、验证码校验
    /// </summary>
    public class TotpService
    {
        /// <summary>已使用验证码缓存键前缀，配合 M-1 重放保护使用</summary>
        private const string UsedCodeCachePrefix = "totp:used:";

        /// <summary>已使用验证码缓存 TTL（秒），覆盖 ±1 时间窗口（90 秒）确保同一验证码不可重放</summary>
        private const int UsedCodeCacheTtlSeconds = 90;

        /// <summary>生成新的 TOTP 密钥（20 字节，Base32 编码）</summary>
        /// <returns>Base32 编码的密钥字符串</returns>
        public string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        /// <summary>生成 OTPAuth URI（用于二维码扫描）</summary>
        /// <param name="secret">Base32 密钥</param>
        /// <param name="userId">用户标识</param>
        /// <param name="issuer">签发者名称</param>
        /// <returns>otpauth:// 格式的 URI</returns>
        public string GenerateOtpAuthUri(string secret, string userId, string issuer = "考勤管理系统")
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedUserId = Uri.EscapeDataString(userId);
            return $"otpauth://totp/{encodedIssuer}:{encodedUserId}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&period=30&digits=6";
        }

        /// <summary>校验 TOTP 验证码（无重放保护，仅用于不涉及安全敏感的场景）</summary>
        /// <param name="secret">Base32 密钥</param>
        /// <param name="code">用户输入的 6 位验证码</param>
        /// <param name="window">时间窗口容差（默认 ±1，即前后 30 秒都有效）</param>
        /// <returns>验证通过返回 true，否则 false</returns>
        public bool VerifyCode(string secret, string code, int window = 1)
        {
#if DEBUG
            // DEBUG 模式后门：输入 888888 直接通过，便于测试避免每次查动态密码
            if (code == "888888") return true;
#endif
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key, step: 30, totpSize: 6, mode: OtpHashMode.Sha1);

            // 尝试在指定时间窗口内匹配验证码
            var currentTime = DateTimeOffset.UtcNow;
            var windowStart = -window;
            var windowEnd = window;

            for (var i = windowStart; i <= windowEnd; i++)
            {
                var candidate = totp.ComputeTotp(currentTime.AddSeconds(i * 30).DateTime);
                if (string.Equals(candidate, code, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 校验 TOTP 验证码（M-1 重放保护版本，符合 RFC 6238 §5.2 强制要求）。
        /// 验证成功后将已使用验证码写入缓存（TTL=90 秒），同一用户/会话的同一验证码在窗口期内不可重放。
        /// </summary>
        /// <param name="secret">Base32 密钥</param>
        /// <param name="code">用户输入的 6 位验证码</param>
        /// <param name="replayKey">重放保护缓存键标识（如 userId 或 2FA 临时令牌），不同用户的同一验证码互不影响</param>
        /// <param name="cache">分布式缓存</param>
        /// <param name="window">时间窗口容差（默认 ±1）</param>
        /// <returns>验证通过且未被重放返回 true，否则 false</returns>
        public async Task<bool> VerifyCodeAsync(string secret, string code, string replayKey, IDistributedCache cache, int window = 1)
        {
#if DEBUG
            // DEBUG 模式后门：输入 888888 直接通过，便于测试避免每次查动态密码
            // 注意：DEBUG 后门跳过重放保护，仅限本地测试使用
            if (code == "888888") return true;
#endif
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code) || string.IsNullOrEmpty(replayKey))
            {
                return false;
            }

            // 先检查该验证码是否已被使用，命中则直接拒绝（防重放）
            var usedCacheKey = $"{UsedCodeCachePrefix}{replayKey}:{code}";
            var usedValue = await cache.GetStringAsync(usedCacheKey);
            if (!string.IsNullOrEmpty(usedValue))
            {
                return false;
            }

            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key, step: 30, totpSize: 6, mode: OtpHashMode.Sha1);

            var currentTime = DateTimeOffset.UtcNow;
            var windowStart = -window;
            var windowEnd = window;

            for (var i = windowStart; i <= windowEnd; i++)
            {
                var candidate = totp.ComputeTotp(currentTime.AddSeconds(i * 30).DateTime);
                if (string.Equals(candidate, code, StringComparison.OrdinalIgnoreCase))
                {
                    // 验证通过：写入已使用标记，TTL 覆盖完整 ±1 时间窗口
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(UsedCodeCacheTtlSeconds)
                    };
                    await cache.SetStringAsync(usedCacheKey, "1", options);
                    return true;
                }
            }

            return false;
        }
    }
}
