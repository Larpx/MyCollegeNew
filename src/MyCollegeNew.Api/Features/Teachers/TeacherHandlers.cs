using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers;

/// <summary>
/// 教师相关查询与命令处理器
/// </summary>
public class TeacherHandlers :
    IRequestHandler<GetTeachersQuery, ApiResponse<PagedResult<TeacherResponseDto>>>,
    IRequestHandler<GetTeacherByIdQuery, ApiResponse<TeacherResponseDto>>,
    IRequestHandler<CreateTeacherCommand, ApiResponse<TeacherResponseDto>>,
    IRequestHandler<UpdateTeacherCommand, ApiResponse<TeacherResponseDto>>,
    IRequestHandler<DeleteTeacherCommand, ApiResponse<object>>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<TeacherHandlers> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public TeacherHandlers(IDbContext dbContext, ILogger<TeacherHandlers> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>分页查询教师列表</summary>
    public async Task<ApiResponse<PagedResult<TeacherResponseDto>>> Handle(GetTeachersQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var q = db.Queryable<Teacher, Department, Major>((t, d, m) =>
                new JoinQueryInfos(
                    JoinType.Left, t.DepartmentId == d.Id,
                    JoinType.Left, t.MajorId == m.Id))
            .Where((t, d, m) => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where((t, d, m) => t.Id.Contains(query.Keyword) || t.Name.Contains(query.Keyword));
        }

        if (query.Role.HasValue)
        {
            q = q.Where((t, d, m) => t.Role == query.Role.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            q = q.Where((t, d, m) => t.DepartmentId == query.DepartmentId.Value);
        }

        var total = await q.CountAsync();
        var rows = await q
            .Select<TeacherResponseDto>((t, d, m) => new TeacherResponseDto
            {
                Id = t.Id, Name = t.Name, Gender = t.Gender,
                DepartmentName = d.Name, MajorName = m.Name,
                Role = t.Role, Remark = t.Remark
            })
            .OrderBy(it => it.Id)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiResponse<PagedResult<TeacherResponseDto>>.Success(
            PagedResult<TeacherResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
    }

    /// <summary>根据工号查询教师</summary>
    public async Task<ApiResponse<TeacherResponseDto>> Handle(GetTeacherByIdQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<Teacher, Department, Major>((t, d, m) =>
                new JoinQueryInfos(
                    JoinType.Left, t.DepartmentId == d.Id,
                    JoinType.Left, t.MajorId == m.Id))
            .Where((t, d, m) => t.Id == query.Id && !t.IsDeleted)
            .Select<TeacherResponseDto>((t, d, m) => new TeacherResponseDto
            {
                Id = t.Id, Name = t.Name, Gender = t.Gender,
                DepartmentName = d.Name, MajorName = m.Name,
                Role = t.Role, Remark = t.Remark
            })
            .FirstAsync();

        if (dto is null)
        {
            return ApiResponse<TeacherResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {query.Id}"), 404);
        }

        return ApiResponse<TeacherResponseDto>.Success(dto);
    }

    /// <summary>创建教师</summary>
    public async Task<ApiResponse<TeacherResponseDto>> Handle(CreateTeacherCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var exists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.Id);
        if (exists)
        {
            return ApiResponse<TeacherResponseDto>.Fail(Msg.User.TeacherIdExists(command.Dto.Id), 400);
        }

        var teacher = new Teacher
        {
            Id = command.Dto.Id,
            Name = command.Dto.Name,
            Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.Password),
            Gender = command.Dto.Gender,
            DepartmentId = command.Dto.DepartmentId,
            MajorId = command.Dto.MajorId,
            Role = command.Dto.Role,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(teacher).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("创建教师 {TeacherId}", teacher.Id);

        return await Handle(new GetTeacherByIdQuery(teacher.Id), cancellationToken);
    }

    /// <summary>更新教师</summary>
    public async Task<ApiResponse<TeacherResponseDto>> Handle(UpdateTeacherCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == command.Id && !t.IsDeleted);
        if (teacher is null)
        {
            return ApiResponse<TeacherResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {command.Id}"), 404);
        }

        teacher.Name = command.Dto.Name;
        teacher.Gender = command.Dto.Gender;
        teacher.DepartmentId = command.Dto.DepartmentId;
        teacher.MajorId = command.Dto.MajorId;
        teacher.Role = command.Dto.Role;
        teacher.Remark = command.Dto.Remark;
        teacher.UpdateTime = DateTime.UtcNow;

        await db.Updateable(teacher).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新教师 {TeacherId}", teacher.Id);

        return await Handle(new GetTeacherByIdQuery(teacher.Id), cancellationToken);
    }

    /// <summary>删除教师</summary>
    public async Task<ApiResponse<object>> Handle(DeleteTeacherCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == command.Id && !t.IsDeleted);
        if (teacher is null)
        {
            return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"教师 {command.Id}"), 404);
        }

        teacher.IsDeleted = true;
        teacher.UpdateTime = DateTime.UtcNow;
        await db.Updateable(teacher).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除教师 {TeacherId}", teacher.Id);

        return ApiResponse<object>.Success( "删除成功");
    }
}
