using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Models.Courses;
using Campus.Attendance.Services.Courses;
using Campus.Attendance.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Campus.Attendance.Tests.Courses;

/// <summary>
/// CourseService 单元测试，使用 SQLite 内存数据库隔离测试
/// </summary>
public class CourseServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly CourseService _courseService;

    /// <summary>
    /// 构造函数，初始化测试上下文与 CourseService 实例
    /// </summary>
    public CourseServiceTests()
    {
        _dbContext = new TestDbContext();
        _courseService = new CourseService(_dbContext, NullLogger<CourseService>.Instance);
    }

    /// <summary>
    /// 创建课程使用合法 DTO 应返回创建后的课程信息
    /// </summary>
    [Fact]
    public async Task CreateCourseAsync_ValidDto_ReturnsCreatedCourse()
    {
        // Arrange
        await SeedTeacherAsync("T001", "张老师");
        var dto = new CourseCreateDto
        {
            Name = "高等数学",
            TeacherId = "T001",
            Credit = 4m,
            Remark = "公共基础课"
        };

        // Act
        var result = await _courseService.CreateCourseAsync(dto);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("高等数学", result.Name);
        Assert.Equal("T001", result.TeacherId);
        Assert.Equal("张老师", result.TeacherName);
        Assert.Equal(4m, result.Credit);
    }

    /// <summary>
    /// 创建课程使用不存在的教师工号应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task CreateCourseAsync_NonExistentTeacher_ThrowsBusinessException()
    {
        // Arrange
        var dto = new CourseCreateDto
        {
            Name = "离散数学",
            TeacherId = "T999",
            Credit = 3m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _courseService.CreateCourseAsync(dto));
        Assert.Contains("教师", ex.Message);
    }

    /// <summary>
    /// 按教师查询课程应返回该教师的所有课程
    /// </summary>
    [Fact]
    public async Task GetCoursesByTeacherAsync_ReturnsTeacherCourses()
    {
        // Arrange
        await SeedTeacherAsync("T001", "张老师");
        await _courseService.CreateCourseAsync(new CourseCreateDto { Name = "高等数学", TeacherId = "T001", Credit = 4m });
        await _courseService.CreateCourseAsync(new CourseCreateDto { Name = "线性代数", TeacherId = "T001", Credit = 3m });

        // Act
        var result = await _courseService.GetCoursesByTeacherAsync("T001");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "高等数学");
        Assert.Contains(result, c => c.Name == "线性代数");
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
    /// 播种教师账号
    /// </summary>
    private async Task SeedTeacherAsync(string id, string name)
    {
        var teacher = new Teacher
        {
            Id = id,
            Name = name,
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "男",
            DepartmentId = 1,
            MajorId = null,
            Role = TeacherRole.Teacher,
            CreateTime = DateTime.UtcNow
        };
        await _dbContext.Client.Insertable(teacher).ExecuteCommandAsync();
    }
}
