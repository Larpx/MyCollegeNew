using System.Text;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Models.Users;
using Campus.Attendance.Services.Users;
using Campus.Attendance.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Campus.Attendance.Tests.Users;

/// <summary>
/// UserService 单元测试，使用 SQLite 内存数据库隔离测试
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly UserService _userService;

    /// <summary>
    /// 构造函数，初始化测试上下文与 UserService 实例
    /// </summary>
    public UserServiceTests()
    {
        _dbContext = new TestDbContext();
        _userService = new UserService(_dbContext, NullLogger<UserService>.Instance);
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
        var result = await _userService.CreateStudentAsync(dto);

        // Assert
        Assert.Equal("20220102", result.Id);
        Assert.Equal("张三", result.Name);
    }

    /// <summary>
    /// 创建学生使用重复学号应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task CreateStudentAsync_DuplicateId_ThrowsBusinessException()
    {
        // Arrange
        await SeedReferenceDataAsync();
        await _userService.CreateStudentAsync(new StudentCreateDto
        {
            Id = "20220103",
            Name = "李四",
            Password = "123456",
            Gender = "男",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022
        });

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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _userService.CreateStudentAsync(dto));
        Assert.Contains("已存在", ex.Message);
    }

    /// <summary>
    /// 删除已存在学生应将 IsDeleted 标记为 true
    /// </summary>
    [Fact]
    public async Task DeleteStudentAsync_ExistingId_SetsIsDeletedTrue()
    {
        // Arrange
        await SeedReferenceDataAsync();
        await _userService.CreateStudentAsync(new StudentCreateDto
        {
            Id = "20220104",
            Name = "王五",
            Password = "123456",
            Gender = "男",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022
        });

        // Act
        await _userService.DeleteStudentAsync("20220104");

        // Assert
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
        var result = await _userService.BatchImportStudentsAsync(stream);

        // Assert
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    /// <summary>
    /// 修改密码使用正确旧密码应更新密码
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_CorrectOldPassword_UpdatesPassword()
    {
        // Arrange
        await SeedReferenceDataAsync();
        await _userService.CreateStudentAsync(new StudentCreateDto
        {
            Id = "20220107",
            Name = "孙八",
            Password = "123456",
            Gender = "男",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022
        });

        var dto = new PasswordChangeDto
        {
            OldPassword = "123456",
            NewPassword = "newpass123"
        };

        // Act
        await _userService.ChangePasswordAsync("20220107", UserRole.Student, dto);

        // Assert
        var student = await _dbContext.Client.Queryable<Student>().FirstAsync(s => s.Id == "20220107");
        Assert.NotNull(student);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpass123", student!.Password));
    }

    /// <summary>
    /// 修改密码使用错误旧密码应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ThrowsBusinessException()
    {
        // Arrange
        await SeedReferenceDataAsync();
        await _userService.CreateStudentAsync(new StudentCreateDto
        {
            Id = "20220108",
            Name = "周九",
            Password = "123456",
            Gender = "男",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022
        });

        var dto = new PasswordChangeDto
        {
            OldPassword = "wrong",
            NewPassword = "newpass123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _userService.ChangePasswordAsync("20220108", UserRole.Student, dto));
        Assert.Contains("旧密码", ex.Message);
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
