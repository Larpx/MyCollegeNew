using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Scheduling;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Scheduling
{
    /// <summary>
    /// 排课冲突校验服务单元测试，覆盖教师时段冲突、班级时段冲突、教室占用冲突、代课覆盖层等场景
    /// </summary>
    public class ScheduleConflictServiceTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly ScheduleConflictService _service;

        /// <summary>
        /// 构造函数，初始化测试上下文与 ScheduleConflictService 实例
        /// </summary>
        public ScheduleConflictServiceTests()
        {
            _dbContext = new TestDbContext();
            _service = new ScheduleConflictService(_dbContext, NullLogger<ScheduleConflictService>.Instance);
        }

        /// <summary>
        /// 无同期排课时应返回无冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_NoExistingSchedule_ReturnsNoConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            var input = CreateInput("T001", new List<long> { 1 }, "教室101", 1, 1, 2, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.False(result.HasConflict);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// 教师同一时段已有排课应返回教师时段冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_TeacherTimeConflict_ReturnsConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedCourseAsync(1, "课程1", "T001");
            // 创建已有排课：周一第1-2节第1-16周，教师T001
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：同一教师同一时段
            var input = CreateInput("T001", new List<long> { 2 }, "教室102", 1, 1, 2, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.True(result.HasConflict);
            Assert.Contains(result.Conflicts, c => c.Contains("教师时段冲突"));
        }

        /// <summary>
        /// 班级同一时段已有排课应返回班级时段冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ClassTimeConflict_ReturnsConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedTeacherAsync("T002", "教师B");
            await SeedClassAsync(1, "班级1");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T002");
            // 创建已有排课：周一第1-2节第1-16周，班级1
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：不同教师但同一班级同一时段
            var input = CreateInput("T002", new List<long> { 1 }, "教室102", 1, 1, 2, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.True(result.HasConflict);
            Assert.Contains(result.Conflicts, c => c.Contains("班级时段冲突"));
        }

        /// <summary>
        /// 教室同一时段已被占用应返回教室冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ClassroomConflict_ReturnsConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedTeacherAsync("T002", "教师B");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T002");
            // 创建已有排课：周一第1-2节第1-16周，教室101
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：不同教师不同班级但同一教室同一时段
            var input = CreateInput("T002", new List<long> { 2 }, "教室101", 1, 1, 2, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.True(result.HasConflict);
            Assert.Contains(result.Conflicts, c => c.Contains("教室冲突"));
        }

        /// <summary>
        /// 教师时段不重叠（周次错开）应返回无冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_TeacherDifferentWeeks_ReturnsNoConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T001");
            // 创建已有排课：第1-8周
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 8, "教室101");

            // 待校验排课：同一教师同一时段但第9-16周（周次不重叠）
            var input = CreateInput("T001", new List<long> { 2 }, "教室102", 1, 1, 2, 9, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.False(result.HasConflict);
        }

        /// <summary>
        /// 教师时段不重叠（节次错开）应返回无冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_TeacherDifferentSections_ReturnsNoConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T001");
            // 创建已有排课：第1-2节
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：同一教师同一时段但第3-4节（节次不重叠）
            var input = CreateInput("T001", new List<long> { 2 }, "教室102", 1, 3, 4, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.False(result.HasConflict);
        }

        /// <summary>
        /// 存在代课覆盖层时，原教师时段应视为空闲，代课教师时段应视为占用
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOverride_OriginalTeacherFreeSubstituteOccupied()
        {
            // Arrange
            await SeedTeacherAsync("T001", "原教师");
            await SeedTeacherAsync("T002", "代课教师");
            await SeedTeacherAsync("T003", "新教师");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T003");
            // 创建已有排课：教师T001，周一第1-2节第1-16周
            var scheduleId = await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");
            // 创建代课覆盖层：第5-10周由T002代课
            await CreateOverrideAsync(scheduleId, "T002", 5, 10);

            // 测试1：原教师T001在同一时段（第5-10周）应视为空闲
            var input1 = CreateInput("T001", new List<long> { 2 }, "教室102", 1, 1, 2, 5, 10);
            var result1 = await _service.ValidateAsync(input1);
            Assert.False(result1.HasConflict);

            // 测试2：代课教师T002在同一时段（第5-10周）应视为占用
            var input2 = CreateInput("T002", new List<long> { 2 }, "教室102", 1, 1, 2, 5, 10);
            var result2 = await _service.ValidateAsync(input2);
            Assert.True(result2.HasConflict);
            Assert.Contains(result2.Conflicts, c => c.Contains("教师时段冲突"));
        }

        /// <summary>
        /// 编辑排课时排除自身记录，避免与自身比较产生假冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_EditExistingSchedule_ExcludesSelf()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedCourseAsync(1, "课程1", "T001");
            // 创建已有排课
            var scheduleId = await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：传入 ScheduleId 排除自身
            var input = CreateInput("T001", new List<long> { 1 }, "教室101", 1, 1, 2, 1, 16, scheduleId);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.False(result.HasConflict);
        }

        /// <summary>
        /// 多项冲突并存时应返回全部冲突描述
        /// </summary>
        [Fact]
        public async Task ValidateAsync_MultipleConflicts_ReturnsAllConflicts()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedCourseAsync(1, "课程1", "T001");
            // 创建已有排课：教师T001，班级1，教室101
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：同一教师、同一班级、同一教室（三项冲突）
            var input = CreateInput("T001", new List<long> { 1 }, "教室101", 1, 1, 2, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.True(result.HasConflict);
            Assert.Equal(3, result.Conflicts.Count);
            Assert.Contains(result.Conflicts, c => c.Contains("教师时段冲突"));
            Assert.Contains(result.Conflicts, c => c.Contains("班级时段冲突"));
            Assert.Contains(result.Conflicts, c => c.Contains("教室冲突"));
        }

        /// <summary>
        /// 周次区间部分重叠时应返回冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_PartialWeekOverlap_ReturnsConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T001");
            // 创建已有排课：第1-8周
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 8, "教室101");

            // 待校验排课：同一教师同一时段，第5-12周（部分重叠：第5-8周）
            var input = CreateInput("T001", new List<long> { 2 }, "教室102", 1, 1, 2, 5, 12);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.True(result.HasConflict);
            Assert.Contains(result.Conflicts, c => c.Contains("教师时段冲突"));
        }

        /// <summary>
        /// 节次区间部分重叠时应返回冲突
        /// </summary>
        [Fact]
        public async Task ValidateAsync_PartialSectionOverlap_ReturnsConflict()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T001");
            // 创建已有排课：第1-2节
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：同一教师同一时段，第2-3节（部分重叠：第2节）
            var input = CreateInput("T001", new List<long> { 2 }, "教室102", 1, 2, 3, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert
            Assert.True(result.HasConflict);
            Assert.Contains(result.Conflicts, c => c.Contains("教师时段冲突"));
        }

        /// <summary>
        /// 教室为空时应跳过教室冲突校验
        /// </summary>
        [Fact]
        public async Task ValidateAsync_EmptyClassroom_SkipsClassroomConflictCheck()
        {
            // Arrange
            await SeedTeacherAsync("T001", "教师A");
            await SeedTeacherAsync("T002", "教师B");
            await SeedClassAsync(1, "班级1");
            await SeedClassAsync(2, "班级2");
            await SeedCourseAsync(1, "课程1", "T001");
            await SeedCourseAsync(2, "课程2", "T002");
            // 创建已有排课：教室101
            await CreateScheduleAsync(1, 1, "T001", 1, 1, 2, 1, 16, "教室101");

            // 待校验排课：不指定教室（教室为空）
            var input = CreateInput("T002", new List<long> { 2 }, "", 1, 1, 2, 1, 16);

            // Act
            var result = await _service.ValidateAsync(input);

            // Assert - 无冲突（教室校验跳过）
            Assert.False(result.HasConflict);
        }

        #region Helper Methods

        /// <summary>
        /// 创建 ScheduleValidationInput 实例
        /// </summary>
        private ScheduleValidationInput CreateInput(
            string teacherId,
            List<long> classIds,
            string classroom,
            int dayOfWeek,
            int startSection,
            int endSection,
            int startWeek,
            int endWeek,
            long? scheduleId = null)
        {
            return new ScheduleValidationInput
            {
                ScheduleId = scheduleId,
                TeacherId = teacherId,
                ClassIds = classIds,
                DayOfWeek = dayOfWeek,
                StartSection = startSection,
                EndSection = endSection,
                StartWeek = startWeek,
                EndWeek = endWeek,
                Classroom = classroom
            };
        }

        /// <summary>
        /// 插入教师基础数据
        /// </summary>
        private async Task SeedTeacherAsync(string id, string name)
        {
            await _dbContext.Client.Insertable(new Teacher
            {
                Id = id,
                Name = name,
                Password = "hashed_password",
                Gender = "男",
                DepartmentId = 1,
                Role = 0,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 插入班级基础数据
        /// </summary>
        private async Task SeedClassAsync(long id, string name)
        {
            await _dbContext.Client.Insertable(new Class
            {
                Id = id,
                Name = name,
                MajorId = 1,
                Grade = 2024,
                CounselorId = "C001",
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 插入课程基础数据
        /// </summary>
        private async Task SeedCourseAsync(long id, string name, string teacherId)
        {
            await _dbContext.Client.Insertable(new Course
            {
                Id = id,
                Name = name,
                TeacherId = teacherId,
                Credit = 2.0m, // decimal 类型
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 创建排课记录
        /// </summary>
        private async Task<long> CreateScheduleAsync(
            long courseId,
            long classId,
            string teacherId,
            int dayOfWeek,
            int startSection,
            int endSection,
            int startWeek,
            int endWeek,
            string classroom)
        {
            return await _dbContext.Client.Insertable(new CourseSchedule
            {
                CourseId = courseId,
                ClassId = classId,
                TeacherId = teacherId,
                DayOfWeek = dayOfWeek,
                StartSection = startSection,
                EndSection = endSection,
                StartWeek = startWeek,
                EndWeek = endWeek,
                Classroom = classroom,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
        }

        /// <summary>
        /// 创建代课覆盖层
        /// </summary>
        private async Task CreateOverrideAsync(long scheduleId, string substituteTeacherId, int startWeek, int endWeek)
        {
            await _dbContext.Client.Insertable(new CourseScheduleOverride
            {
                ScheduleId = scheduleId,
                SubstituteTeacherId = substituteTeacherId,
                StartWeek = startWeek,
                EndWeek = endWeek,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
        }

        #endregion

        /// <summary>
        /// 释放测试资源
        /// </summary>
        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}