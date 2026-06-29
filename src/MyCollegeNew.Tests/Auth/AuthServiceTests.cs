using Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login;
using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Auth
{
    /// <summary>
    /// LoginHandler 单元测试，使用 SQLite 内存数据库隔离测试
    /// </summary>
    public class LoginHandlerTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly LoginHandler _loginHandler;

        /// <summary>
        /// 构造函数，初始化测试上下文与 LoginHandler 实例
        /// </summary>
        public LoginHandlerTests()
        {
            _dbContext = new TestDbContext();
            var tokenService = new TokenService(TestJwtConfigFactory.Create(), NullLogger<TokenService>.Instance);
            var cache = Mock.Of<IDistributedCache>();
            _loginHandler = new LoginHandler(_dbContext, tokenService, cache, NullLogger<LoginHandler>.Instance);
        }

        /// <summary>
        /// 管理员使用正确密码登录应返回包含 Token 的 LoginResult
        /// </summary>
        [Fact]
        public async Task Handle_AdminWithCorrectPassword_ReturnsToken()
        {
            // Arrange
            await SeedAdminAsync("admin", "123456");
            var command = new LoginCommand(new LoginRequest
            {
                Username = "admin",
                Password = "123456"
            });

            // Act
            var result = await _loginHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            // 密码校验通过后进入二次验证流程，返回 2FA 临时令牌而非 JWT
            Assert.True(result.Data!.RequiresTwoFactor);
            Assert.False(string.IsNullOrEmpty(result.Data.TwoFactorToken));
            // 管理员 userId 为 SystemUser 自增主键的字符串形式
            Assert.Equal("1", result.Data.UserId);
            Assert.Equal(UserRole.Admin.ToString(), result.Data.Role);
        }

        /// <summary>
        /// 管理员使用错误密码登录应返回失败响应
        /// </summary>
        [Fact]
        public async Task Handle_AdminWithWrongPassword_ReturnsFail()
        {
            // Arrange
            await SeedAdminAsync("admin", "123456");
            var command = new LoginCommand(new LoginRequest
            {
                Username = "admin",
                Password = "wrong"
            });

            // Act
            var result = await _loginHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(401, result.Code);
            Assert.Null(result.Data);
        }

        /// <summary>
        /// 不存在的用户登录应返回失败响应
        /// </summary>
        [Fact]
        public async Task Handle_NonExistentUser_ReturnsFail()
        {
            // Arrange
            var command = new LoginCommand(new LoginRequest
            {
                Username = "ghost",
                Password = "123456"
            });

            // Act
            var result = await _loginHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(401, result.Code);
            Assert.Null(result.Data);
        }

        /// <summary>
        /// 学生使用正确密码登录应返回 Student 角色
        /// </summary>
        [Fact]
        public async Task Handle_StudentWithCorrectPassword_ReturnsStudentRole()
        {
            // Arrange
            await SeedStudentAsync("20220101", "220101");
            var command = new LoginCommand(new LoginRequest
            {
                Username = "20220101",
                Password = "220101"
            });

            // Act
            var result = await _loginHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            // 密码校验通过后进入二次验证流程，返回 2FA 临时令牌而非 JWT
            Assert.True(result.Data!.RequiresTwoFactor);
            Assert.False(string.IsNullOrEmpty(result.Data.TwoFactorToken));
            Assert.Equal("20220101", result.Data.UserId);
            Assert.Equal(UserRole.Student.ToString(), result.Data.Role);
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
}