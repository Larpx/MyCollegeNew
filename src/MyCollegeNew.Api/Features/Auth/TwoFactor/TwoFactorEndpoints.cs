using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using QRCoder;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.TwoFactor
{
    /// <summary>
    /// TOTP 二次验证端点映射
    /// </summary>
    public static class TwoFactorEndpoints
    {
        /// <summary>
        /// 映射二次验证相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapTwoFactorEndpoints(this RouteGroupBuilder group)
        {
            // 获取 TOTP 绑定信息（未绑定用户调用）
            group.MapPost("/2fa/setup", HandleSetup)
                .WithName("TwoFactorSetup")
                .WithSummary("获取 TOTP 绑定信息")
                .AllowAnonymous()
                .Produces<ApiResponse<TwoFactorSetupResult>>(StatusCodes.Status200OK)
                .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // 验证 TOTP 码（已绑定用户调用）
            // M-2：独立速率限制（每 IP 每分钟 10 次），配合 5 次失败锁定机制防止暴力破解
            group.MapPost("/2fa/verify", HandleVerify)
                .WithName("TwoFactorVerify")
                .WithSummary("验证 TOTP 验证码")
                .AllowAnonymous()
                .RequireRateLimiting("twofa")
                .Produces<ApiResponse<LoginResult>>(StatusCodes.Status200OK)
                .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status429TooManyRequests);

            // 绑定 TOTP（未绑定用户首次设置）
            // M-2：独立速率限制（每 IP 每分钟 10 次），配合 5 次失败锁定机制防止暴力破解
            group.MapPost("/2fa/bind", HandleBind)
                .WithName("TwoFactorBind")
                .WithSummary("绑定 TOTP 并登录")
                .AllowAnonymous()
                .RequireRateLimiting("twofa")
                .Produces<ApiResponse<LoginResult>>(StatusCodes.Status200OK)
                .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status429TooManyRequests);

            return group;
        }

        /// <summary>
        /// 解析 2FA 临时令牌缓存值。
        /// 缓存格式：{userId}:{role}:{hasSecret}:{systemUserId}
        /// systemUserId 段为 Admin 角色的 SystemUser.Id 字符串（其他角色为空）
        /// </summary>
        /// <param name="cacheValue">缓存原始值</param>
        /// <returns>解析结果；格式不合法返回 null</returns>
        private static TwoFactorCachePayload? ParseCacheValue(string cacheValue)
        {
            var parts = cacheValue.Split(':');
            if (parts.Length < 3)
            {
                return null;
            }

            var userId = parts[0];
            var roleString = parts[1];
            var hasSecret = parts[2] == "True";
            // 第 4 段为 systemUserId（H-3/H-4 修复时新增），旧缓存无此段时为 null
            var systemUserIdString = parts.Length >= 4 ? parts[3] : string.Empty;
            long? systemUserId = long.TryParse(systemUserIdString, out var sid) ? sid : null;

            return new TwoFactorCachePayload(userId, roleString, hasSecret, systemUserId);
        }

        /// <summary>2FA 缓存载荷</summary>
        private sealed record TwoFactorCachePayload(string UserId, string RoleString, bool HasSecret, long? SystemUserId);

        /// <summary>
        /// 获取 TOTP 绑定信息：生成新的密钥、二维码 URI 和 Base64 图片
        /// </summary>
        private static async Task<IResult> HandleSetup(
            TwoFactorSetupRequest request,
            TotpService totpService,
            IDistributedCache cache,
            IDbContext dbContext,
            ILogger<Program> logger)
        {
            // 从缓存读取用户信息
            var cacheKey = $"2fa:{request.TwoFactorToken}";
            var cacheValue = await cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cacheValue))
            {
                return Results.Ok(ApiResponse<TwoFactorSetupResult>.Fail("临时令牌已过期，请重新登录", 401));
            }

            var payload = ParseCacheValue(cacheValue);
            if (payload is null)
            {
                return Results.Ok(ApiResponse<TwoFactorSetupResult>.Fail("临时令牌无效", 401));
            }

            // 已绑定的用户不应该调用 setup 接口
            if (payload.HasSecret)
            {
                return Results.Ok(ApiResponse<TwoFactorSetupResult>.Fail("已绑定 TOTP，请使用验证接口", 400));
            }

            // 生成新的 TOTP 密钥
            var secret = totpService.GenerateSecret();
            var otpAuthUri = totpService.GenerateOtpAuthUri(secret, payload.UserId);

            // 使用 QRCoder 生成二维码
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(8);
            var qrCodeBase64 = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);

            // 将临时 secret 存入缓存，绑定验证时使用
            var setupCacheKey = $"2fa-setup:{request.TwoFactorToken}";
            var setupCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await cache.SetStringAsync(setupCacheKey, secret, setupCacheOptions);

            logger.LogInformation("用户 {UserId} 请求 TOTP 绑定信息", payload.UserId);

            return Results.Ok(ApiResponse<TwoFactorSetupResult>.Success(new TwoFactorSetupResult
            {
                Secret = secret,
                OtpAuthUri = otpAuthUri,
                QrCodeBase64 = qrCodeBase64
            }));
        }

        /// <summary>
        /// 验证 TOTP 验证码：已绑定用户输入验证码后获取正式 JWT
        /// </summary>
        private static async Task<IResult> HandleVerify(
            TwoFactorVerifyRequest request,
            TotpService totpService,
            ITokenService tokenService,
            IDistributedCache cache,
            IDbContext dbContext,
            IAuditService auditService,
            ILogger<Program> logger)
        {
            // 从缓存读取用户信息
            var cacheKey = $"2fa:{request.TwoFactorToken}";
            var cacheValue = await cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cacheValue))
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("临时令牌已过期，请重新登录", 401));
            }

            var payload = ParseCacheValue(cacheValue);
            if (payload is null)
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("临时令牌无效", 401));
            }

            // 未绑定的用户不应该调用 verify 接口
            if (!payload.HasSecret)
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("未绑定 TOTP，请先绑定", 400));
            }

            if (!Enum.TryParse<UserRole>(payload.RoleString, out var role))
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("用户角色无效", 401));
            }

            // M-2：从缓存读取失败次数，超过 5 次直接使令牌失效
            var failCountKey = $"2fa-fail:{request.TwoFactorToken}";
            var failCountValue = await cache.GetStringAsync(failCountKey);
            if (int.TryParse(failCountValue, out var failCount) && failCount >= 5)
            {
                await cache.RemoveAsync(cacheKey);
                await cache.RemoveAsync(failCountKey);
                logger.LogWarning("用户 {UserId} 2FA 验证失败次数超限，临时令牌已失效", payload.UserId);
                // M-5：审计日志记录 2FA 锁定事件
                await auditService.LogAsync("2FA验证失败-锁定", payload.UserId, role, cancellationToken: default);
                return Results.Ok(ApiResponse<LoginResult>.Fail("验证失败次数过多，请重新登录", 401));
            }

            // 从数据库读取 TwoFactorSecret
            var db = dbContext.Client;
            var secret = await GetUserTwoFactorSecretAsync(db, payload.UserId, role);
            if (string.IsNullOrEmpty(secret))
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("未找到 TOTP 绑定信息", 401));
            }

            // 校验验证码（M-1：TotpService 异步版本内部已做重放保护，replayKey=userId）
            if (!await totpService.VerifyCodeAsync(secret, request.Code, payload.UserId, cache))
            {
                // 累加失败次数，5 分钟内累计 5 次锁定
                var newFailCount = failCount + 1;
                var failOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                await cache.SetStringAsync(failCountKey, newFailCount.ToString(), failOptions);

                logger.LogWarning("用户 {UserId} TOTP 验证码错误，失败次数 {FailCount}", payload.UserId, newFailCount);
                return Results.Ok(ApiResponse<LoginResult>.Fail("验证码错误", 401));
            }

            // 验证通过：删除缓存中的临时 token 与失败计数
            await cache.RemoveAsync(cacheKey);
            await cache.RemoveAsync(failCountKey);

            // 获取用户名
            var userName = await GetUserNameAsync(db, payload.UserId, role);

            // 生成正式 JWT，传入 systemUserId（仅 Admin 角色有值）
            var token = tokenService.GenerateToken(payload.UserId, userName, role, payload.SystemUserId);
            logger.LogInformation("用户 {UserId} TOTP 验证通过，已颁发 JWT", payload.UserId);
            // M-5：审计日志记录 2FA 验证成功
            await auditService.LogAsync("2FA验证成功-登录", payload.UserId, role, cancellationToken: default);

            return Results.Ok(ApiResponse<LoginResult>.Success(new LoginResult
            {
                Token = token,
                UserId = payload.UserId,
                UserName = userName,
                Role = role.ToString()
            }));
        }

        /// <summary>
        /// 绑定 TOTP：未绑定用户首次设置，验证通过后写入数据库并颁发 JWT
        /// </summary>
        private static async Task<IResult> HandleBind(
            TwoFactorBindRequest request,
            TotpService totpService,
            ITokenService tokenService,
            IDistributedCache cache,
            IDbContext dbContext,
            IAuditService auditService,
            ILogger<Program> logger)
        {
            // 从缓存读取用户信息
            var cacheKey = $"2fa:{request.TwoFactorToken}";
            var cacheValue = await cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cacheValue))
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("临时令牌已过期，请重新登录", 401));
            }

            var payload = ParseCacheValue(cacheValue);
            if (payload is null)
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("临时令牌无效", 401));
            }

            if (payload.HasSecret)
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("已绑定 TOTP，请使用验证接口", 400));
            }

            if (!Enum.TryParse<UserRole>(payload.RoleString, out var role))
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("用户角色无效", 401));
            }

            // M-2：从缓存读取失败次数，超过 5 次直接使令牌失效
            var failCountKey = $"2fa-fail:{request.TwoFactorToken}";
            var failCountValue = await cache.GetStringAsync(failCountKey);
            if (int.TryParse(failCountValue, out var failCount) && failCount >= 5)
            {
                await cache.RemoveAsync(cacheKey);
                await cache.RemoveAsync(failCountKey);
                logger.LogWarning("用户 {UserId} 2FA 绑定失败次数超限，临时令牌已失效", payload.UserId);
                // M-5：审计日志记录 2FA 绑定锁定事件
                await auditService.LogAsync("2FA绑定失败-锁定", payload.UserId, role, cancellationToken: default);
                return Results.Ok(ApiResponse<LoginResult>.Fail("验证失败次数过多，请重新登录", 401));
            }

            // 从缓存读取 setup 阶段生成的临时 secret
            var setupCacheKey = $"2fa-setup:{request.TwoFactorToken}";
            var cachedSecret = await cache.GetStringAsync(setupCacheKey);
            if (string.IsNullOrEmpty(cachedSecret))
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("绑定信息已过期，请重新获取", 401));
            }

            // 校验 secret 是否匹配（防止篡改）
            if (cachedSecret != request.Secret)
            {
                return Results.Ok(ApiResponse<LoginResult>.Fail("密钥不匹配", 400));
            }

            // 校验验证码（M-1：TotpService 异步版本内部已做重放保护，绑定阶段用 TwoFactorToken 作为 replayKey）
            if (!await totpService.VerifyCodeAsync(request.Secret, request.Code, $"setup:{request.TwoFactorToken}", cache))
            {
                var newFailCount = failCount + 1;
                var failOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                await cache.SetStringAsync(failCountKey, newFailCount.ToString(), failOptions);

                logger.LogWarning("用户 {UserId} TOTP 绑定验证码错误，失败次数 {FailCount}", payload.UserId, newFailCount);
                return Results.Ok(ApiResponse<LoginResult>.Fail("验证码错误", 401));
            }

            // 将 secret 写入数据库对应用户的 TwoFactorSecret 字段
            var db = dbContext.Client;
            await UpdateUserTwoFactorSecretAsync(db, payload.UserId, role, request.Secret);

            // 删除缓存中的临时 token、setup secret 与失败计数
            await cache.RemoveAsync(cacheKey);
            await cache.RemoveAsync(setupCacheKey);
            await cache.RemoveAsync(failCountKey);

            // 获取用户名
            var userName = await GetUserNameAsync(db, payload.UserId, role);

            // 生成正式 JWT，传入 systemUserId（仅 Admin 角色有值）
            var token = tokenService.GenerateToken(payload.UserId, userName, role, payload.SystemUserId);
            logger.LogInformation("用户 {UserId} TOTP 绑定成功，已颁发 JWT", payload.UserId);
            // M-5：审计日志记录 2FA 绑定成功
            await auditService.LogAsync("2FA绑定成功", payload.UserId, role, cancellationToken: default);

            return Results.Ok(ApiResponse<LoginResult>.Success(new LoginResult
            {
                Token = token,
                UserId = payload.UserId,
                UserName = userName,
                Role = role.ToString()
            }));
        }

        /// <summary>
        /// 从数据库读取用户的 TwoFactorSecret
        /// </summary>
        private static async Task<string?> GetUserTwoFactorSecretAsync(SqlSugar.ISqlSugarClient db, string userId, UserRole role)
        {
            return role switch
            {
                UserRole.Admin => (await db.Queryable<SystemUser>()
                    .FirstAsync(u => u.Id.ToString() == userId && !u.IsDeleted))?.TwoFactorSecret,
                UserRole.Teacher or UserRole.Counselor => (await db.Queryable<Teacher>()
                    .FirstAsync(t => t.Id == userId && !t.IsDeleted))?.TwoFactorSecret,
                UserRole.Student => (await db.Queryable<Student>()
                    .FirstAsync(s => s.Id == userId && !s.IsDeleted))?.TwoFactorSecret,
                _ => null
            };
        }

        /// <summary>
        /// 从数据库读取用户名
        /// </summary>
        private static async Task<string> GetUserNameAsync(SqlSugar.ISqlSugarClient db, string userId, UserRole role)
        {
            return role switch
            {
                UserRole.Admin => (await db.Queryable<SystemUser>()
                    .FirstAsync(u => u.Id.ToString() == userId && !u.IsDeleted))?.RealName ?? userId,
                UserRole.Teacher or UserRole.Counselor => (await db.Queryable<Teacher>()
                    .FirstAsync(t => t.Id == userId && !t.IsDeleted))?.Name ?? userId,
                UserRole.Student => (await db.Queryable<Student>()
                    .FirstAsync(s => s.Id == userId && !s.IsDeleted))?.Name ?? userId,
                _ => userId
            };
        }

        /// <summary>
        /// 将 TOTP Secret 写入数据库对应用户
        /// </summary>
        private static async Task UpdateUserTwoFactorSecretAsync(SqlSugar.ISqlSugarClient db, string userId, UserRole role, string secret)
        {
            switch (role)
            {
                case UserRole.Admin:
                    var admin = await db.Queryable<SystemUser>().FirstAsync(u => u.Id.ToString() == userId && !u.IsDeleted);
                    if (admin is not null)
                    {
                        admin.TwoFactorSecret = secret;
                        admin.UpdateTime = DateTime.UtcNow;
                        await db.Updateable(admin).UpdateColumns(u => new { u.TwoFactorSecret, u.UpdateTime }).ExecuteCommandAsync();
                    }
                    break;
                case UserRole.Teacher:
                case UserRole.Counselor:
                    var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == userId && !t.IsDeleted);
                    if (teacher is not null)
                    {
                        teacher.TwoFactorSecret = secret;
                        teacher.UpdateTime = DateTime.UtcNow;
                        await db.Updateable(teacher).UpdateColumns(t => new { t.TwoFactorSecret, t.UpdateTime }).ExecuteCommandAsync();
                    }
                    break;
                case UserRole.Student:
                    var student = await db.Queryable<Student>().FirstAsync(s => s.Id == userId && !s.IsDeleted);
                    if (student is not null)
                    {
                        student.TwoFactorSecret = secret;
                        student.UpdateTime = DateTime.UtcNow;
                        await db.Updateable(student).UpdateColumns(s => new { s.TwoFactorSecret, s.UpdateTime }).ExecuteCommandAsync();
                    }
                    break;
            }
        }
    }
}
