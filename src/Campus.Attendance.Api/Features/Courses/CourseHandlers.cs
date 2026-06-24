using System.Linq.Expressions;
using Campus.Attendance.Shared.Configuration;
using Campus.Attendance.Shared.Contracts;
using Campus.Attendance.Shared.Entities;
using Campus.Attendance.Shared.Features.Courses;
using Campus.Attendance.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Campus.Attendance.Shared.Constants.MessageConstants;

namespace Campus.Attendance.Api.Features.Courses;

/// <summary>
/// 课程与课表处理器
/// </summary>
public class CourseHandlers :
    IRequestHandler<GetCoursesQuery, ApiResponse<PagedResult<CourseResponseDto>>>,
    IRequestHandler<GetCourseByIdQuery, ApiResponse<CourseResponseDto>>,
    IRequestHandler<CreateCourseCommand, ApiResponse<CourseResponseDto>>,
    IRequestHandler<UpdateCourseCommand, ApiResponse<CourseResponseDto>>,
    IRequestHandler<DeleteCourseCommand, ApiResponse<object>>,
    IRequestHandler<GetCoursesByTeacherQuery, ApiResponse<List<CourseResponseDto>>>,
    IRequestHandler<GetSchedulesQuery, ApiResponse<PagedResult<ScheduleResponseDto>>>,
    IRequestHandler<GetScheduleByIdQuery, ApiResponse<ScheduleResponseDto>>,
    IRequestHandler<CreateScheduleCommand, ApiResponse<ScheduleResponseDto>>,
    IRequestHandler<UpdateScheduleCommand, ApiResponse<ScheduleResponseDto>>,
    IRequestHandler<DeleteScheduleCommand, ApiResponse<object>>,
    IRequestHandler<GetScheduleByTeacherQuery, ApiResponse<WeeklyScheduleDto>>,
    IRequestHandler<GetScheduleByStudentQuery, ApiResponse<WeeklyScheduleDto>>,
    IRequestHandler<GetScheduleByClassQuery, ApiResponse<WeeklyScheduleDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<CourseHandlers> _logger;

    /// <summary>课表多表联查的 Select 映射表达式</summary>
    private static readonly Expression<Func<CourseSchedule, Course, Class, Teacher, ScheduleResponseDto>> ScheduleSelector =
        (s, c, cls, t) => new ScheduleResponseDto
        {
            Id = s.Id, CourseId = s.CourseId, CourseName = c.Name,
            ClassId = s.ClassId, ClassName = cls.Name,
            TeacherId = s.TeacherId, TeacherName = t.Name,
            DayOfWeek = s.DayOfWeek, StartSection = s.StartSection, EndSection = s.EndSection,
            StartWeek = s.StartWeek, EndWeek = s.EndWeek, Classroom = s.Classroom
        };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public CourseHandlers(IDbContext dbContext, ILogger<CourseHandlers> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>分页查询课程</summary>
    public async Task<ApiResponse<PagedResult<CourseResponseDto>>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var q = db.Queryable<Course, Teacher>((c, t) => new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where((c, t) => c.Name.Contains(query.Keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.TeacherId))
        {
            q = q.Where((c, t) => c.TeacherId == query.TeacherId);
        }

        var total = await q.CountAsync();
        var rows = await q.Select((c, t) => new CourseResponseDto
        {
            Id = c.Id, Name = c.Name, TeacherId = c.TeacherId, TeacherName = t.Name,
            Credit = c.Credit, Remark = c.Remark
        }).OrderBy(c => c.Id).Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return ApiResponse<PagedResult<CourseResponseDto>>.Success(
            PagedResult<CourseResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
    }

    /// <summary>根据Id查询课程</summary>
    public async Task<ApiResponse<CourseResponseDto>> Handle(GetCourseByIdQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<Course, Teacher>((c, t) => new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => c.Id == query.Id && !c.IsDeleted)
            .Select((c, t) => new CourseResponseDto
            {
                Id = c.Id, Name = c.Name, TeacherId = c.TeacherId, TeacherName = t.Name,
                Credit = c.Credit, Remark = c.Remark
            }).FirstAsync();

        if (dto is null)
        {
            return ApiResponse<CourseResponseDto>.Fail(Msg.Common.EntityNotFound($"课程 {query.Id}"), 404);
        }

        return ApiResponse<CourseResponseDto>.Success(dto);
    }

    /// <summary>创建课程</summary>
    public async Task<ApiResponse<CourseResponseDto>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.TeacherId && !t.IsDeleted);
        if (!teacherExists)
        {
            return ApiResponse<CourseResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {command.Dto.TeacherId}"), 404);
        }

        var course = new Course
        {
            Name = command.Dto.Name, TeacherId = command.Dto.TeacherId,
            Credit = command.Dto.Credit, Remark = command.Dto.Remark, CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(course).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建课程 {CourseId}（教师 {TeacherId}）", id, command.Dto.TeacherId);
        return await Handle(new GetCourseByIdQuery(id), cancellationToken);
    }

    /// <summary>更新课程</summary>
    public async Task<ApiResponse<CourseResponseDto>> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var course = await db.Queryable<Course>().FirstAsync(c => c.Id == command.Id && !c.IsDeleted);
        if (course is null)
        {
            return ApiResponse<CourseResponseDto>.Fail(Msg.Common.EntityNotFound($"课程 {command.Id}"), 404);
        }

        var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.TeacherId && !t.IsDeleted);
        if (!teacherExists)
        {
            return ApiResponse<CourseResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {command.Dto.TeacherId}"), 404);
        }

        course.Name = command.Dto.Name;
        course.TeacherId = command.Dto.TeacherId;
        course.Credit = command.Dto.Credit;
        course.Remark = command.Dto.Remark;
        course.UpdateTime = DateTime.UtcNow;
        await db.Updateable(course).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新课程 {CourseId}", command.Id);
        return await Handle(new GetCourseByIdQuery(command.Id), cancellationToken);
    }

    /// <summary>删除课程</summary>
    public async Task<ApiResponse<object>> Handle(DeleteCourseCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var course = await db.Queryable<Course>().FirstAsync(c => c.Id == command.Id && !c.IsDeleted);
        if (course is null)
        {
            return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"课程 {command.Id}"), 404);
        }

        course.IsDeleted = true;
        course.UpdateTime = DateTime.UtcNow;
        await db.Updateable(course).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除课程 {CourseId}", command.Id);
        return ApiResponse<object>.Success( "删除成功");
    }

    /// <summary>按教师查询课程</summary>
    public async Task<ApiResponse<List<CourseResponseDto>>> Handle(GetCoursesByTeacherQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var rows = await db.Queryable<Course, Teacher>((c, t) => new JoinQueryInfos(JoinType.Left, c.TeacherId == t.Id))
            .Where((c, t) => !c.IsDeleted && c.TeacherId == query.TeacherId)
            .OrderBy((c, t) => c.Id)
            .Select((c, t) => new CourseResponseDto
            {
                Id = c.Id, Name = c.Name, TeacherId = c.TeacherId, TeacherName = t.Name,
                Credit = c.Credit, Remark = c.Remark
            }).ToListAsync();

        return ApiResponse<List<CourseResponseDto>>.Success(rows);
    }

    /// <summary>分页查询课表</summary>
    public async Task<ApiResponse<PagedResult<ScheduleResponseDto>>> Handle(GetSchedulesQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var q = db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => !s.IsDeleted);

        if (query.ClassId.HasValue)
        {
            q = q.Where((s, c, cls, t) => s.ClassId == query.ClassId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TeacherId))
        {
            q = q.Where((s, c, cls, t) => s.TeacherId == query.TeacherId);
        }

        if (query.CourseId.HasValue)
        {
            q = q.Where((s, c, cls, t) => s.CourseId == query.CourseId.Value);
        }

        var total = await q.CountAsync();
        var rows = await q.Select(ScheduleSelector).OrderBy(s => s.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return ApiResponse<PagedResult<ScheduleResponseDto>>.Success(
            PagedResult<ScheduleResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
    }

    /// <summary>根据Id查询课表</summary>
    public async Task<ApiResponse<ScheduleResponseDto>> Handle(GetScheduleByIdQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => s.Id == query.Id && !s.IsDeleted)
            .Select(ScheduleSelector).FirstAsync();

        if (dto is null)
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"课表 {query.Id}"), 404);
        }

        return ApiResponse<ScheduleResponseDto>.Success(dto);
    }

    /// <summary>创建课表</summary>
    public async Task<ApiResponse<ScheduleResponseDto>> Handle(CreateScheduleCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        if (!await db.Queryable<Course>().AnyAsync(c => c.Id == command.Dto.CourseId && !c.IsDeleted))
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"课程 {command.Dto.CourseId}"), 404);
        }

        if (!await db.Queryable<Class>().AnyAsync(c => c.Id == command.Dto.ClassId && !c.IsDeleted))
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"班级 {command.Dto.ClassId}"), 404);
        }

        if (!await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.TeacherId && !t.IsDeleted))
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {command.Dto.TeacherId}"), 404);
        }

        if (command.Dto.StartSection > command.Dto.EndSection)
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Course.StartSectionAfterEnd, 400);
        }

        if (command.Dto.StartWeek > command.Dto.EndWeek)
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Course.StartWeekAfterEnd, 400);
        }

        var schedule = new CourseSchedule
        {
            CourseId = command.Dto.CourseId, ClassId = command.Dto.ClassId, TeacherId = command.Dto.TeacherId,
            DayOfWeek = command.Dto.DayOfWeek, StartSection = command.Dto.StartSection, EndSection = command.Dto.EndSection,
            StartWeek = command.Dto.StartWeek, EndWeek = command.Dto.EndWeek, Classroom = command.Dto.Classroom,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(schedule).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建课表 {ScheduleId}", id);
        return await Handle(new GetScheduleByIdQuery(id), cancellationToken);
    }

    /// <summary>更新课表</summary>
    public async Task<ApiResponse<ScheduleResponseDto>> Handle(UpdateScheduleCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var schedule = await db.Queryable<CourseSchedule>().FirstAsync(s => s.Id == command.Id && !s.IsDeleted);
        if (schedule is null)
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"课表 {command.Id}"), 404);
        }

        if (!await db.Queryable<Course>().AnyAsync(c => c.Id == command.Dto.CourseId && !c.IsDeleted))
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"课程 {command.Dto.CourseId}"), 404);
        }

        if (!await db.Queryable<Class>().AnyAsync(c => c.Id == command.Dto.ClassId && !c.IsDeleted))
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"班级 {command.Dto.ClassId}"), 404);
        }

        if (!await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.TeacherId && !t.IsDeleted))
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {command.Dto.TeacherId}"), 404);
        }

        if (command.Dto.StartSection > command.Dto.EndSection)
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Course.StartSectionAfterEnd, 400);
        }

        if (command.Dto.StartWeek > command.Dto.EndWeek)
        {
            return ApiResponse<ScheduleResponseDto>.Fail(Msg.Course.StartWeekAfterEnd, 400);
        }

        schedule.CourseId = command.Dto.CourseId;
        schedule.ClassId = command.Dto.ClassId;
        schedule.TeacherId = command.Dto.TeacherId;
        schedule.DayOfWeek = command.Dto.DayOfWeek;
        schedule.StartSection = command.Dto.StartSection;
        schedule.EndSection = command.Dto.EndSection;
        schedule.StartWeek = command.Dto.StartWeek;
        schedule.EndWeek = command.Dto.EndWeek;
        schedule.Classroom = command.Dto.Classroom;
        schedule.UpdateTime = DateTime.UtcNow;
        await db.Updateable(schedule).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新课表 {ScheduleId}", command.Id);
        return await Handle(new GetScheduleByIdQuery(command.Id), cancellationToken);
    }

    /// <summary>删除课表</summary>
    public async Task<ApiResponse<object>> Handle(DeleteScheduleCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var schedule = await db.Queryable<CourseSchedule>().FirstAsync(s => s.Id == command.Id && !s.IsDeleted);
        if (schedule is null)
        {
            return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"课表 {command.Id}"), 404);
        }

        schedule.IsDeleted = true;
        schedule.UpdateTime = DateTime.UtcNow;
        await db.Updateable(schedule).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除课表 {ScheduleId}", command.Id);
        return ApiResponse<object>.Success( "删除成功");
    }

    /// <summary>按教师查询周课表</summary>
    public async Task<ApiResponse<WeeklyScheduleDto>> Handle(GetScheduleByTeacherQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var schedules = await db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => !s.IsDeleted && s.TeacherId == query.TeacherId && s.StartWeek <= query.Week && s.EndWeek >= query.Week)
            .Select(ScheduleSelector).OrderBy(s => s.DayOfWeek).OrderBy(s => s.StartSection).ToListAsync();

        return ApiResponse<WeeklyScheduleDto>.Success(BuildWeeklySchedule(query.Week, schedules));
    }

    /// <summary>按学生查询周课表</summary>
    public async Task<ApiResponse<WeeklyScheduleDto>> Handle(GetScheduleByStudentQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == query.StudentId && !s.IsDeleted);
        if (student is null)
        {
            return ApiResponse<WeeklyScheduleDto>.Fail(Msg.Common.EntityNotFound($"学生 {query.StudentId}"), 404);
        }

        return await Handle(new GetScheduleByClassQuery((int)student.ClassId, query.Week), cancellationToken);
    }

    /// <summary>按班级查询周课表</summary>
    public async Task<ApiResponse<WeeklyScheduleDto>> Handle(GetScheduleByClassQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var schedules = await db.Queryable<CourseSchedule, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => !s.IsDeleted && s.ClassId == query.ClassId && s.StartWeek <= query.Week && s.EndWeek >= query.Week)
            .Select(ScheduleSelector).OrderBy(s => s.DayOfWeek).OrderBy(s => s.StartSection).ToListAsync();

        return ApiResponse<WeeklyScheduleDto>.Success(BuildWeeklySchedule(query.Week, schedules));
    }

    /// <summary>将课表列表按 DayOfWeek 分组构造周课表 DTO</summary>
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
