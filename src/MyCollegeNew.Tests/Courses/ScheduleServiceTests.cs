using Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Courses
{
    /// <summary>
    /// CourseHandlers 课表部分单元测试，使用 SQLite 内存数据库隔离测试
    /// </summary>
    public class ScheduleServiceTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly CourseHandlers _courseHandlers;

        /// <summary>
        /// 构造函数，初始化测试上下文与 CourseHandlers 实例
        /// </summary>
        public ScheduleServiceTests()
        {
            _dbContext = new TestDbContext();
            _courseHandlers = new CourseHandlers(_dbContext, NullLogger<CourseHandlers>.Instance);
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
            var result = await _courseHandlers.Handle(new CreateScheduleCommand(dto), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.Id > 0);
            Assert.Equal(courseId, result.Data.CourseId);
            Assert.Equal("高等数学", result.Data.CourseName);
            Assert.Equal(classId, result.Data.ClassId);
            Assert.Equal("软工2201", result.Data.ClassName);
            Assert.Equal(teacherId, result.Data.TeacherId);
            Assert.Equal("张老师", result.Data.TeacherName);
            Assert.Equal(1, result.Data.DayOfWeek);
            Assert.Equal("A101", result.Data.Classroom);
        }

        /// <summary>
        /// 创建课表使用起始节次大于结束节次应返回失败响应
        /// </summary>
        [Fact]
        public async Task CreateScheduleAsync_StartSectionGreaterThanEnd_ReturnsFail()
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

            // Act
            var result = await _courseHandlers.Handle(new CreateScheduleCommand(dto), CancellationToken.None);

            // Assert
            Assert.Equal(400, result.Code);
            Assert.Contains("节次", result.Message);
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
            await _courseHandlers.Handle(new CreateScheduleCommand(new ScheduleCreateDto
            {
                CourseId = courseId,
                ClassId = classId,
                TeacherId = "T001",
                DayOfWeek = 1,
                StartSection = 1,
                EndSection = 2,
                StartWeek = 1,
                EndWeek = 16,
                Classroom = "A101"
            }), CancellationToken.None);
            await _courseHandlers.Handle(new CreateScheduleCommand(new ScheduleCreateDto
            {
                CourseId = courseId,
                ClassId = classId,
                TeacherId = "T001",
                DayOfWeek = 3,
                StartSection = 3,
                EndSection = 4,
                StartWeek = 1,
                EndWeek = 16,
                Classroom = "A102"
            }), CancellationToken.None);

            // Act
            var result = await _courseHandlers.Handle(new GetScheduleByTeacherQuery("T001", Week: 5), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data!.Week);
            Assert.Equal(2, result.Data.Days.Count);
            Assert.True(result.Data.Days.ContainsKey(1));
            Assert.True(result.Data.Days.ContainsKey(3));
            Assert.Single(result.Data.Days[1]);
            Assert.Single(result.Data.Days[3]);
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
            await _courseHandlers.Handle(new CreateScheduleCommand(new ScheduleCreateDto
            {
                CourseId = courseId,
                ClassId = classId,
                TeacherId = "T001",
                DayOfWeek = 1,
                StartSection = 1,
                EndSection = 2,
                StartWeek = 1,
                EndWeek = 16,
                Classroom = "A101"
            }), CancellationToken.None);

            // Act：查询第 20 周（超出 1-16 周范围）
            var result = await _courseHandlers.Handle(new GetScheduleByTeacherQuery("T001", Week: 20), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(20, result.Data!.Week);
            Assert.Empty(result.Data.Days);
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
}