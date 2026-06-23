using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Models.Auth;
using Campus.Attendance.Services.Auth;
using Campus.Attendance.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Campus.Attendance.Tests.Auth;

/// <summary>
/// AuthService 单元测试，使用 SQLite 内存数据库隔离测试
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly AuthService _authService;

    /// <summary>
    /// 构造函数，初始化测试上下文与 AuthService 实例
    /// </summary>
    public AuthServiceTests()
    {
        _dbContext = new TestDbContext();
        var tokenService = new TokenService(TestJwtConfigFactory.Create(), NullLogger<TokenService>.Instance);
        _authService = new AuthService(_dbContext, tokenService, NullLogger<AuthService>.Instance);
    }

    /// <summary>
    /// 管理员使用正确密码登录应返回包含 Token 的 LoginResult
    /// </summary>
    [Fact]
    public async Task LoginAsync_AdminWithCorrectPassword_ReturnsToken()
    {
        // Arrange
        await SeedAdminAsync("admin", "123456");

        // Act
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "123456"
        });

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.Equal("admin", result.UserId);
        Assert.Equal(UserRole.Admin.ToString(), result.Role);
    }

    /// <summary>
    /// 管理员使用错误密码登录应返回 null
    /// </summary>
    [Fact]
    public async Task LoginAsync_AdminWithWrongPassword_ReturnsNull()
    {
        // Arrange
        await SeedAdminAsync("admin", "123456");

        // Act
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "wrong"
        });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 不存在的用户登录应返回 null
    /// </summary>
    [Fact]
    public async Task LoginAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Username = "ghost",
            Password = "123456"
        });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 学生使用正确密码登录应返回 Student 角色
    /// </summary>
    [Fact]
    public async Task LoginAsync_StudentWithCorrectPassword_ReturnsStudentRole()
    {
        // Arrange
        await SeedStudentAsync("20220101", "220101");

        // Act
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Username = "20220101",
            Password = "220101"
        });

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.Equal("20220101", result.UserId);
        Assert.Equal(UserRole.Student.ToString(), result.Role);
    }

    /// <summary>
    /// 释放测试上下文资源
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 播种管理员账号
    /// </summary>
    private async Task SeedAdminAsync(string username, string password)
    {
        var admin = new SystemUser
        {
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Admin,
            RealName = "管理员",
            CreateTime = DateTime.UtcNow
        };
        await _dbContext.Client.Insertable(admin).ExecuteCommandAsync();
    }

    /// <summary>
    /// 播种学生账号
    /// </summary>
    private async Task SeedStudentAsync(string id, string password)
    {
        var student = new Student
        {
            Id = id,
            Name = "测试学生",
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Gender = "男",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022,
            Status = 0,
            CreateTime = DateTime.UtcNow
        };
        await _dbContext.Client.Insertable(student).ExecuteCommandAsync();
    }
}
