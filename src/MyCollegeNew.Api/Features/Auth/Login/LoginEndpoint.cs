using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login
{
    /// <summary>
    /// 登录端点映射
    /// </summary>
    public static class LoginEndpoint
    {
        /// <summary>
        /// 映射登录相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/login", async (LoginRequest request, IMediator mediator) =>
            {
                var result = await mediator.Send(new LoginCommand(request));
                return Results.Ok(result);
            })
            .WithName("Login")
            .WithSummary("用户登录")
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .Produces<ApiResponse<LoginResult>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);

            // M-3 修复：登出端点，将当前 JWT 加入撤销黑名单
            // 黑名单 TTL = JWT 剩余有效期，令牌过期后黑名单条目自动清除
            group.MapPost("/auth/logout", async (HttpContext context, ITokenRevocationService revocationService) =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authorization["Bearer ".Length..].Trim()
                    : null;

                if (string.IsNullOrEmpty(token))
                {
                    return Results.BadRequest(ApiResponse<object>.Fail("未提供有效的 Authorization 头", 400));
                }

                // 解析 jti 与剩余有效期
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    return Results.BadRequest(ApiResponse<object>.Fail("令牌格式无效", 400));
                }

                var jwt = handler.ReadJwtToken(token);
                var jti = jwt.Id;
                if (string.IsNullOrEmpty(jti))
                {
                    return Results.BadRequest(ApiResponse<object>.Fail("令牌缺少 jti 声明", 400));
                }

                var remaining = jwt.ValidTo - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    // 令牌已过期，无需撤销
                    return Results.Ok(ApiResponse<object>.Success("已登出"));
                }

                await revocationService.RevokeAsync(jti, remaining, context.RequestAborted);
                return Results.Ok(ApiResponse<object>.Success("已登出"));
            })
            .WithName("Logout")
            .WithSummary("用户登出（撤销当前 JWT）")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // L-2 修复：强制修改密码端点（学生首次登录使用随机初始密码后调用）
            // 复用登录速率限制，防止暴力枚举临时令牌
            group.MapPost("/auth/force-change-password", HandleForceChangePassword)
                .WithName("ForceChangePassword")
                .WithSummary("强制修改密码（首次登录）")
                .AllowAnonymous()
                .RequireRateLimiting("login")
                .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
                .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status429TooManyRequests);

            return group;
        }

        /// <summary>
        /// L-2 修复：处理强制修改密码请求
        /// 校验临时令牌 → 校验新密码复杂度 → 更新密码并清除 MustChangePassword 标记
        /// </summary>
        private static async Task<IResult> HandleForceChangePassword(
            ForceChangePasswordRequest request,
            IDistributedCache cache,
            IDbContext dbContext,
            IAuditService auditService,
            ILogger<Program> logger)
        {
            // 1. 校验临时令牌
            if (string.IsNullOrEmpty(request.TwoFactorToken) || string.IsNullOrEmpty(request.NewPassword))
            {
                return Results.Ok(ApiResponse<object>.Fail("临时令牌与新密码均不能为空", 400));
            }

            var cacheKey = $"force-pwd:{request.TwoFactorToken}";
            var cacheValue = await cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cacheValue))
            {
                return Results.Ok(ApiResponse<object>.Fail("临时令牌已过期，请重新登录", 401));
            }

            // 缓存格式：{role}:{userId}，当前仅支持 student
            var parts = cacheValue.Split(':');
            if (parts.Length < 2 || parts[0] != "student")
            {
                return Results.Ok(ApiResponse<object>.Fail("临时令牌无效", 401));
            }
            var studentId = parts[1];

            // 2. 校验新密码复杂度（与 L-1 ApplyPasswordPolicy 规则一致）
            var passwordError = ValidatePasswordComplexity(request.NewPassword);
            if (passwordError is not null)
            {
                return Results.Ok(ApiResponse<object>.Fail(passwordError, 400));
            }

            // 3. 更新学生密码并清除 MustChangePassword 标记
            var db = dbContext.Client;
            var student = await db.Queryable<Student>().FirstAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student is null)
            {
                return Results.Ok(ApiResponse<object>.Fail("用户不存在", 404));
            }

            student.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            student.MustChangePassword = false;
            student.UpdateTime = DateTime.UtcNow;
            await db.Updateable(student)
                .UpdateColumns(s => new { s.Password, s.MustChangePassword, s.UpdateTime })
                .ExecuteCommandAsync();

            // 4. 删除临时令牌，防止重复使用
            await cache.RemoveAsync(cacheKey);

            logger.LogInformation("学生 {StudentId} 首次登录改密成功", studentId);
            await auditService.LogAsync("首次登录改密", studentId, UserRole.Student);

            return Results.Ok(ApiResponse<object>.Success("密码修改成功，请使用新密码重新登录"));
        }

        /// <summary>
        /// L-2 修复：校验密码复杂度（与 L-1 ApplyPasswordPolicy 规则一致）
        /// </summary>
        /// <param name="password">待校验的密码明文</param>
        /// <returns>校验失败返回错误消息，成功返回 null</returns>
        private static string? ValidatePasswordComplexity(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "密码不能为空";
            }
            if (password.Length < 8)
            {
                return "密码长度不能少于8位";
            }
            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                return "密码必须包含至少一个大写字母";
            }
            if (!Regex.IsMatch(password, "[a-z]"))
            {
                return "密码必须包含至少一个小写字母";
            }
            if (!Regex.IsMatch(password, "[0-9]"))
            {
                return "密码必须包含至少一个数字";
            }
            return null;
        }
    }
}