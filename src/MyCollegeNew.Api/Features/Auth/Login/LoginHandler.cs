using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login
{
    /// <summary>
    /// 登录处理器：依次按管理员/教师/学生身份校验密码，
    /// 密码校验通过后进入二次验证流程（未绑定需先绑定 TOTP，已绑定需输入验证码）
    /// </summary>
    public class LoginHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResult>>
    {
        private readonly IDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<LoginHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="tokenService">JWT 令牌服务</param>
        /// <param name="cache">分布式缓存（用于存储 2FA 临时令牌）</param>
        /// <param name="logger">日志器</param>
        public LoginHandler(IDbContext dbContext, ITokenService tokenService, IDistributedCache cache, ILogger<LoginHandler> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 处理登录命令
        /// </summary>
        public async Task<ApiResponse<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var request = command.Request;

            // 1. 优先按用户名查 SystemUser（管理员）
            var admin = await db.Queryable<SystemUser>()
                .FirstAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);
            if (admin is not null)
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.Password))
                {
                    _logger.LogWarning("管理员 {Username} 密码校验失败", request.Username);
                    return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
                }

                return await BuildTwoFactorResultAsync(admin.Id.ToString(), admin.RealName, UserRole.Admin, admin.TwoFactorSecret, cancellationToken);
            }

            // 2. 按工号查 Teacher
            var teacher = await db.Queryable<Teacher>()
                .FirstAsync(t => t.Id == request.Username && !t.IsDeleted, cancellationToken);
            if (teacher is not null)
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, teacher.Password))
                {
                    _logger.LogWarning("教师 {TeacherId} 密码校验失败", request.Username);
                    return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
                }

                var userRole = teacher.Role == TeacherRole.Counselor ? UserRole.Counselor : UserRole.Teacher;
                return await BuildTwoFactorResultAsync(teacher.Id, teacher.Name, userRole, teacher.TwoFactorSecret, cancellationToken);
            }

            // 3. 按学号查 Student
            var student = await db.Queryable<Student>()
                .FirstAsync(s => s.Id == request.Username && !s.IsDeleted, cancellationToken);
            if (student is not null)
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, student.Password))
                {
                    _logger.LogWarning("学生 {StudentId} 密码校验失败", request.Username);
                    return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
                }

                return await BuildTwoFactorResultAsync(student.Id, student.Name, UserRole.Student, student.TwoFactorSecret, cancellationToken);
            }

            _logger.LogWarning("登录失败：未找到用户 {Username}", request.Username);
            return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
        }

        /// <summary>
        /// 构造二次验证结果：生成临时令牌存入缓存，不直接返回 JWT
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="role">用户角色</param>
        /// <param name="twoFactorSecret">用户已绑定的 TOTP 密钥（null 表示未绑定）</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task<ApiResponse<LoginResult>> BuildTwoFactorResultAsync(
            string userId, string userName, UserRole role, string? twoFactorSecret, CancellationToken cancellationToken)
        {
            var hasSecret = !string.IsNullOrEmpty(twoFactorSecret);
            var twoFactorToken = Guid.NewGuid().ToString("N");

            // 临时 token 存入缓存，5 分钟过期，格式：{userId}:{role}:{hasSecret}
            var cacheValue = $"{userId}:{role}:{hasSecret}";
            var cacheKey = $"2fa:{twoFactorToken}";
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync(cacheKey, cacheValue, options, cancellationToken);

            _logger.LogInformation("用户 {UserId} 密码校验通过，进入二次验证流程（已绑定: {HasSecret}）", userId, hasSecret);

            return ApiResponse<LoginResult>.Success(new LoginResult
            {
                RequiresTwoFactor = true,
                HasTwoFactorSecret = hasSecret,
                TwoFactorToken = twoFactorToken,
                UserId = userId,
                UserName = userName,
                Role = role.ToString()
            });
        }
    }
}
