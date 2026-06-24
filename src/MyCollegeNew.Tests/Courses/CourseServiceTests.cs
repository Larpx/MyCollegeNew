using Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Courses
{
    /// <summary>
    /// CourseHandlers 单元测试，使用 SQLite 内存数据库隔离测试
    /// </summary>
    public class CourseServiceTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly CourseHandlers _courseHandlers;

        /// <summary>
        /// 构造函数，初始化测试上下文与 CourseHandlers 实例
        /// </summary>
        public CourseServiceTests()
        {
            _dbContext = new TestDbContext();
            _courseHandlers = new CourseHandlers(_dbContext, NullLogger<CourseHandlers>.Instance);
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
            var result = await _courseHandlers.Handle(new CreateCourseCommand(dto), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.Id > 0);
            Assert.Equal("高等数学", result.Data.Name);
            Assert.Equal("T001", result.Data.TeacherId);
            Assert.Equal("张老师", result.Data.TeacherName);
            Assert.Equal(4m, result.Data.Credit);
        }

        /// <summary>
        /// 创建课程使用不存在的教师工号应返回失败响应
        /// </summary>
        [Fact]
        public async Task CreateCourseAsync_NonExistentTeacher_ReturnsFail()
        {
            // Arrange
            var dto = new CourseCreateDto
            {
                Name = "离散数学",
                TeacherId = "T999",
                Credit = 3m
            };

            // Act
            var result = await _courseHandlers.Handle(new CreateCourseCommand(dto), CancellationToken.None);

            // Assert
            Assert.Equal(404, result.Code);
            Assert.Contains("教师", result.Message);
        }

        /// <summary>
        /// 按教师查询课程应返回该教师的所有课程
        /// </summary>
        [Fact]
        public async Task GetCoursesByTeacherAsync_ReturnsTeacherCourses()
        {
            // Arrange
            await SeedTeacherAsync("T001", "张老师");
            await _courseHandlers.Handle(new CreateCourseCommand(new CourseCreateDto { Name = "高等数学", TeacherId = "T001", Credit = 4m }), CancellationToken.None);
            await _courseHandlers.Handle(new CreateCourseCommand(new CourseCreateDto { Name = "线性代数", TeacherId = "T001", Credit = 3m }), CancellationToken.None);

            // Act
            var result = await _courseHandlers.Handle(new GetCoursesByTeacherQuery("T001"), CancellationToken.None);

            // Assert
            Assert.Equal(200, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data!.Count);
            Assert.Contains(result.Data, c => c.Name == "高等数学");
            Assert.Contains(result.Data, c => c.Name == "线性代数");
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
}