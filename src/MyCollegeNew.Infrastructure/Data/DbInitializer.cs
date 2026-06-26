using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Data
{
    /// <summary>
    /// 数据库初始化器，负责 CodeFirst 自动建表与种子数据播种。
    /// DEBUG 模式下播种完整演示数据，RELEASE 模式下仅播种必要初始数据。
    /// </summary>
    public class DbInitializer
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<DbInitializer> _logger;

        /// <summary>
        /// 构造函数，注入数据库上下文与日志器
        /// </summary>
        public DbInitializer(IDbContext dbContext, ILogger<DbInitializer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 初始化数据库，使用 SqlSugar CodeFirst 自动创建所有实体表
        /// </summary>
        public void Initialize(CancellationToken cancellationToken = default)
        {
            var db = _dbContext.Client;
            _logger.LogInformation("开始执行 CodeFirst 自动建表");

            db.CodeFirst.InitTables(
                typeof(Department),
                typeof(Major),
                typeof(Class),
                typeof(Student),
                typeof(Teacher),
                typeof(Course),
                typeof(CourseSchedule),
                typeof(AttendanceSession),
                typeof(AttendanceRecord),
                typeof(LeaveRequest),
                typeof(SystemUser),
                typeof(AuditLog));

            _logger.LogInformation("CodeFirst 自动建表完成");
        }

        /// <summary>
        /// 异步播种种子数据。DEBUG 模式下播种完整演示数据，RELEASE 模式下仅播种管理员账号
        /// </summary>
        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var db = _dbContext.Client;
            _logger.LogInformation("开始播种种子数据（模式: {Mode}）",
#if DEBUG
                "DEBUG"
#else
                "RELEASE"
#endif
            );

            // RELEASE 与 DEBUG 均需：默认管理员
            await SeedSystemUserAsync(db);

#if DEBUG
            // 以下为 DEBUG 模式专属的完整演示数据
            var csDeptId = await SeedDepartmentAsync(db, "计算机学院");
            var eeDeptId = await SeedDepartmentAsync(db, "电子工程学院");
            var mgDeptId = await SeedDepartmentAsync(db, "经济管理学院");

            var seMajorId = await SeedMajorAsync(db, "软件工程", csDeptId);
            var csMajorId = await SeedMajorAsync(db, "计算机科学与技术", csDeptId);
            var eeMajorId = await SeedMajorAsync(db, "电子信息工程", eeDeptId);
            var accMajorId = await SeedMajorAsync(db, "会计学", mgDeptId);

            // 任课教师
            var t001 = await SeedTeacherAsync(db, "T001", "张明", "男", csDeptId, seMajorId, TeacherRole.Teacher);
            var t002 = await SeedTeacherAsync(db, "T002", "刘芳", "女", csDeptId, csMajorId, TeacherRole.Teacher);
            var t003 = await SeedTeacherAsync(db, "T003", "陈伟", "男", eeDeptId, eeMajorId, TeacherRole.Teacher);
            var t004 = await SeedTeacherAsync(db, "T004", "赵丽", "女", mgDeptId, accMajorId, TeacherRole.Teacher);
            var t005 = await SeedTeacherAsync(db, "T005", "王强", "男", csDeptId, seMajorId, TeacherRole.Teacher);

            // 辅导员
            var t101 = await SeedTeacherAsync(db, "T101", "李红", "女", csDeptId, null, TeacherRole.Counselor);
            var t102 = await SeedTeacherAsync(db, "T102", "周建国", "男", csDeptId, null, TeacherRole.Counselor);
            var t103 = await SeedTeacherAsync(db, "T103", "孙丽华", "女", eeDeptId, null, TeacherRole.Counselor);
            var t104 = await SeedTeacherAsync(db, "T104", "吴明辉", "男", mgDeptId, null, TeacherRole.Counselor);

            // 班级
            var se2201Id = await SeedClassAsync(db, "软工2201", seMajorId, 2022, "T101");
            var se2202Id = await SeedClassAsync(db, "软工2202", seMajorId, 2022, "T101");
            var cs2201Id = await SeedClassAsync(db, "计科2201", csMajorId, 2022, "T102");
            var ee2201Id = await SeedClassAsync(db, "电信2201", eeMajorId, 2022, "T103");
            var acc2201Id = await SeedClassAsync(db, "会计2201", accMajorId, 2022, "T104");

            // 课程
            var mathCourseId = await SeedCourseAsync(db, "高等数学", "T001", 4m);
            var progCourseId = await SeedCourseAsync(db, "程序设计基础", "T005", 3m);
            var dbCourseId = await SeedCourseAsync(db, "数据库原理", "T001", 3m);
            var osCourseId = await SeedCourseAsync(db, "操作系统", "T002", 3.5m);
            var dsCourseId = await SeedCourseAsync(db, "数据结构", "T002", 3.5m);
            var circuitCourseId = await SeedCourseAsync(db, "电路分析", "T003", 4m);
            var acctCourseId = await SeedCourseAsync(db, "基础会计", "T004", 3m);

            // 课表
            await SeedCourseScheduleAsync(db, mathCourseId, se2201Id, "T001", 1, 1, 2, 1, 16, "A301");
            await SeedCourseScheduleAsync(db, mathCourseId, se2202Id, "T001", 1, 3, 4, 1, 16, "A302");
            await SeedCourseScheduleAsync(db, progCourseId, se2201Id, "T005", 2, 1, 2, 1, 16, "B201");
            await SeedCourseScheduleAsync(db, progCourseId, se2202Id, "T005", 2, 3, 4, 1, 16, "B202");
            await SeedCourseScheduleAsync(db, dbCourseId, se2201Id, "T001", 3, 1, 2, 1, 16, "C101");
            await SeedCourseScheduleAsync(db, osCourseId, cs2201Id, "T002", 1, 3, 4, 1, 16, "A401");
            await SeedCourseScheduleAsync(db, dsCourseId, cs2201Id, "T002", 3, 1, 2, 1, 16, "A401");
            await SeedCourseScheduleAsync(db, circuitCourseId, ee2201Id, "T003", 2, 1, 2, 1, 16, "D301");
            await SeedCourseScheduleAsync(db, acctCourseId, acc2201Id, "T004", 1, 1, 2, 1, 16, "E201");

            // 学生（软工2201 班 8 人，软工2202 班 4 人，计科2201 班 4 人，电信2201 班 3 人，会计2201 班 3 人）
            await SeedStudentAsync(db, "2022010101", "王浩", "男", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010102", "李雪", "女", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010103", "张磊", "男", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010104", "陈静", "女", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010105", "刘洋", "男", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010106", "赵敏", "女", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010107", "孙鹏", "男", csDeptId, seMajorId, se2201Id, 2022);
            await SeedStudentAsync(db, "2022010108", "周婷", "女", csDeptId, seMajorId, se2201Id, 2022);

            await SeedStudentAsync(db, "2022010201", "吴昊", "男", csDeptId, seMajorId, se2202Id, 2022);
            await SeedStudentAsync(db, "2022010202", "郑颖", "女", csDeptId, seMajorId, se2202Id, 2022);
            await SeedStudentAsync(db, "2022010203", "钱伟", "男", csDeptId, seMajorId, se2202Id, 2022);
            await SeedStudentAsync(db, "2022010204", "许琳", "女", csDeptId, seMajorId, se2202Id, 2022);

            await SeedStudentAsync(db, "2022020101", "冯杰", "男", csDeptId, csMajorId, cs2201Id, 2022);
            await SeedStudentAsync(db, "2022020102", "蒋薇", "女", csDeptId, csMajorId, cs2201Id, 2022);
            await SeedStudentAsync(db, "2022020103", "韩超", "男", csDeptId, csMajorId, cs2201Id, 2022);
            await SeedStudentAsync(db, "2022020104", "杨倩", "女", csDeptId, csMajorId, cs2201Id, 2022);

            await SeedStudentAsync(db, "2022030101", "朱磊", "男", eeDeptId, eeMajorId, ee2201Id, 2022);
            await SeedStudentAsync(db, "2022030102", "秦悦", "女", eeDeptId, eeMajorId, ee2201Id, 2022);
            await SeedStudentAsync(db, "2022030103", "许峰", "男", eeDeptId, eeMajorId, ee2201Id, 2022);

            await SeedStudentAsync(db, "2022040101", "何佳", "女", mgDeptId, accMajorId, acc2201Id, 2022);
            await SeedStudentAsync(db, "2022040102", "吕鑫", "男", mgDeptId, accMajorId, acc2201Id, 2022);
            await SeedStudentAsync(db, "2022040103", "施婷", "女", mgDeptId, accMajorId, acc2201Id, 2022);

            // 考勤会话与考勤记录
            var now = DateTime.UtcNow;
            var session1Id = await SeedAttendanceSessionAsync(db, mathCourseId, se2201Id, "T001",
                now.AddHours(-2), now.AddHours(-1).AddMinutes(-30), SessionStatus.Closed);
            var session2Id = await SeedAttendanceSessionAsync(db, progCourseId, se2201Id, "T005",
                now.AddMinutes(-30), now.AddMinutes(30), SessionStatus.Active);

            // 软工2201 高数考勤记录（已关闭会话）
            await SeedAttendanceRecordAsync(db, session1Id, "2022010101", "王浩", AttendanceStatus.Present, now.AddHours(-2).AddMinutes(3));
            await SeedAttendanceRecordAsync(db, session1Id, "2022010102", "李雪", AttendanceStatus.Present, now.AddHours(-2).AddMinutes(1));
            await SeedAttendanceRecordAsync(db, session1Id, "2022010103", "张磊", AttendanceStatus.Late, now.AddHours(-1).AddMinutes(-50));
            await SeedAttendanceRecordAsync(db, session1Id, "2022010104", "陈静", AttendanceStatus.Present, now.AddHours(-2).AddMinutes(5));
            await SeedAttendanceRecordAsync(db, session1Id, "2022010105", "刘洋", AttendanceStatus.Absent, null);
            await SeedAttendanceRecordAsync(db, session1Id, "2022010106", "赵敏", AttendanceStatus.Leave, null);
            await SeedAttendanceRecordAsync(db, session1Id, "2022010107", "孙鹏", AttendanceStatus.Present, now.AddHours(-2).AddMinutes(2));
            await SeedAttendanceRecordAsync(db, session1Id, "2022010108", "周婷", AttendanceStatus.Present, now.AddHours(-2).AddMinutes(4));

            // 软工2201 程序设计考勤记录（进行中会话，部分已签到）
            await SeedAttendanceRecordAsync(db, session2Id, "2022010101", "王浩", AttendanceStatus.Present, now.AddMinutes(-28));
            await SeedAttendanceRecordAsync(db, session2Id, "2022010102", "李雪", AttendanceStatus.Present, now.AddMinutes(-25));
            await SeedAttendanceRecordAsync(db, session2Id, "2022010103", "张磊", AttendanceStatus.Present, now.AddMinutes(-20));

            // 请假申请
            await SeedLeaveRequestAsync(db, "2022010106", "T101",
                now.AddHours(-5), now.AddHours(3),
                LeaveType.Sick, "感冒发烧，需休息半天", LeaveStatus.Approved,
                "注意休息，早日康复", now.AddHours(-4));

            await SeedLeaveRequestAsync(db, "2022010105", "T101",
                now.AddHours(-1), now.AddHours(5),
                LeaveType.Personal, "家中有急事需处理", LeaveStatus.Pending,
                null, null);

            await SeedLeaveRequestAsync(db, "2022010203", "T101",
                now.AddDays(-3), now.AddDays(-2),
                LeaveType.Official, "参加省级编程竞赛", LeaveStatus.Approved,
                "已确认竞赛通知，同意请假", now.AddDays(-3).AddHours(1));

            await SeedLeaveRequestAsync(db, "2022020103", "T102",
                now.AddDays(-1), now.AddDays(1),
                LeaveType.Sick, "胃痛需就医检查", LeaveStatus.Rejected,
                "请提供医院诊断证明后重新申请", now.AddDays(-1).AddHours(2));
#endif

            _logger.LogInformation("种子数据播种完成");
        }

        #region 通用播种方法

        /// <summary>
        /// 播种默认管理员账号 admin/123456
        /// </summary>
        private async Task SeedSystemUserAsync(ISqlSugarClient db)
        {
            var exists = await db.Queryable<SystemUser>().AnyAsync(u => u.Username == "admin");
            if (exists) return;

            await db.Insertable(new SystemUser
            {
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Role = UserRole.Admin,
                RealName = "系统管理员",
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
            _logger.LogInformation("已播种默认管理员账号 admin");
        }

        /// <summary>
        /// 播种院系，返回院系 Id
        /// </summary>
        private async Task<long> SeedDepartmentAsync(ISqlSugarClient db, string name)
        {
            var entity = await db.Queryable<Department>().FirstAsync(d => d.Name == name);
            if (entity is not null) return entity.Id;

            var id = await db.Insertable(new Department
            {
                Name = name,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
            _logger.LogInformation("已播种院系「{Name}」", name);
            return id;
        }

        /// <summary>
        /// 播种专业，返回专业 Id
        /// </summary>
        private async Task<long> SeedMajorAsync(ISqlSugarClient db, string name, long departmentId)
        {
            var entity = await db.Queryable<Major>().FirstAsync(m => m.Name == name && m.DepartmentId == departmentId);
            if (entity is not null) return entity.Id;

            var id = await db.Insertable(new Major
            {
                Name = name,
                DepartmentId = departmentId,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
            _logger.LogInformation("已播种专业「{Name}」", name);
            return id;
        }

        /// <summary>
        /// 播种教师，返回工号
        /// </summary>
        private async Task<string> SeedTeacherAsync(ISqlSugarClient db, string id, string name,
            string gender, long departmentId, long? majorId, TeacherRole role)
        {
            var exists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == id);
            if (exists) return id;

            await db.Insertable(new Teacher
            {
                Id = id,
                Name = name,
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Gender = gender,
                DepartmentId = departmentId,
                MajorId = majorId,
                Role = role,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
            _logger.LogInformation("已播种教师 {Id}「{Name}」", id, name);
            return id;
        }

        /// <summary>
        /// 播种班级，返回班级 Id
        /// </summary>
        private async Task<long> SeedClassAsync(ISqlSugarClient db, string name, long majorId,
            int grade, string counselorId)
        {
            var entity = await db.Queryable<Class>().FirstAsync(c => c.Name == name && c.MajorId == majorId);
            if (entity is not null) return entity.Id;

            var id = await db.Insertable(new Class
            {
                Name = name,
                MajorId = majorId,
                Grade = grade,
                CounselorId = counselorId,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
            _logger.LogInformation("已播种班级「{Name}」", name);
            return id;
        }

        /// <summary>
        /// 播种学生，密码统一为学号后 6 位
        /// </summary>
        private async Task SeedStudentAsync(ISqlSugarClient db, string id, string name,
            string gender, long departmentId, long majorId, long classId, int grade)
        {
            var exists = await db.Queryable<Student>().AnyAsync(s => s.Id == id);
            if (exists) return;

            await db.Insertable(new Student
            {
                Id = id,
                Name = name,
                Password = BCrypt.Net.BCrypt.HashPassword(id.Length >= 6 ? id[^6..] : id),
                Gender = gender,
                DepartmentId = departmentId,
                MajorId = majorId,
                ClassId = classId,
                Grade = grade,
                Status = 0,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
            _logger.LogInformation("已播种学生 {Id}「{Name}」", id, name);
        }

        /// <summary>
        /// 播种课程，返回课程 Id
        /// </summary>
        private async Task<long> SeedCourseAsync(ISqlSugarClient db, string name, string teacherId, decimal credit)
        {
            var entity = await db.Queryable<Course>().FirstAsync(c => c.Name == name && c.TeacherId == teacherId);
            if (entity is not null) return entity.Id;

            var id = await db.Insertable(new Course
            {
                Name = name,
                TeacherId = teacherId,
                Credit = credit,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
            _logger.LogInformation("已播种课程「{Name}」", name);
            return id;
        }

        /// <summary>
        /// 播种课表
        /// </summary>
        private async Task SeedCourseScheduleAsync(ISqlSugarClient db, long courseId, long classId,
            string teacherId, int dayOfWeek, int startSection, int endSection,
            int startWeek, int endWeek, string classroom)
        {
            var exists = await db.Queryable<CourseSchedule>().AnyAsync(s =>
                s.CourseId == courseId && s.ClassId == classId && s.DayOfWeek == dayOfWeek
                && s.StartSection == startSection && s.StartWeek == startWeek);
            if (exists) return;

            await db.Insertable(new CourseSchedule
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
            }).ExecuteCommandAsync();
            _logger.LogInformation("已播种课表：课程{CourseId} 班级{ClassId} 周{DayOfWeek} 第{Start}-{End}节",
                courseId, classId, dayOfWeek, startSection, endSection);
        }

        /// <summary>
        /// 播种考勤会话，返回会话 Id
        /// </summary>
        private async Task<long> SeedAttendanceSessionAsync(ISqlSugarClient db, long courseId,
            long classId, string teacherId, DateTime startTime, DateTime endTime, SessionStatus status)
        {
            var id = await db.Insertable(new AttendanceSession
            {
                CourseId = courseId,
                ClassId = classId,
                TeacherId = teacherId,
                StartTime = startTime,
                EndTime = endTime,
                Status = status,
                CreateTime = DateTime.UtcNow
            }).ExecuteReturnIdentityAsync();
            _logger.LogInformation("已播种考勤会话 {Id}", id);
            return id;
        }

        /// <summary>
        /// 播种考勤记录
        /// </summary>
        private async Task SeedAttendanceRecordAsync(ISqlSugarClient db, long sessionId,
            string studentId, string studentName, AttendanceStatus status, DateTime? checkInTime)
        {
            var exists = await db.Queryable<AttendanceRecord>().AnyAsync(r =>
                r.SessionId == sessionId && r.StudentId == studentId);
            if (exists) return;

            await db.Insertable(new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = studentId,
                StudentName = studentName,
                Status = status,
                CheckInTime = checkInTime,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 播种请假申请
        /// </summary>
        private async Task SeedLeaveRequestAsync(ISqlSugarClient db, string studentId,
            string counselorId, DateTime startTime, DateTime endTime,
            LeaveType leaveType, string reason, LeaveStatus status,
            string? reviewRemark, DateTime? reviewTime)
        {
            var exists = await db.Queryable<LeaveRequest>().AnyAsync(l =>
                l.StudentId == studentId && l.CounselorId == counselorId
                && l.StartTime == startTime && l.LeaveType == leaveType);
            if (exists) return;

            await db.Insertable(new LeaveRequest
            {
                StudentId = studentId,
                CounselorId = counselorId,
                StartTime = startTime,
                EndTime = endTime,
                LeaveType = leaveType,
                Reason = reason,
                Status = status,
                ReviewRemark = reviewRemark,
                ReviewTime = reviewTime,
                CreateTime = DateTime.UtcNow
            }).ExecuteCommandAsync();
            _logger.LogInformation("已播种请假申请：学生{StudentId} 类型{LeaveType} 状态{Status}",
                studentId, leaveType, status);
        }

        #endregion
    }
}
