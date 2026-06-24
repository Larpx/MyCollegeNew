using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Data
{
/// <summary>
/// 数据库初始化器，负责 CodeFirst 自动建表与种子数据播种
/// </summary>
public class DbInitializer
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<DbInitializer> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public DbInitializer(IDbContext dbContext, ILogger<DbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 异步初始化数据库，使用 SqlSugar CodeFirst 自动创建所有实体表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        _logger.LogInformation("开始执行 CodeFirst 自动建表");

        // CodeFirst.InitTables 为同步 API（SqlSugar 未提供异步版本），启动时一次性执行
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
    /// 异步播种种子数据，包含默认管理员、示例院系/专业/班级/教师/学生/课程，重复执行不会产生重复数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        _logger.LogInformation("开始播种种子数据");

        await SeedSystemUserAsync(db);
        var departmentId = await SeedDepartmentAsync(db);
        var majorId = await SeedMajorAsync(db, departmentId);
        await SeedTeacherT001Async(db, departmentId, majorId);
        await SeedTeacherT002Async(db, departmentId);
        await SeedClassAsync(db, majorId);
        await SeedStudentAsync(db, departmentId, majorId);
        await SeedCourseAsync(db);

        _logger.LogInformation("种子数据播种完成");
    }

    /// <summary>
    /// 播种默认管理员账号 admin/123456
    /// </summary>
    private async Task SeedSystemUserAsync(ISqlSugarClient db)
    {
        var exists = await db.Queryable<SystemUser>().AnyAsync(u => u.Username == "admin");
        if (exists)
        {
            return;
        }

        var admin = new SystemUser
        {
            Username = "admin",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = UserRole.Admin,
            RealName = "系统管理员",
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(admin).ExecuteCommandAsync();
        _logger.LogInformation("已播种默认管理员账号 admin");
    }

    /// <summary>
    /// 播种示例院系「计算机学院」，返回院系 Id
    /// </summary>
    private async Task<long> SeedDepartmentAsync(ISqlSugarClient db)
    {
        var department = await db.Queryable<Department>().FirstAsync(d => d.Name == "计算机学院");
        if (department is not null)
        {
            return department.Id;
        }

        department = new Department
        {
            Name = "计算机学院",
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(department).ExecuteReturnIdentityAsync();
        _logger.LogInformation("已播种示例院系「计算机学院」");
        return id;
    }

    /// <summary>
    /// 播种示例专业「软件工程」，返回专业 Id
    /// </summary>
    private async Task<long> SeedMajorAsync(ISqlSugarClient db, long departmentId)
    {
        var major = await db.Queryable<Major>().FirstAsync(m => m.Name == "软件工程" && m.DepartmentId == departmentId);
        if (major is not null)
        {
            return major.Id;
        }

        major = new Major
        {
            Name = "软件工程",
            DepartmentId = departmentId,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(major).ExecuteReturnIdentityAsync();
        _logger.LogInformation("已播种示例专业「软件工程」");
        return id;
    }

    /// <summary>
    /// 播种示例任课教师 T001（高等数学任课教师）
    /// </summary>
    private async Task SeedTeacherT001Async(ISqlSugarClient db, long departmentId, long majorId)
    {
        var exists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == "T001");
        if (exists)
        {
            return;
        }

        var teacher = new Teacher
        {
            Id = "T001",
            Name = "张老师",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "男",
            DepartmentId = departmentId,
            MajorId = majorId,
            Role = TeacherRole.Teacher,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(teacher).ExecuteCommandAsync();
        _logger.LogInformation("已播种示例任课教师 T001");
    }

    /// <summary>
    /// 播种示例辅导员 T002（软工2201 辅导员）
    /// </summary>
    private async Task SeedTeacherT002Async(ISqlSugarClient db, long departmentId)
    {
        var exists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == "T002");
        if (exists)
        {
            return;
        }

        var teacher = new Teacher
        {
            Id = "T002",
            Name = "李老师",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "女",
            DepartmentId = departmentId,
            MajorId = null,
            Role = TeacherRole.Counselor,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(teacher).ExecuteCommandAsync();
        _logger.LogInformation("已播种示例辅导员 T002");
    }

    /// <summary>
    /// 播种示例班级「软工2201」，辅导员为 T002
    /// </summary>
    private async Task SeedClassAsync(ISqlSugarClient db, long majorId)
    {
        var exists = await db.Queryable<Class>().AnyAsync(c => c.Name == "软工2201" && c.MajorId == majorId);
        if (exists)
        {
            return;
        }

        var entity = new Class
        {
            Name = "软工2201",
            MajorId = majorId,
            Grade = 2022,
            CounselorId = "T002",
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(entity).ExecuteCommandAsync();
        _logger.LogInformation("已播种示例班级「软工2201」");
    }

    /// <summary>
    /// 播种示例学生 20220101，密码为学号后 6 位
    /// </summary>
    private async Task SeedStudentAsync(ISqlSugarClient db, long departmentId, long majorId)
    {
        var exists = await db.Queryable<Student>().AnyAsync(s => s.Id == "20220101");
        if (exists)
        {
            return;
        }

        var classEntity = await db.Queryable<Class>().FirstAsync(c => c.Name == "软工2201" && c.MajorId == majorId);
        if (classEntity is null)
        {
            _logger.LogWarning("播种学生时未找到班级「软工2201」，跳过");
            return;
        }

        var student = new Student
        {
            Id = "20220101",
            Name = "王同学",
            Password = BCrypt.Net.BCrypt.HashPassword("220101"),
            Gender = "男",
            DepartmentId = departmentId,
            MajorId = majorId,
            ClassId = classEntity.Id,
            Grade = 2022,
            Status = 0,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(student).ExecuteCommandAsync();
        _logger.LogInformation("已播种示例学生 20220101");
    }

    /// <summary>
    /// 播种示例课程「高等数学」，任课教师 T001
    /// </summary>
    private async Task SeedCourseAsync(ISqlSugarClient db)
    {
        var exists = await db.Queryable<Course>().AnyAsync(c => c.Name == "高等数学" && c.TeacherId == "T001");
        if (exists)
        {
            return;
        }

        var course = new Course
        {
            Name = "高等数学",
            TeacherId = "T001",
            Credit = 4m,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(course).ExecuteCommandAsync();
        _logger.LogInformation("已播种示例课程「高等数学」");
    }
}
}