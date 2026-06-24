using Larpx.PersonalTools.MyCollegeNew.Api.Features.Statistics;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Statistics
{
    /// <summary>
    /// StatisticsHandlers 单元测试，覆盖全局统计计数、学生维度统计、院系排名等场景
    /// </summary>
    public class StatisticsServiceTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly StatisticsHandlers _statisticsHandlers;

        /// <summary>
        /// 构造函数，初始化测试上下文与 StatisticsHandlers 实例
        /// </summary>
        public StatisticsServiceTests()
        {
            _dbContext = new TestDbContext();
            // 使用 Mock 创建 ICurrentUser，默认为管理员角色以绕过权限校验
            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(c => c.Role).Returns(UserRole.Admin);
            currentUser.Setup(c => c.UserId).Returns("admin");
            currentUser.Setup(c => c.IsAuthenticated).Returns(true);
            _statisticsHandlers = new StatisticsHandlers(_dbContext, currentUser.Object, NullLogger<StatisticsHandlers>.Instance);
        }

        /// <summary>
        /// 全局统计应返回正确的学生数、教师数与今日会话数
        /// </summary>
        [Fact]
        public async Task GetOverviewAsync_ReturnsCorrectCounts()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await SeedAttendanceDataAsync();

            // Act
            var result = await _statisticsHandlers.Handle(new GetOverviewQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data!.TotalStudents);
            Assert.Equal(2, result.Data.TotalTeachers);
            Assert.Equal(1, result.Data.TodaySessions);
            Assert.True(result.Data.OverallAttendanceRate >= 0 && result.Data.OverallAttendanceRate <= 100);
        }

        /// <summary>
        /// 学生个人统计应返回正确的出勤/迟到/缺勤/请假次数
        /// </summary>
        [Fact]
        public async Task GetStudentStatisticsAsync_ReturnsAttendanceCounts()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await SeedAttendanceDataAsync();

            // Act
            var result = await _statisticsHandlers.Handle(new GetStudentStatisticsQuery("S001"), CancellationToken.None);

            // Assert - S001 有 1 次 Present、1 次 Late、1 次 Absent
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal("S001", result.Data!.StudentId);
            Assert.Equal("李同学", result.Data.StudentName);
            Assert.Equal(3, result.Data.TotalSessions);
            Assert.Equal(1, result.Data.PresentCount);
            Assert.Equal(1, result.Data.LateCount);
            Assert.Equal(1, result.Data.AbsentCount);
            Assert.Equal(0, result.Data.LeaveCount);
            // 出勤率 = (Present + Late) / Total = 2/3 ≈ 66.67%
            Assert.True(result.Data.AttendanceRate > 66 && result.Data.AttendanceRate < 67);
        }

        /// <summary>
        /// 院系排名应按出勤率降序排列
        /// </summary>
        [Fact]
        public async Task GetDepartmentRankingAsync_ReturnsSortedByRate()
        {
            // Arrange
            await SeedReferenceDataAsync();
            await SeedAttendanceDataAsync();

            // Act
            var result = await _statisticsHandlers.Handle(new GetDepartmentRankingQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data!);
            // 验证按出勤率降序排列
            for (var i = 1; i < result.Data!.Count; i++)
            {
                Assert.True(result.Data[i - 1].AttendanceRate >= result.Data[i].AttendanceRate);
            }
            // 验证排名序号从 1 开始递增
            for (var i = 0; i < result.Data.Count; i++)
            {
                Assert.Equal(i + 1, result.Data[i].Rank);
            }
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
        /// 播种考勤会话与考勤记录数据，用于统计测试
        /// </summary>
        private async Task SeedAttendanceDataAsync()
        {
            var db = _dbContext.Client;

            // 创建今日会话
            var session = new AttendanceSession
            {
                CourseId = 1,
                ClassId = 1,
                TeacherId = "T001",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(30),
                Status = SessionStatus.Closed,
                QrToken = "test-token",
                CreateTime = DateTime.UtcNow
            };
            var sessionId = await db.Insertable(session).ExecuteReturnIdentityAsync();

            // S001: 1 次 Present、1 次 Late、1 次 Absent
            await db.Insertable(new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = "S001",
                StudentName = "李同学",
                Status = AttendanceStatus.Present,
                CheckInTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();

            await db.Insertable(new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = "S001",
                StudentName = "李同学",
                Status = AttendanceStatus.Late,
                CheckInTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();

            await db.Insertable(new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = "S001",
                StudentName = "李同学",
                Status = AttendanceStatus.Absent,
                CheckInTime = null,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();

            // S002: 1 次 Present
            await db.Insertable(new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = "S002",
                StudentName = "赵同学",
                Status = AttendanceStatus.Present,
                CheckInTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }
    }
}