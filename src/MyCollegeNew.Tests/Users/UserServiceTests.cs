using Larpx.PersonalTools.MyCollegeNew.Api.Features.Profile.ChangePassword;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Students;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Users
{
    /// <summary>
    /// StudentHandlers 与 ChangePasswordHandler 单元测试，使用 SQLite 内存数据库隔离测试
    /// </summary>
    public class UserServiceTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly StudentHandlers _studentHandlers;
        private readonly ChangePasswordHandler _changePasswordHandler;

        /// <summary>
        /// 构造函数，初始化测试上下文与 Handler 实例
        /// </summary>
        public UserServiceTests()
        {
            _dbContext = new TestDbContext();
            var auditService = new NullAuditService();
            _studentHandlers = new StudentHandlers(_dbContext, auditService, NullLogger<StudentHandlers>.Instance);
            _changePasswordHandler = new ChangePasswordHandler(_dbContext, auditService, NullLogger<ChangePasswordHandler>.Instance);
        }

        /// <summary>
        /// 创建学生使用合法 DTO 应返回创建后的学生信息
        /// </summary>
        [Fact]
        public async Task CreateStudentAsync_ValidDto_ReturnsCreatedStudent()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var dto = new StudentCreateDto
            {
                Id = "20220102",
                Name = "张三",
                Password = "123456",
                Gender = "男",
                DepartmentId = 1,
                MajorId = 1,
                ClassId = 1,
                Grade = 2022
            };

            // Act
            var result = await _studentHandlers.Handle(new CreateStudentCommand(dto), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal("20220102", result.Data!.Id);
            Assert.Equal("张三", result.Data.Name);
        }

        /// <summary>
        /// 创建学生使用重复学号应返回失败响应
        /// </summary>
        [Fact]
        public async Task CreateStudentAsync_DuplicateId_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await _studentHandlers.Handle(new CreateStudentCommand(new StudentCreateDto
            {
                Id = "20220103",
                Name = "李四",
                Password = "123456",
                Gender = "男",
                DepartmentId = 1,
                MajorId = 1,
                ClassId = 1,
                Grade = 2022
            }), CancellationToken.None);

            var dto = new StudentCreateDto
            {
                Id = "20220103",
                Name = "重复",
                Password = "123456",
                Gender = "女",
                DepartmentId = 1,
                MajorId = 1,
                ClassId = 1,
                Grade = 2022
            };

            // Act
            var result = await _studentHandlers.Handle(new CreateStudentCommand(dto), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("已存在", result.Message);
        }

        /// <summary>
        /// 删除已存在学生应将 IsDeleted 标记为 true
        /// </summary>
        [Fact]
        public async Task DeleteStudentAsync_ExistingId_SetsIsDeletedTrue()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await _studentHandlers.Handle(new CreateStudentCommand(new StudentCreateDto
            {
                Id = "20220104",
                Name = "王五",
                Password = "123456",
                Gender = "男",
                DepartmentId = 1,
                MajorId = 1,
                ClassId = 1,
                Grade = 2022
            }), CancellationToken.None);

            // Act
            var result = await _studentHandlers.Handle(new DeleteStudentCommand("20220104"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            var student = await _dbContext.Client.Queryable<Student>().FirstAsync(s => s.Id == "20220104");
            Assert.NotNull(student);
            Assert.True(student!.IsDeleted);
        }

        /// <summary>
        /// 批量导入合法 CSV 应返回成功计数
        /// </summary>
        [Fact]
        public async Task BatchImportStudentsAsync_ValidCsv_ReturnsSuccessCount()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var csv = "Id,Name,Gender,DepartmentId,MajorId,ClassId,Grade\n" +
                      "20220105,赵六,男,1,1,1,2022\n" +
                      "20220106,钱七,女,1,1,1,2022\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            // Act
            var result = await _studentHandlers.Handle(new BatchImportStudentsCommand(stream), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data!.SuccessCount);
            Assert.Equal(0, result.Data.FailedCount);
        }

        /// <summary>
        /// 修改密码使用正确旧密码应更新密码
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_CorrectOldPassword_UpdatesPassword()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await _studentHandlers.Handle(new CreateStudentCommand(new StudentCreateDto
            {
                Id = "20220107",
                Name = "孙八",
                Password = "123456",
                Gender = "男",
                DepartmentId = 1,
                MajorId = 1,
                ClassId = 1,
                Grade = 2022
            }), CancellationToken.None);

            var dto = new PasswordChangeDto
            {
                OldPassword = "123456",
                NewPassword = "newpass123"
            };

            // Act
            var result = await _changePasswordHandler.Handle(new ChangePasswordCommand(dto, "20220107", UserRole.Student), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            var student = await _dbContext.Client.Queryable<Student>().FirstAsync(s => s.Id == "20220107");
            Assert.NotNull(student);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass123", student!.Password));
        }

        /// <summary>
        /// 修改密码使用错误旧密码应返回失败响应
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_WrongOldPassword_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await _studentHandlers.Handle(new CreateStudentCommand(new StudentCreateDto
            {
                Id = "20220108",
                Name = "周九",
                Password = "123456",
                Gender = "男",
                DepartmentId = 1,
                MajorId = 1,
                ClassId = 1,
                Grade = 2022
            }), CancellationToken.None);

            var dto = new PasswordChangeDto
            {
                OldPassword = "wrong",
                NewPassword = "newpass123"
            };

            // Act
            var result = await _changePasswordHandler.Handle(new ChangePasswordCommand(dto, "20220108", UserRole.Student), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("旧密码", result.Message);
        }

        /// <summary>
        /// 教师修改密码使用正确旧密码应更新密码
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_TeacherWithCorrectOldPassword_UpdatesPassword()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var teacher = new Teacher
            {
                Id = "T010",
                Name = "测试教师",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Gender = "男",
                DepartmentId = 1,
                Role = TeacherRole.Teacher,
                CreateTime = DateTime.UtcNow
            };
            await _dbContext.Client.Insertable(teacher).ExecuteCommandAsync();

            var dto = new PasswordChangeDto
            {
                OldPassword = "123456",
                NewPassword = "newteacherpass"
            };

            // Act
            var result = await _changePasswordHandler.Handle(new ChangePasswordCommand(dto, "T010", UserRole.Teacher), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            var updated = await _dbContext.Client.Queryable<Teacher>().FirstAsync(t => t.Id == "T010");
            Assert.NotNull(updated);
            Assert.True(BCrypt.Net.BCrypt.Verify("newteacherpass", updated!.Password));
        }

        /// <summary>
        /// 管理员修改密码使用正确旧密码应更新密码
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_AdminWithCorrectOldPassword_UpdatesPassword()
        {
            // Arrange
            var admin = new SystemUser
            {
                Username = "testadmin",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin,
                RealName = "测试管理员",
                CreateTime = DateTime.UtcNow
            };
            await _dbContext.Client.Insertable(admin).ExecuteCommandAsync();

            var dto = new PasswordChangeDto
            {
                OldPassword = "admin123",
                NewPassword = "newadminpass"
            };

            // Act
            var result = await _changePasswordHandler.Handle(new ChangePasswordCommand(dto, "testadmin", UserRole.Admin), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            var updated = await _dbContext.Client.Queryable<SystemUser>().FirstAsync(u => u.Username == "testadmin");
            Assert.NotNull(updated);
            Assert.True(BCrypt.Net.BCrypt.Verify("newadminpass", updated!.Password));
        }

        /// <summary>
        /// 软删除教师应将 IsDeleted 标记为 true
        /// </summary>
        [Fact]
        public async Task DeleteTeacherAsync_ExistingId_SetsIsDeletedTrue()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var teacher = new Teacher
            {
                Id = "T020",
                Name = "待删除教师",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Gender = "女",
                DepartmentId = 1,
                Role = TeacherRole.Teacher,
                CreateTime = DateTime.UtcNow
            };
            await _dbContext.Client.Insertable(teacher).ExecuteCommandAsync();

            // Act
            var result = await new Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers.TeacherHandlers(_dbContext, NullLogger<Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers.TeacherHandlers>.Instance)
                .Handle(new Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers.DeleteTeacherCommand("T020"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            var deleted = await _dbContext.Client.Queryable<Teacher>().FirstAsync(t => t.Id == "T020");
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
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
        /// 播种院系/专业/班级等关联数据，便于学生创建测试
        /// </summary>
        private async Task SeedReferenceDataAsync()
        {
            await _dbContext.Client.Insertable(new Department
            {
                Id = 1,
                Name = "计算机学院",
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();

            await _dbContext.Client.Insertable(new Major
            {
                Id = 1,
                Name = "软件工程",
                DepartmentId = 1,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();

            await _dbContext.Client.Insertable(new Class
            {
                Id = 1,
                Name = "软工2201",
                MajorId = 1,
                Grade = 2022,
                CounselorId = "T001",
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }
    }
}