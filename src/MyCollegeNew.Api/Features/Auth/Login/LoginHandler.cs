using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login
{
    /// <summary>
    /// 登录处理器：依次按管理员/教师/学生身份校验密码，校验通过后颁发 JWT 令牌
    /// </summary>
    public class LoginHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResult>>
    {
        private readonly IDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly ILogger<LoginHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="tokenService">JWT 令牌服务</param>
        /// <param name="logger">日志器</param>
        public LoginHandler(IDbContext dbContext, ITokenService tokenService, ILogger<LoginHandler> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
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

                return ApiResponse<LoginResult>.Success(BuildLoginResult(admin.Username, admin.RealName, UserRole.Admin));
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
                return ApiResponse<LoginResult>.Success(BuildLoginResult(teacher.Id, teacher.Name, userRole));
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

                return ApiResponse<LoginResult>.Success(BuildLoginResult(student.Id, student.Name, UserRole.Student));
            }

            _logger.LogWarning("登录失败：未找到用户 {Username}", request.Username);
            return ApiResponse<LoginResult>.Fail("用户名或密码错误", 401);
        }

        /// <summary>
        /// 构造登录结果，包含生成的 JWT 令牌
        /// </summary>
        private LoginResult BuildLoginResult(string userId, string userName, UserRole role)
        {
            var token = _tokenService.GenerateToken(userId, userName, role);
            return new LoginResult
            {
                Token = token,
                UserId = userId,
                UserName = userName,
                Role = role.ToString()
            };
        }
    }
}