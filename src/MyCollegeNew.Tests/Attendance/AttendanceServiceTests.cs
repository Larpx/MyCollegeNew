using Larpx.PersonalTools.MyCollegeNew.Api.Features.Attendance;
using Larpx.PersonalTools.MyCollegeNew.Shared.Constants;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Attendance;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Attendance
{
    /// <summary>
    /// AttendanceHandlers 单元测试，覆盖签到状态判定、重复签到、过期令牌、一键点名、随机点名等场景
    /// </summary>
    public class AttendanceHandlersTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly AttendanceHandlers _attendanceHandlers;
        private readonly IOptions<JwtConfig> _jwtConfig;

        /// <summary>
        /// 构造函数，初始化测试上下文与 AttendanceHandlers 实例
        /// </summary>
        public AttendanceHandlersTests()
        {
            _dbContext = new TestDbContext();
            _jwtConfig = TestJwtConfigFactory.Create();
            _attendanceHandlers = new AttendanceHandlers(_dbContext, _jwtConfig, NullLogger<AttendanceHandlers>.Instance);
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
            var result = await _attendanceHandlers.Handle(new CheckInCommand(sessionId, token, "S001"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(AttendanceStatus.Present, result.Data!.Status);
            Assert.Contains("签到成功", result.Data.Message);
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
            var result = await _attendanceHandlers.Handle(new CheckInCommand(sessionId, token, "S001"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(AttendanceStatus.Late, result.Data!.Status);
            Assert.Contains("迟到", result.Data.Message);
        }

        /// <summary>
        /// 使用过期令牌签到应返回失败响应
        /// </summary>
        [Fact]
        public async Task CheckInAsync_ExpiredToken_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);
            // 生成已过期的令牌（签发时间为 2 分钟前，已超过 30 秒有效期）
            var expiredToken = GenerateCustomToken(sessionId, DateTime.UtcNow.AddMinutes(-2));

            // Act
            var result = await _attendanceHandlers.Handle(new CheckInCommand(sessionId, expiredToken, "S001"), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("过期", result.Message);
        }

        /// <summary>
        /// 重复签到应返回失败响应
        /// </summary>
        [Fact]
        public async Task CheckInAsync_DuplicateCheckIn_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);
            var token = await GenerateValidQrTokenAsync(sessionId);

            // 第一次签到成功
            await _attendanceHandlers.Handle(new CheckInCommand(sessionId, token, "S001"), CancellationToken.None);

            // Act - 第二次签到应返回失败
            var result = await _attendanceHandlers.Handle(new CheckInCommand(sessionId, token, "S001"), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("重复", result.Message);
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
            var result = await _attendanceHandlers.Handle(new RollCallCommand(sessionId, "T001"), CancellationToken.None);

            // Assert - 班级中有 2 名学生（S001、S002），均未签到
            Assert.Equal(200, result.Code);
            Assert.Equal(2, result.Data);

            var records = await _dbContext.Client.Queryable<AttendanceRecord>()
                .Where(r => r.SessionId == sessionId)
                .ToListAsync();
            Assert.Equal(2, records.Count);
            Assert.True(records.All(r => r.Status == AttendanceStatus.Present));
        }

        /// <summary>
        /// 随机点名应仅从已签到（Present）学生中抽取
        /// </summary>
        [Fact]
        public async Task RandomPickAsync_ReturnsStudentFromPresentStudents()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);
            // 一键点名将班级学生标记为 Present，作为随机提问候选
            await _attendanceHandlers.Handle(new RollCallCommand(sessionId, "T001"), CancellationToken.None);

            // Act
            var result = await _attendanceHandlers.Handle(new RandomPickQuery(1, sessionId), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data!.ClassId);
            Assert.False(string.IsNullOrEmpty(result.Data.StudentId));
            Assert.False(string.IsNullOrEmpty(result.Data.StudentName));
            Assert.False(string.IsNullOrEmpty(result.Data.ClassName));

            // 验证抽中的学生确实为已签到（Present）状态
            var record = await _dbContext.Client.Queryable<AttendanceRecord>()
                .FirstAsync(r => r.SessionId == sessionId && r.StudentId == result.Data.StudentId);
            Assert.NotNull(record);
            Assert.Equal(AttendanceStatus.Present, record!.Status);
        }

        /// <summary>
        /// 会话下无已签到学生时随机点名应返回友好提示
        /// </summary>
        [Fact]
        public async Task RandomPickAsync_NoPresentStudents_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);
            // 不执行一键点名，会话下无任何已签到学生

            // Act
            var result = await _attendanceHandlers.Handle(new RandomPickQuery(1, sessionId), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("已签到", result.Message);
        }

        /// <summary>
        /// 标记随机点名结果应返回成功响应
        /// </summary>
        [Fact]
        public async Task MarkRandomPickResultAsync_AnsweredTrue_ReturnsSuccess()
        {
            // Arrange
            await SeedReferenceDataAsync();

            // Act
            var result = await _attendanceHandlers.Handle(new MarkRandomPickCommand(1, "S001", Answered: true), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
        }

        /// <summary>
        /// 标记随机点名结果 - 未回答也应返回成功响应
        /// </summary>
        [Fact]
        public async Task MarkRandomPickResultAsync_AnsweredFalse_ReturnsSuccess()
        {
            // Arrange
            await SeedReferenceDataAsync();

            // Act
            var result = await _attendanceHandlers.Handle(new MarkRandomPickCommand(1, "S001", Answered: false), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
        }

        /// <summary>
        /// 签到时间超过 15 分钟应返回 Absent
        /// </summary>
        [Fact]
        public async Task CheckInAsync_After15Minutes_ReturnsAbsent()
        {
            // Arrange
            await SeedReferenceDataAsync();
            // 会话开始时间为 20 分钟前，超过迟到窗口
            var sessionId = await CreateSessionAsync(DateTime.UtcNow.AddMinutes(-20));
            var token = await GenerateValidQrTokenAsync(sessionId);

            // Act
            var result = await _attendanceHandlers.Handle(new CheckInCommand(sessionId, token, "S001"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(AttendanceStatus.Absent, result.Data!.Status);
            Assert.Contains("缺勤", result.Data.Message);
        }

        /// <summary>
        /// 关闭会话应为未签到学生创建缺勤记录
        /// </summary>
        [Fact]
        public async Task CloseSessionAsync_CreatesAbsentRecordsForUncheckedStudents()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);

            // Act
            var result = await _attendanceHandlers.Handle(new CloseSessionCommand(sessionId, "T001"), CancellationToken.None);

            // Assert - 班级中有 2 名学生，均未签到，应生成 2 条缺勤记录
            Assert.Equal(200, result.Code);

            var records = await _dbContext.Client.Queryable<AttendanceRecord>()
                .Where(r => r.SessionId == sessionId)
                .ToListAsync();
            Assert.Equal(2, records.Count);
            Assert.True(records.All(r => r.Status == AttendanceStatus.Absent));

            // 验证会话状态已关闭
            var session = await _dbContext.Client.Queryable<AttendanceSession>()
                .FirstAsync(s => s.Id == sessionId);
            Assert.Equal(SessionStatus.Closed, session.Status);
        }

        /// <summary>
        /// 教师手动补签应创建新的考勤记录
        /// </summary>
        [Fact]
        public async Task ManualCheckInAsync_NewRecord_CreatesAttendanceRecord()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);

            // Act
            var result = await _attendanceHandlers.Handle(
                new ManualCheckInCommand(sessionId, "S001", AttendanceStatus.Present, "T001"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal("S001", result.Data!.StudentId);
            Assert.Equal(AttendanceStatus.Present, result.Data.Status);
            Assert.Contains("手动补签", result.Data.Remark);
        }

        /// <summary>
        /// 不存在的班级随机点名应返回失败响应
        /// </summary>
        [Fact]
        public async Task RandomPickAsync_NonExistentClass_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();

            // Act - 提供会话 Id 但班级不存在，命中班级校验分支
            var result = await _attendanceHandlers.Handle(new RandomPickQuery(999, 1), CancellationToken.None);

            // Assert
            Assert.Equal(404, result.Code);
            Assert.Contains("不存在", result.Message);
        }

        /// <summary>
        /// 缺少会话参数时随机点名应返回失败响应
        /// </summary>
        [Fact]
        public async Task RandomPickAsync_NoSession_ReturnsFail()
        {
            // Arrange
            await SeedReferenceDataAsync();

            // Act - 未提供会话 Id
            var result = await _attendanceHandlers.Handle(new RandomPickQuery(1, null), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("会话", result.Message);
        }

        /// <summary>
        /// 查询会话签到记录时，命中已批准请假的学生应自动置为请假状态并携带请假信息
        /// </summary>
        [Fact]
        public async Task GetSessionRecordsAsync_WithApprovedLeave_SetsLeaveStatus()
        {
            // Arrange
            await SeedReferenceDataAsync();
            var sessionId = await CreateSessionAsync(DateTime.UtcNow);

            // 学生 S001 已签到（Present），命中请假后应被叠加为 Leave
            var token = await GenerateValidQrTokenAsync(sessionId);
            await _attendanceHandlers.Handle(new CheckInCommand(sessionId, token, "S001"), CancellationToken.None);

            // 插入一条覆盖该会话时间范围的已批准请假申请
            var session = await _dbContext.Client.Queryable<AttendanceSession>().FirstAsync(s => s.Id == sessionId);
            await _dbContext.Client.Insertable(new LeaveRequest
            {
                StudentId = "S001",
                CounselorId = "T002",
                StartTime = session.StartTime.AddHours(-1),
                EndTime = session.EndTime.AddHours(1),
                LeaveType = LeaveType.Sick,
                Reason = "感冒发烧",
                Status = LeaveStatus.Approved,
                ReviewRemark = "同意请假",
                ReviewTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();

            // Act
            var result = await _attendanceHandlers.Handle(new GetSessionRecordsQuery(sessionId), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            var s001 = result.Data!.FirstOrDefault(r => r.StudentId == "S001");
            Assert.NotNull(s001);
            Assert.Equal(AttendanceStatus.Leave, s001!.Status);
            Assert.Equal("感冒发烧", s001.LeaveReason);
            Assert.Equal("同意请假", s001.LeaveRemark);
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
            var result = await _attendanceHandlers.Handle(new CreateSessionCommand(dto, "T001"), CancellationToken.None);
            return result.Data!.Id;
        }

        /// <summary>
        /// 通过 AttendanceHandlers 生成有效的二维码令牌
        /// </summary>
        /// <param name="sessionId">会话 Id</param>
        /// <returns>有效的 JWT 令牌</returns>
        private async Task<string> GenerateValidQrTokenAsync(long sessionId)
        {
            var result = await _attendanceHandlers.Handle(new GenerateQrCodeCommand(sessionId, "T001"), CancellationToken.None);
            return result.Data!.Token;
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
}