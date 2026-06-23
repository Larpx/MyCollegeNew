using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Courses;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Campus.Attendance.Services.Courses;

/// <summary>
/// 课表管理服务实现，封装课表的增删改查与按教师/学生/班级的周课表查询
/// </summary>
public class ScheduleService : IScheduleService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<ScheduleService> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public ScheduleService(IDbContext dbContext, ILogger<ScheduleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询课表，支持班级、教师、课程过滤
    /// </summary>
    public async Task<PagedResult<ScheduleResponseDto>> GetSchedulesAsync(
        int pageIndex, int pageSize, long? classId = null, string? teacherId = null, long? courseId = null,
        CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var query = db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => !s.IsDeleted);

        if (classId.HasValue)
        {
            query = query.Where((s, c, cls, t) => s.ClassId == classId.Value);
        }

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            query = query.Where((s, c, cls, t) => s.TeacherId == teacherId);
        }

        if (courseId.HasValue)
        {
            query = query.Where((s, c, cls, t) => s.CourseId == courseId.Value);
        }

        var total = await query.CountAsync();
        var rows = await query
            .Select((s, c, cls, t) => new ScheduleResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                DayOfWeek = s.DayOfWeek,
                StartSection = s.StartSection,
                EndSection = s.EndSection,
                StartWeek = s.StartWeek,
                EndWeek = s.EndWeek,
                Classroom = s.Classroom
            })
            .OrderBy(s => s.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<ScheduleResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 查询课表详情
    /// </summary>
    public async Task<ScheduleResponseDto?> GetScheduleByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => s.Id == id && !s.IsDeleted)
            .Select((s, c, cls, t) => new ScheduleResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                DayOfWeek = s.DayOfWeek,
                StartSection = s.StartSection,
                EndSection = s.EndSection,
                StartWeek = s.StartWeek,
                EndWeek = s.EndWeek,
                Classroom = s.Classroom
            })
            .FirstAsync();

        return dto;
    }

    /// <summary>
    /// 创建排课，需校验课程、班级、教师存在，并校验节次与周次范围
    /// </summary>
    public async Task<ScheduleResponseDto> CreateScheduleAsync(ScheduleCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 校验关联实体存在
        var courseExists = await db.Queryable<Course>().AnyAsync(c => c.Id == dto.CourseId && !c.IsDeleted);
        if (!courseExists)
        {
            throw new BusinessException($"课程 {dto.CourseId} 不存在", 404);
        }

        var classExists = await db.Queryable<Class>().AnyAsync(c => c.Id == dto.ClassId && !c.IsDeleted);
        if (!classExists)
        {
            throw new BusinessException($"班级 {dto.ClassId} 不存在", 404);
        }

        var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.TeacherId && !t.IsDeleted);
        if (!teacherExists)
        {
            throw new BusinessException($"教师 {dto.TeacherId} 不存在", 404);
        }

        // 校验节次与周次范围
        if (dto.StartSection > dto.EndSection)
        {
            throw new BusinessException("起始节次不能大于结束节次", 400);
        }

        if (dto.StartWeek > dto.EndWeek)
        {
            throw new BusinessException("起始周次不能大于结束周次", 400);
        }

        var schedule = new CourseSchedule
        {
            CourseId = dto.CourseId,
            ClassId = dto.ClassId,
            TeacherId = dto.TeacherId,
            DayOfWeek = dto.DayOfWeek,
            StartSection = dto.StartSection,
            EndSection = dto.EndSection,
            StartWeek = dto.StartWeek,
            EndWeek = dto.EndWeek,
            Classroom = dto.Classroom,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(schedule).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建课表 {ScheduleId}（课程 {CourseId} 班级 {ClassId}）", id, dto.CourseId, dto.ClassId);

        return (await GetScheduleByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 更新课表
    /// </summary>
    public async Task<ScheduleResponseDto> UpdateScheduleAsync(long id, ScheduleUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var schedule = await db.Queryable<CourseSchedule>().FirstAsync(s => s.Id == id && !s.IsDeleted);
        if (schedule is null)
        {
            throw new BusinessException($"课表 {id} 不存在", 404);
        }

        // 校验关联实体存在
        var courseExists = await db.Queryable<Course>().AnyAsync(c => c.Id == dto.CourseId && !c.IsDeleted);
        if (!courseExists)
        {
            throw new BusinessException($"课程 {dto.CourseId} 不存在", 404);
        }

        var classExists = await db.Queryable<Class>().AnyAsync(c => c.Id == dto.ClassId && !c.IsDeleted);
        if (!classExists)
        {
            throw new BusinessException($"班级 {dto.ClassId} 不存在", 404);
        }

        var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.TeacherId && !t.IsDeleted);
        if (!teacherExists)
        {
            throw new BusinessException($"教师 {dto.TeacherId} 不存在", 404);
        }

        if (dto.StartSection > dto.EndSection)
        {
            throw new BusinessException("起始节次不能大于结束节次", 400);
        }

        if (dto.StartWeek > dto.EndWeek)
        {
            throw new BusinessException("起始周次不能大于结束周次", 400);
        }

        schedule.CourseId = dto.CourseId;
        schedule.ClassId = dto.ClassId;
        schedule.TeacherId = dto.TeacherId;
        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.StartSection = dto.StartSection;
        schedule.EndSection = dto.EndSection;
        schedule.StartWeek = dto.StartWeek;
        schedule.EndWeek = dto.EndWeek;
        schedule.Classroom = dto.Classroom;
        schedule.UpdateTime = DateTime.UtcNow;
        await db.Updateable(schedule).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新课表 {ScheduleId}", id);

        return (await GetScheduleByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除课表
    /// </summary>
    public async Task DeleteScheduleAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var schedule = await db.Queryable<CourseSchedule>().FirstAsync(s => s.Id == id && !s.IsDeleted);
        if (schedule is null)
        {
            throw new BusinessException($"课表 {id} 不存在", 404);
        }

        schedule.IsDeleted = true;
        schedule.UpdateTime = DateTime.UtcNow;
        await db.Updateable(schedule).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除课表 {ScheduleId}", id);
    }

    /// <summary>
    /// 按教师查询某周课表（返回按星期分组的课表）
    /// </summary>
    public async Task<WeeklyScheduleDto> GetScheduleByTeacherAsync(string teacherId, int week, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var schedules = await db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => !s.IsDeleted && s.TeacherId == teacherId
                && s.StartWeek <= week && s.EndWeek >= week)
            .Select((s, c, cls, t) => new ScheduleResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                DayOfWeek = s.DayOfWeek,
                StartSection = s.StartSection,
                EndSection = s.EndSection,
                StartWeek = s.StartWeek,
                EndWeek = s.EndWeek,
                Classroom = s.Classroom
            })
            .OrderBy(s => s.DayOfWeek)
            .OrderBy(s => s.StartSection)
            .ToListAsync();

        return BuildWeeklySchedule(week, schedules);
    }

    /// <summary>
    /// 按学生查询某周课表（通过班级关联）
    /// </summary>
    public async Task<WeeklyScheduleDto> GetScheduleByStudentAsync(string studentId, int week, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student is null)
        {
            throw new BusinessException($"学生 {studentId} 不存在", 404);
        }

        return await GetScheduleByClassAsync((int)student.ClassId, week, cancellationToken);
    }

    /// <summary>
    /// 按班级查询某周课表
    /// </summary>
    public async Task<WeeklyScheduleDto> GetScheduleByClassAsync(int classId, int week, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var schedules = await db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => !s.IsDeleted && s.ClassId == classId
                && s.StartWeek <= week && s.EndWeek >= week)
            .Select((s, c, cls, t) => new ScheduleResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = c.Name,
                ClassId = s.ClassId,
                ClassName = cls.Name,
                TeacherId = s.TeacherId,
                TeacherName = t.Name,
                DayOfWeek = s.DayOfWeek,
                StartSection = s.StartSection,
                EndSection = s.EndSection,
                StartWeek = s.StartWeek,
                EndWeek = s.EndWeek,
                Classroom = s.Classroom
            })
            .OrderBy(s => s.DayOfWeek)
            .OrderBy(s => s.StartSection)
            .ToListAsync();

        return BuildWeeklySchedule(week, schedules);
    }

    /// <summary>
    /// 将课表列表按 DayOfWeek 分组构造周课表 DTO
    /// </summary>
    /// <param name="week">查询的周次</param>
    /// <param name="schedules">课表列表</param>
    /// <returns>按星期分组的周课表</returns>
    private static WeeklyScheduleDto BuildWeeklySchedule(int week, List<ScheduleResponseDto> schedules)
    {
        var days = new Dictionary<int, List<ScheduleResponseDto>>();
        foreach (var schedule in schedules)
        {
            if (!days.ContainsKey(schedule.DayOfWeek))
            {
                days[schedule.DayOfWeek] = new List<ScheduleResponseDto>();
            }
            days[schedule.DayOfWeek].Add(schedule);
        }

        return new WeeklyScheduleDto { Week = week, Days = days };
    }
}
