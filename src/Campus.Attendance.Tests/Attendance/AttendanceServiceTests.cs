using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Campus.Attendance.Core.Constants;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Attendance;
using Campus.Attendance.Services.Attendance;
using Campus.Attendance.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using Xunit;

namespace Campus.Attendance.Tests.Attendance;

/// <summary>
/// AttendanceService 单元测试，覆盖签到状态判定、重复签到、过期令牌、一键点名、随机点名等场景
/// </summary>
public class AttendanceServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly AttendanceService _attendanceService;
    private readonly IOptions<JwtConfig> _jwtConfig;

    /// <summary>
    /// 构造函数，初始化测试上下文与 AttendanceService 实例
    /// </summary>
    public AttendanceServiceTests()
    {
        _dbContext = new TestDbContext();
        _jwtConfig = TestJwtConfigFactory.Create();
        _attendanceService = new AttendanceService(_dbContext, _jwtConfig, NullLogger<AttendanceService>.Instance);
    }

    /// <summary>
    /// 签到时间在会话开始后 5 分钟内应返回 Present
    /// </summary>
    [Fact]
    public async Task CheckInAsync_Within5Minutes_ReturnsPresent()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var sessionId = await CreateSessionAsync(DateTime.UtcNow);
        var token = await GenerateValidQrTokenAsync(sessionId);

        // Act
        var result = await _attendanceService.CheckInAsync(sessionId, token, "S001");

        // Assert
        Assert.Equal(AttendanceStatus.Present, result.Status);
        Assert.Contains("签到成功", result.Message);
    }

    /// <summary>
    /// 签到时间在会话开始后 5-15 分钟内应返回 Late
    /// </summary>
    [Fact]
    public async Task CheckInAsync_Between5And15Minutes_ReturnsLate()
    {
        // Arrange
        await SeedReferenceDataAsync();
        // 会话开始时间为 10 分钟前，处于迟到窗口
        var sessionId = await CreateSessionAsync(DateTime.UtcNow.AddMinutes(-10));
        var token = await GenerateValidQrTokenAsync(sessionId);

        // Act
        var result = await _attendanceService.CheckInAsync(sessionId, token, "S001");

        // Assert
        Assert.Equal(AttendanceStatus.Late, result.Status);
        Assert.Contains("迟到", result.Message);
    }

    /// <summary>
    /// 使用过期令牌签到应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task CheckInAsync_ExpiredToken_ThrowsBusinessException()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var sessionId = await CreateSessionAsync(DateTime.UtcNow);
        // 生成已过期的令牌（签发时间为 2 分钟前，已超过 30 秒有效期）
        var expiredToken = GenerateCustomToken(sessionId, DateTime.UtcNow.AddMinutes(-2));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _attendanceService.CheckInAsync(sessionId, expiredToken, "S001"));
        Assert.Contains("过期", ex.Message);
    }

    /// <summary>
    /// 重复签到应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task CheckInAsync_DuplicateCheckIn_ThrowsBusinessException()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var sessionId = await CreateSessionAsync(DateTime.UtcNow);
        var token = await GenerateValidQrTokenAsync(sessionId);

        // 第一次签到成功
        await _attendanceService.CheckInAsync(sessionId, token, "S001");

        // Act & Assert - 第二次签到应抛出异常
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _attendanceService.CheckInAsync(sessionId, token, "S001"));
        Assert.Contains("重复", ex.Message);
    }

    /// <summary>
    /// 一键点名应将所有未签到学生标记为 Present
    /// </summary>
    [Fact]
    public async Task RollCallAllPresentAsync_MarksAllUnCheckedStudentsAsPresent()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var sessionId = await CreateSessionAsync(DateTime.UtcNow);

        // Act
        var count = await _attendanceService.RollCallAllPresentAsync(sessionId, "T001");

        // Assert - 班级中有 2 名学生（S001、S002），均未签到
        Assert.Equal(2, count);

        var records = await _dbContext.Client.Queryable<AttendanceRecord>()
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.All(records, r => Assert.Equal(AttendanceStatus.Present, r.Status));
    }

    /// <summary>
    /// 随机点名应返回指定班级的学生
    /// </summary>
    [Fact]
    public async Task RandomPickAsync_ReturnsStudentFromSpecifiedClass()
    {
        // Arrange
        await SeedReferenceDataAsync();

        // Act
        var result = await _attendanceService.RandomPickAsync(1);

        // Assert
        Assert.Equal(1, result.ClassId);
        Assert.False(string.IsNullOrEmpty(result.StudentId));
        Assert.False(string.IsNullOrEmpty(result.StudentName));
        Assert.False(string.IsNullOrEmpty(result.ClassName));

        // 验证返回的学生确实属于该班级
        var student = await _dbContext.Client.Queryable<Student>()
            .FirstAsync(s => s.Id == result.StudentId);
        Assert.NotNull(student);
        Assert.Equal(1, student!.ClassId);
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
    /// 播种院系、专业、班级、教师、课程、学生等关联数据
    /// </summary>
    private async Task SeedReferenceDataAsync()
    {
        var db = _dbContext.Client;

        await db.Insertable(new Department { Id = 1, Name = "计算机学院", CreateTime = DateTime.UtcNow }).ExecuteCommandAsync();
        await db.Insertable(new Major { Id = 1, Name = "软件工程", DepartmentId = 1, CreateTime = DateTime.UtcNow }).ExecuteCommandAsync();
        await db.Insertable(new Class { Id = 1, Name = "软工2201", MajorId = 1, Grade = 2022, CounselorId = "T002", CreateTime = DateTime.UtcNow }).ExecuteCommandAsync();

        await db.Insertable(new Teacher
        {
            Id = "T001",
            Name = "张老师",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "男",
            DepartmentId = 1,
            Role = TeacherRole.Teacher,
            CreateTime = DateTime.UtcNow
        }).ExecuteCommandAsync();

        await db.Insertable(new Teacher
        {
            Id = "T002",
            Name = "王辅导员",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "女",
            DepartmentId = 1,
            Role = TeacherRole.Counselor,
            CreateTime = DateTime.UtcNow
        }).ExecuteCommandAsync();

        await db.Insertable(new Course { Id = 1, Name = "数据结构", TeacherId = "T001", Credit = 3, CreateTime = DateTime.UtcNow }).ExecuteCommandAsync();

        await db.Insertable(new Student
        {
            Id = "S001",
            Name = "李同学",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "男",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022,
            Status = 0,
            CreateTime = DateTime.UtcNow
        }).ExecuteCommandAsync();

        await db.Insertable(new Student
        {
            Id = "S002",
            Name = "赵同学",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "女",
            DepartmentId = 1,
            MajorId = 1,
            ClassId = 1,
            Grade = 2022,
            Status = 0,
            CreateTime = DateTime.UtcNow
        }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 创建考勤会话并返回会话 Id
    /// </summary>
    /// <param name="startTime">会话开始时间</param>
    /// <returns>会话 Id</returns>
    private async Task<long> CreateSessionAsync(DateTime startTime)
    {
        var dto = new SessionCreateDto
        {
            CourseId = 1,
            ClassId = 1,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(30)
        };
        var session = await _attendanceService.CreateSessionAsync(dto, "T001");
        return session.Id;
    }

    /// <summary>
    /// 通过 AttendanceService 生成有效的二维码令牌
    /// </summary>
    /// <param name="sessionId">会话 Id</param>
    /// <returns>有效的 JWT 令牌</returns>
    private async Task<string> GenerateValidQrTokenAsync(long sessionId)
    {
        var result = await _attendanceService.GenerateQrCodeAsync(sessionId, "T001");
        return result.Token;
    }

    /// <summary>
    /// 生成自定义签发时间的二维码令牌（用于测试过期场景）
    /// </summary>
    /// <param name="sessionId">会话 Id</param>
    /// <param name="issuedAt">签发时间</param>
    /// <returns>JWT 令牌</returns>
    private string GenerateCustomToken(long sessionId, DateTime issuedAt)
    {
        var config = _jwtConfig.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(AttendanceConstants.ClaimSessionId, sessionId.ToString()),
            new Claim(AttendanceConstants.ClaimPurpose, AttendanceConstants.PurposeCheckIn)
        };

        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims,
            notBefore: issuedAt.AddSeconds(-1),
            expires: issuedAt.AddSeconds(AttendanceConstants.QrTokenExpireSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
