using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Models.Leave;
using Campus.Attendance.Services.Leave;
using Campus.Attendance.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;
using Xunit;

namespace Campus.Attendance.Tests.Leave;

/// <summary>
/// LeaveService 单元测试，覆盖请假申请、审批通过/驳回、审批后考勤记录联动更新、待审批数量统计等场景
/// </summary>
public class LeaveServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly LeaveService _leaveService;

    /// <summary>
    /// 构造函数，初始化测试上下文与 LeaveService 实例
    /// </summary>
    public LeaveServiceTests()
    {
        _dbContext = new TestDbContext();
        _leaveService = new LeaveService(_dbContext, NullLogger<LeaveService>.Instance);
    }

    /// <summary>
    /// 提交合法请假申请应返回创建后的请假信息，状态为 Pending
    /// </summary>
    [Fact]
    public async Task CreateLeaveAsync_ValidDto_ReturnsCreatedLeave()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var dto = new LeaveCreateDto
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddDays(2),
            LeaveType = LeaveType.Sick,
            Reason = "感冒发烧"
        };

        // Act
        var result = await _leaveService.CreateLeaveAsync(dto, "S001");

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("S001", result.StudentId);
        Assert.Equal("李同学", result.StudentName);
        Assert.Equal("T002", result.CounselorId);
        Assert.Equal(LeaveStatus.Pending, result.Status);
        Assert.Equal(LeaveType.Sick, result.LeaveType);
    }

    /// <summary>
    /// 审批通过合法请假申请应将状态更新为 Approved
    /// </summary>
    [Fact]
    public async Task ApproveLeaveAsync_ValidLeave_UpdatesStatusToApproved()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var leaveId = await CreateLeaveAsync();
        var reviewDto = new LeaveReviewDto { ReviewRemark = "同意请假" };

        // Act
        var result = await _leaveService.ApproveLeaveAsync(leaveId, "T002", reviewDto);

        // Assert
        Assert.Equal(LeaveStatus.Approved, result.Status);
        Assert.Equal("同意请假", result.ReviewRemark);
        Assert.NotNull(result.ReviewTime);
    }

    /// <summary>
    /// 审批通过后应将请假时间段内该学生的考勤记录状态更新为 Leave
    /// </summary>
    [Fact]
    public async Task ApproveLeaveAsync_UpdatesAttendanceRecordsToLeave()
    {
        // Arrange
        await SeedReferenceDataAsync();

        // 创建考勤会话（开始时间在请假区间内）并写入一条缺勤记录
        var sessionStartTime = DateTime.UtcNow.AddDays(1);
        var sessionId = await CreateSessionWithRecordAsync(sessionStartTime, "S001", AttendanceStatus.Absent);

        // 提交请假申请，区间覆盖会话开始时间
        var leaveId = await CreateLeaveAsync(sessionStartTime.AddHours(-1), sessionStartTime.AddHours(1));

        // Act
        await _leaveService.ApproveLeaveAsync(leaveId, "T002", new LeaveReviewDto { ReviewRemark = "同意" });

        // Assert - 考勤记录应被联动更新为 Leave
        var record = await _dbContext.Client.Queryable<AttendanceRecord>()
            .Where(r => r.SessionId == sessionId && r.StudentId == "S001")
            .FirstAsync();
        Assert.NotNull(record);
        Assert.Equal(AttendanceStatus.Leave, record!.Status);
    }

    /// <summary>
    /// 辅导员待审批数量应返回正确的 Pending 状态记录数
    /// </summary>
    [Fact]
    public async Task GetPendingLeavesCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await SeedReferenceDataAsync();
        await CreateLeaveAsync();
        await CreateLeaveAsync();
        // 第三条请假审批通过，不计入待审批数
        var approvedId = await CreateLeaveAsync();
        await _leaveService.ApproveLeaveAsync(approvedId, "T002", new LeaveReviewDto());

        // Act
        var count = await _leaveService.GetPendingLeavesCountAsync("T002");

        // Assert
        Assert.Equal(2, count);
    }

    /// <summary>
    /// 驳回合法请假申请应将状态更新为 Rejected
    /// </summary>
    [Fact]
    public async Task RejectLeaveAsync_ValidLeave_UpdatesStatusToRejected()
    {
        // Arrange
        await SeedReferenceDataAsync();
        var leaveId = await CreateLeaveAsync();
        var reviewDto = new LeaveReviewDto { ReviewRemark = "事由不充分" };

        // Act
        var result = await _leaveService.RejectLeaveAsync(leaveId, "T002", reviewDto);

        // Assert
        Assert.Equal(LeaveStatus.Rejected, result.Status);
        Assert.Equal("事由不充分", result.ReviewRemark);
        Assert.NotNull(result.ReviewTime);
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
    /// 播种院系、专业、班级、教师、学生等关联数据
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
    }

    /// <summary>
    /// 创建请假申请并返回请假 Id，默认区间为当前时间起 2 天
    /// </summary>
    /// <param name="startTime">请假开始时间（可选）</param>
    /// <param name="endTime">请假结束时间（可选）</param>
    /// <returns>请假申请 Id</returns>
    private async Task<long> CreateLeaveAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        var dto = new LeaveCreateDto
        {
            StartTime = startTime ?? DateTime.UtcNow,
            EndTime = endTime ?? DateTime.UtcNow.AddDays(2),
            LeaveType = LeaveType.Personal,
            Reason = "家中有事"
        };
        var leave = await _leaveService.CreateLeaveAsync(dto, "S001");
        return leave.Id;
    }

    /// <summary>
    /// 创建考勤会话并写入一条指定状态的考勤记录
    /// </summary>
    /// <param name="sessionStartTime">会话开始时间</param>
    /// <param name="studentId">学生学号</param>
    /// <param name="status">考勤状态</param>
    /// <returns>会话 Id</returns>
    private async Task<long> CreateSessionWithRecordAsync(DateTime sessionStartTime, string studentId, AttendanceStatus status)
    {
        var db = _dbContext.Client;

        var session = new AttendanceSession
        {
            CourseId = 1,
            ClassId = 1,
            TeacherId = "T001",
            StartTime = sessionStartTime,
            EndTime = sessionStartTime.AddMinutes(30),
            Status = SessionStatus.Closed,
            QrToken = "test-token",
            CreateTime = DateTime.UtcNow
        };
        var sessionId = await db.Insertable(session).ExecuteReturnIdentityAsync();

        var record = new AttendanceRecord
        {
            SessionId = sessionId,
            StudentId = studentId,
            StudentName = "李同学",
            Status = status,
            CheckInTime = null,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(record).ExecuteCommandAsync();

        return sessionId;
    }
}
