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
/// ScheduleService 单元测试，使用 SQLite 内存数据库隔离测试
/// </summary>
public class ScheduleServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly ScheduleService _scheduleService;

    /// <summary>
    /// 构造函数，初始化测试上下文与 ScheduleService 实例
    /// </summary>
    public ScheduleServiceTests()
    {
        _dbContext = new TestDbContext();
        _scheduleService = new ScheduleService(_dbContext, NullLogger<ScheduleService>.Instance);
    }

    /// <summary>
    /// 创建课表使用合法 DTO 应返回创建后的课表信息
    /// </summary>
    [Fact]
    public async Task CreateScheduleAsync_ValidDto_ReturnsCreatedSchedule()
    {
        // Arrange
        var classId = await SeedReferenceDataAsync();
        var teacherId = "T001";
        await SeedTeacherAsync(teacherId, "张老师");
        var courseId = await SeedCourseAsync("高等数学", teacherId);
        var dto = new ScheduleCreateDto
        {
            CourseId = courseId,
            ClassId = classId,
            TeacherId = teacherId,
            DayOfWeek = 1,
            StartSection = 1,
            EndSection = 2,
            StartWeek = 1,
            EndWeek = 16,
            Classroom = "A101"
        };

        // Act
        var result = await _scheduleService.CreateScheduleAsync(dto);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal(courseId, result.CourseId);
        Assert.Equal("高等数学", result.CourseName);
        Assert.Equal(classId, result.ClassId);
        Assert.Equal("软工2201", result.ClassName);
        Assert.Equal(teacherId, result.TeacherId);
        Assert.Equal("张老师", result.TeacherName);
        Assert.Equal(1, result.DayOfWeek);
        Assert.Equal("A101", result.Classroom);
    }

    /// <summary>
    /// 创建课表使用起始节次大于结束节次应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task CreateScheduleAsync_StartSectionGreaterThanEnd_ThrowsBusinessException()
    {
        // Arrange
        var classId = await SeedReferenceDataAsync();
        await SeedTeacherAsync("T001", "张老师");
        var courseId = await SeedCourseAsync("高等数学", "T001");
        var dto = new ScheduleCreateDto
        {
            CourseId = courseId,
            ClassId = classId,
            TeacherId = "T001",
            DayOfWeek = 1,
            StartSection = 3,
            EndSection = 2,
            StartWeek = 1,
            EndWeek = 16,
            Classroom = "A101"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _scheduleService.CreateScheduleAsync(dto));
        Assert.Contains("节次", ex.Message);
    }

    /// <summary>
    /// 按教师查询某周课表应返回按星期分组的周课表
    /// </summary>
    [Fact]
    public async Task GetScheduleByTeacherAsync_ReturnsWeeklySchedule()
    {
        // Arrange
        var classId = await SeedReferenceDataAsync();
        await SeedTeacherAsync("T001", "张老师");
        var courseId = await SeedCourseAsync("高等数学", "T001");

        // 创建两条课表：周一第 1-2 节、周三第 3-4 节，均在第 1-16 周
        await _scheduleService.CreateScheduleAsync(new ScheduleCreateDto
        {
            CourseId = courseId, ClassId = classId, TeacherId = "T001",
            DayOfWeek = 1, StartSection = 1, EndSection = 2,
            StartWeek = 1, EndWeek = 16, Classroom = "A101"
        });
        await _scheduleService.CreateScheduleAsync(new ScheduleCreateDto
        {
            CourseId = courseId, ClassId = classId, TeacherId = "T001",
            DayOfWeek = 3, StartSection = 3, EndSection = 4,
            StartWeek = 1, EndWeek = 16, Classroom = "A102"
        });

        // Act
        var result = await _scheduleService.GetScheduleByTeacherAsync("T001", week: 5);

        // Assert
        Assert.Equal(5, result.Week);
        Assert.Equal(2, result.Days.Count);
        Assert.True(result.Days.ContainsKey(1));
        Assert.True(result.Days.ContainsKey(3));
        Assert.Single(result.Days[1]);
        Assert.Single(result.Days[3]);
    }

    /// <summary>
    /// 按教师查询不在范围内的周次应返回空周课表
    /// </summary>
    [Fact]
    public async Task GetScheduleByTeacherAsync_OutOfRangeWeek_ReturnsEmpty()
    {
        // Arrange
        var classId = await SeedReferenceDataAsync();
        await SeedTeacherAsync("T001", "张老师");
        var courseId = await SeedCourseAsync("高等数学", "T001");
        await _scheduleService.CreateScheduleAsync(new ScheduleCreateDto
        {
            CourseId = courseId, ClassId = classId, TeacherId = "T001",
            DayOfWeek = 1, StartSection = 1, EndSection = 2,
            StartWeek = 1, EndWeek = 16, Classroom = "A101"
        });

        // Act：查询第 20 周（超出 1-16 周范围）
        var result = await _scheduleService.GetScheduleByTeacherAsync("T001", week: 20);

        // Assert
        Assert.Equal(20, result.Week);
        Assert.Empty(result.Days);
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
    /// 播种院系/专业/班级等关联数据，返回班级 Id
    /// </summary>
    private async Task<long> SeedReferenceDataAsync()
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

        var classId = await _dbContext.Client.Insertable(new Class
        {
            Name = "软工2201",
            MajorId = 1,
            Grade = 2022,
            CounselorId = "T002",
            CreateTime = DateTime.UtcNow
        }).ExecuteReturnIdentityAsync();

        return classId;
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

    /// <summary>
    /// 播种课程并返回课程 Id
    /// </summary>
    private async Task<long> SeedCourseAsync(string name, string teacherId)
    {
        var course = new Course
        {
            Name = name,
            TeacherId = teacherId,
            Credit = 4m,
            CreateTime = DateTime.UtcNow
        };
        return await _dbContext.Client.Insertable(course).ExecuteReturnIdentityAsync();
    }
}
