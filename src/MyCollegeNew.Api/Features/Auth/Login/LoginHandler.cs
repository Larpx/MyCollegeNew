using Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Captcha;
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
        /// <summary>用于消除用户存在性时序侧信道的固定 BCrypt 哈希（哈希值与真实密码哈希算法/成本因子一致）</summary>
        private const string DummyPasswordHash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";

        private readonly IDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IDistributedCache _cache;
        private readonly IAuditService _auditService;
        private readonly ILogger<LoginHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="tokenService">JWT 令牌服务</param>
        /// <param name="cache">分布式缓存（用于存储 2FA 临时令牌）</param>
        /// <param name="auditService">审计日志服务（M-5：记录登录成败）</param>
        /// <param name="logger">日志器</param>
        public LoginHandler(IDbContext dbContext, ITokenService tokenService, IDistributedCache cache, IAuditService auditService, ILogger<LoginHandler> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _cache = cache;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// 处理登录命令
        /// </summary>
        public async Task<ApiResponse<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var request = command.Request;

            // 0. 密码校验前先校验滑块验证码 token（一次性，校验后即从缓存删除）
            // 防止前端绕过滑块验证码直接调用 API 暴力破解
            if (string.IsNullOrEmpty(request.CaptchaToken)
                || !await CaptchaEndpoints.ValidateCaptchaTokenAsync(request.CaptchaToken, _cache))
            {
                _logger.LogWarning("登录失败：滑块验证码校验未通过，用户名 {Username}", request.Username);
                await _auditService.LogAsync("登录失败-验证码错误", request.Username, null, cancellationToken: cancellationToken);
                return ApiResponse<LoginResult>.Fail("滑块验证码校验失败，请完成验证后重试", 401);
            }

            // 1. 优先按用户名查 SystemUser（管理员）
            var admin = await db.Queryable<SystemUser>()
                .FirstAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);
            if (admin is not null)
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.Password))
                {
                    _logger.LogWarning("管理员 {Username} 密码校验失败", request.Username);
                    await _auditService.LogAsync("登录失败-密码错误", request.Username, UserRole.Admin, cancellationToken: cancellationToken);
                    return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
                }

                await _auditService.LogAsync("登录成功-进入2FA", request.Username, UserRole.Admin, cancellationToken: cancellationToken);
                return await BuildTwoFactorResultAsync(admin.Id.ToString(), admin.Id, admin.RealName, UserRole.Admin, admin.TwoFactorSecret, cancellationToken);
            }

            // 2. 按工号查 Teacher
            var teacher = await db.Queryable<Teacher>()
                .FirstAsync(t => t.Id == request.Username && !t.IsDeleted, cancellationToken);
            if (teacher is not null)
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, teacher.Password))
                {
                    _logger.LogWarning("教师 {TeacherId} 密码校验失败", request.Username);
                    var teacherRoleForAudit = teacher.Role == TeacherRole.Counselor ? UserRole.Counselor : UserRole.Teacher;
                    await _auditService.LogAsync("登录失败-密码错误", request.Username, teacherRoleForAudit, cancellationToken: cancellationToken);
                    return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
                }

                var userRole = teacher.Role == TeacherRole.Counselor ? UserRole.Counselor : UserRole.Teacher;
                await _auditService.LogAsync("登录成功-进入2FA", request.Username, userRole, cancellationToken: cancellationToken);
                return await BuildTwoFactorResultAsync(teacher.Id, null, teacher.Name, userRole, teacher.TwoFactorSecret, cancellationToken);
            }

            // 3. 按学号查 Student
            var student = await db.Queryable<Student>()
                .FirstAsync(s => s.Id == request.Username && !s.IsDeleted, cancellationToken);
            if (student is not null)
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, student.Password))
                {
                    _logger.LogWarning("学生 {StudentId} 密码校验失败", request.Username);
                    await _auditService.LogAsync("登录失败-密码错误", request.Username, UserRole.Student, cancellationToken: cancellationToken);
                    return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
                }

                // L-2 修复：学生首次登录（使用 CSV 导入生成的随机初始密码）须先强制修改密码，跳过 2FA
                if (student.MustChangePassword)
                {
                    var forceToken = Guid.NewGuid().ToString("N");
                    var forceCacheKey = $"force-pwd:{forceToken}";
                    var forceCacheValue = $"student:{student.Id}";
                    var forceOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
                    await _cache.SetStringAsync(forceCacheKey, forceCacheValue, forceOptions, cancellationToken);

                    _logger.LogInformation("学生 {StudentId} 首次登录，要求强制修改密码", student.Id);
                    await _auditService.LogAsync("登录成功-要求改密", student.Id, UserRole.Student, cancellationToken: cancellationToken);

                    return ApiResponse<LoginResult>.Success(new LoginResult
                    {
                        MustChangePassword = true,
                        TwoFactorToken = forceToken,
                        UserId = student.Id,
                        UserName = student.Name,
                        Role = UserRole.Student.ToString()
                    });
                }

                await _auditService.LogAsync("登录成功-进入2FA", request.Username, UserRole.Student, cancellationToken: cancellationToken);
                return await BuildTwoFactorResultAsync(student.Id, null, student.Name, UserRole.Student, student.TwoFactorSecret, cancellationToken);
            }

            // L-3：用户不存在时执行一次 dummy BCrypt Verify，对齐密码校验耗时，消除用户存在性时序侧信道
            _ = BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash);
            _logger.LogWarning("登录失败：未找到用户 {Username}", request.Username);
            await _auditService.LogAsync("登录失败-用户不存在", request.Username, null, cancellationToken: cancellationToken);
            return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
        }

        /// <summary>
        /// 构造二次验证结果：生成临时令牌存入缓存，不直接返回 JWT
        /// </summary>
        /// <param name="userId">用户ID（写入 JWT user_id claim，Admin 为 SystemUser.Id 数字字符串）</param>
        /// <param name="systemUserId">系统用户主键（仅 Admin 角色有值，对应 SystemUser.Id，写入 system_user_id claim）</param>
        /// <param name="userName">用户名</param>
        /// <param name="role">用户角色</param>
        /// <param name="twoFactorSecret">用户已绑定的 TOTP 密钥（null 表示未绑定）</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task<ApiResponse<LoginResult>> BuildTwoFactorResultAsync(
            string userId, long? systemUserId, string userName, UserRole role, string? twoFactorSecret, CancellationToken cancellationToken)
        {
            var hasSecret = !string.IsNullOrEmpty(twoFactorSecret);
            var twoFactorToken = Guid.NewGuid().ToString("N");

            // 临时 token 存入缓存，5 分钟过期，格式：{userId}:{role}:{hasSecret}:{systemUserId}
            // systemUserId 仅 Admin 角色有值，用于 2FA 验证通过后写回 system_user_id claim
            var cacheValue = $"{userId}:{role}:{hasSecret}:{systemUserId?.ToString() ?? string.Empty}";
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
