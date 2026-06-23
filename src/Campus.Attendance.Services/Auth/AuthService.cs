using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Auth;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Campus.Attendance.Services.Auth;

/// <summary>
/// 认证服务实现：依次按管理员/教师/学生身份校验密码，校验通过后颁发 JWT 令牌
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文、令牌服务与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tokenService">JWT 令牌服务</param>
    /// <param name="logger">日志器</param>
    public AuthService(IDbContext dbContext, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// 异步登录认证：依次按管理员/教师/学生身份校验
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录成功返回 LoginResult，失败返回 null</returns>
    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 1. 优先按用户名查 SystemUser（管理员）
        var admin = await db.Queryable<SystemUser>()
            .FirstAsync(u => u.Username == request.Username && !u.IsDeleted);
        if (admin is not null)
        {
            if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.Password))
            {
                _logger.LogWarning("管理员 {Username} 密码校验失败", request.Username);
                return null;
            }

            return BuildLoginResult(admin.Username, admin.RealName, UserRole.Admin);
        }

        // 2. 按工号查 Teacher
        var teacher = await db.Queryable<Teacher>()
            .FirstAsync(t => t.Id == request.Username && !t.IsDeleted);
        if (teacher is not null)
        {
            if (!BCrypt.Net.BCrypt.Verify(request.Password, teacher.Password))
            {
                _logger.LogWarning("教师 {TeacherId} 密码校验失败", request.Username);
                return null;
            }

            // 教师角色映射：TeacherRole.Counselor -> UserRole.Counselor，其他 -> UserRole.Teacher
            var userRole = teacher.Role == TeacherRole.Counselor ? UserRole.Counselor : UserRole.Teacher;
            return BuildLoginResult(teacher.Id, teacher.Name, userRole);
        }

        // 3. 按学号查 Student
        var student = await db.Queryable<Student>()
            .FirstAsync(s => s.Id == request.Username && !s.IsDeleted);
        if (student is not null)
        {
            if (!BCrypt.Net.BCrypt.Verify(request.Password, student.Password))
            {
                _logger.LogWarning("学生 {StudentId} 密码校验失败", request.Username);
                return null;
            }

            return BuildLoginResult(student.Id, student.Name, UserRole.Student);
        }

        _logger.LogWarning("登录失败：未找到用户 {Username}", request.Username);
        return null;
    }

    /// <summary>
    /// 构造登录结果，包含生成的 JWT 令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <returns>登录结果</returns>
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
