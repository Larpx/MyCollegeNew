using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Courses;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Services.Courses;

/// <summary>
/// 课程管理服务实现，封装课程的增删改查与按教师/班级查询
/// </summary>
public class CourseService : ICourseService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<CourseService> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public CourseService(IDbContext dbContext, ILogger<CourseService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询课程，支持关键字（课程名称）与教师工号过滤
    /// </summary>
    public async Task<PagedResult<CourseResponseDto>> GetCoursesAsync(
        int pageIndex, int pageSize, string? keyword = null, string? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var query = db.Queryable<Course, Teacher>((c, t) =>
                new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where((c, t) => c.Name.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            query = query.Where((c, t) => c.TeacherId == teacherId);
        }

        var total = await query.CountAsync();
        var rows = await query
            .Select((c, t) => new CourseResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                TeacherId = c.TeacherId,
                TeacherName = t.Name,
                Credit = c.Credit,
                Remark = c.Remark
            })
            .OrderBy(c => c.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<CourseResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 查询课程详情（含教师姓名）
    /// </summary>
    public async Task<CourseResponseDto?> GetCourseByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<Course, Teacher>((c, t) =>
                new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => c.Id == id && !c.IsDeleted)
            .Select((c, t) => new CourseResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                TeacherId = c.TeacherId,
                TeacherName = t.Name,
                Credit = c.Credit,
                Remark = c.Remark
            })
            .FirstAsync();

        return dto;
    }

    /// <summary>
    /// 创建课程，需校验任课教师存在
    /// </summary>
    public async Task<CourseResponseDto> CreateCourseAsync(CourseCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.TeacherId && !t.IsDeleted);
        if (!teacherExists)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"教师 {dto.TeacherId}"), 404);
        }

        var course = new Course
        {
            Name = dto.Name,
            TeacherId = dto.TeacherId,
            Credit = dto.Credit,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(course).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建课程 {CourseId}（教师 {TeacherId}）", id, dto.TeacherId);

        return (await GetCourseByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 更新课程
    /// </summary>
    public async Task<CourseResponseDto> UpdateCourseAsync(long id, CourseUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var course = await db.Queryable<Course>().FirstAsync(c => c.Id == id && !c.IsDeleted);
        if (course is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"课程 {id}"), 404);
        }

        var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.TeacherId && !t.IsDeleted);
        if (!teacherExists)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"教师 {dto.TeacherId}"), 404);
        }

        course.Name = dto.Name;
        course.TeacherId = dto.TeacherId;
        course.Credit = dto.Credit;
        course.Remark = dto.Remark;
        course.UpdateTime = DateTime.UtcNow;
        await db.Updateable(course).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新课程 {CourseId}", id);

        return (await GetCourseByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除课程
    /// </summary>
    public async Task DeleteCourseAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var course = await db.Queryable<Course>().FirstAsync(c => c.Id == id && !c.IsDeleted);
        if (course is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"课程 {id}"), 404);
        }

        await _dbContext.SoftDeleteAsync(course, cancellationToken);
        _logger.LogInformation("软删除课程 {CourseId}", id);
    }

    /// <summary>
    /// 按教师查询课程列表
    /// </summary>
    public async Task<List<CourseResponseDto>> GetCoursesByTeacherAsync(string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var rows = await db.Queryable<Course, Teacher>((c, t) =>
                new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => !c.IsDeleted && c.TeacherId == teacherId)
            .OrderBy((c, t) => c.Id)
            .Select((c, t) => new CourseResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                TeacherId = c.TeacherId,
                TeacherName = t.Name,
                Credit = c.Credit,
                Remark = c.Remark
            })
            .ToListAsync();

        return rows;
    }

    /// <summary>
    /// 按班级查询课程列表（通过课表关联，去重）
    /// </summary>
    public async Task<List<CourseResponseDto>> GetCoursesByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        // 通过课表关联查询该班级的所有课程 Id（去重），再关联教师信息
        var courseIds = await db.Queryable<CourseSchedule>()
            .Where(s => !s.IsDeleted && s.ClassId == classId)
            .Select(s => s.CourseId)
            .Distinct()
            .ToListAsync();

        if (courseIds.Count == 0)
        {
            return new List<CourseResponseDto>();
        }

        var rows = await db.Queryable<Course, Teacher>((c, t) =>
                new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => !c.IsDeleted && courseIds.Contains(c.Id))
            .OrderBy((c, t) => c.Id)
            .Select((c, t) => new CourseResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                TeacherId = c.TeacherId,
                TeacherName = t.Name,
                Credit = c.Credit,
                Remark = c.Remark
            })
            .ToListAsync();

        return rows;
    }
}
