using System.Globalization;
using System.Text;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Students
{
/// <summary>
/// 学生相关查询与命令处理器
/// </summary>
public class StudentHandlers :
    IRequestHandler<GetStudentsQuery, ApiResponse<PagedResult<StudentResponseDto>>>,
    IRequestHandler<GetStudentByIdQuery, ApiResponse<StudentResponseDto>>,
    IRequestHandler<CreateStudentCommand, ApiResponse<StudentResponseDto>>,
    IRequestHandler<UpdateStudentCommand, ApiResponse<StudentResponseDto>>,
    IRequestHandler<DeleteStudentCommand, ApiResponse<object>>,
    IRequestHandler<BatchImportStudentsCommand, ApiResponse<BatchImportResultDto>>
{
    /// <summary>CSV 导入默认密码取学号后 6 位</summary>
    private const int DefaultPasswordTailLength = 6;

    /// <summary>CSV 表头字段数</summary>
    private const int CsvColumnCount = 7;

    private readonly IDbContext _dbContext;
    private readonly ILogger<StudentHandlers> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public StudentHandlers(IDbContext dbContext, ILogger<StudentHandlers> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>分页查询学生列表</summary>
    public async Task<ApiResponse<PagedResult<StudentResponseDto>>> Handle(GetStudentsQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var q = db.Queryable<Student, Department, Major, Class>((s, d, m, c) =>
                new JoinQueryInfos(
                    JoinType.Left, s.DepartmentId == d.Id,
                    JoinType.Left, s.MajorId == m.Id,
                    JoinType.Left, s.ClassId == c.Id))
            .Where((s, d, m, c) => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where((s, d, m, c) => s.Id.Contains(query.Keyword) || s.Name.Contains(query.Keyword));
        }

        if (query.ClassId.HasValue)
        {
            q = q.Where((s, d, m, c) => s.ClassId == query.ClassId.Value);
        }

        if (query.MajorId.HasValue)
        {
            q = q.Where((s, d, m, c) => s.MajorId == query.MajorId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            q = q.Where((s, d, m, c) => s.DepartmentId == query.DepartmentId.Value);
        }

        var total = await q.CountAsync();
        var rows = await q
            .Select<StudentResponseDto>((s, d, m, c) => new StudentResponseDto
            {
                Id = s.Id, Name = s.Name, Gender = s.Gender,
                DepartmentName = d.Name, MajorName = m.Name, ClassName = c.Name,
                Grade = s.Grade, Status = s.Status, Remark = s.Remark
            })
            .OrderBy(it => it.Id)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiResponse<PagedResult<StudentResponseDto>>.Success(
            PagedResult<StudentResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
    }

    /// <summary>根据学号查询学生</summary>
    public async Task<ApiResponse<StudentResponseDto>> Handle(GetStudentByIdQuery query, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<Student, Department, Major, Class>((s, d, m, c) =>
                new JoinQueryInfos(
                    JoinType.Left, s.DepartmentId == d.Id,
                    JoinType.Left, s.MajorId == m.Id,
                    JoinType.Left, s.ClassId == c.Id))
            .Where((s, d, m, c) => s.Id == query.Id && !s.IsDeleted)
            .Select<StudentResponseDto>((s, d, m, c) => new StudentResponseDto
            {
                Id = s.Id, Name = s.Name, Gender = s.Gender,
                DepartmentName = d.Name, MajorName = m.Name, ClassName = c.Name,
                Grade = s.Grade, Status = s.Status, Remark = s.Remark
            })
            .FirstAsync();

        if (dto is null)
        {
            return ApiResponse<StudentResponseDto>.Fail(Msg.Common.EntityNotFound($"学生 {query.Id}"), 404);
        }

        return ApiResponse<StudentResponseDto>.Success(dto);
    }

    /// <summary>创建学生</summary>
    public async Task<ApiResponse<StudentResponseDto>> Handle(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var exists = await db.Queryable<Student>().AnyAsync(s => s.Id == command.Dto.Id);
        if (exists)
        {
            return ApiResponse<StudentResponseDto>.Fail(Msg.User.StudentIdExists(command.Dto.Id), 400);
        }

        var student = new Student
        {
            Id = command.Dto.Id,
            Name = command.Dto.Name,
            Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.Password),
            Gender = command.Dto.Gender,
            DepartmentId = command.Dto.DepartmentId,
            MajorId = command.Dto.MajorId,
            ClassId = command.Dto.ClassId,
            Grade = command.Dto.Grade,
            Status = 0,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(student).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("创建学生 {StudentId}", student.Id);

        // 重新查询以获取关联名称
        return await Handle(new GetStudentByIdQuery(student.Id), cancellationToken);
    }

    /// <summary>更新学生</summary>
    public async Task<ApiResponse<StudentResponseDto>> Handle(UpdateStudentCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == command.Id && !s.IsDeleted);
        if (student is null)
        {
            return ApiResponse<StudentResponseDto>.Fail(Msg.Common.EntityNotFound($"学生 {command.Id}"), 404);
        }

        student.Name = command.Dto.Name;
        student.Gender = command.Dto.Gender;
        student.DepartmentId = command.Dto.DepartmentId;
        student.MajorId = command.Dto.MajorId;
        student.ClassId = command.Dto.ClassId;
        student.Grade = command.Dto.Grade;
        student.Status = command.Dto.Status;
        student.Remark = command.Dto.Remark;
        student.UpdateTime = DateTime.UtcNow;

        await db.Updateable(student).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新学生 {StudentId}", student.Id);

        return await Handle(new GetStudentByIdQuery(student.Id), cancellationToken);
    }

    /// <summary>删除学生</summary>
    public async Task<ApiResponse<object>> Handle(DeleteStudentCommand command, CancellationToken cancellationToken)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == command.Id && !s.IsDeleted);
        if (student is null)
        {
            return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"学生 {command.Id}"), 404);
        }

        student.IsDeleted = true;
        student.UpdateTime = DateTime.UtcNow;
        await db.Updateable(student).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除学生 {StudentId}", student.Id);

        return ApiResponse<object>.Success( "删除成功");
    }

    /// <summary>批量导入学生</summary>
    public async Task<ApiResponse<BatchImportResultDto>> Handle(BatchImportStudentsCommand command, CancellationToken cancellationToken)
    {
        var result = new BatchImportResultDto();
        var db = _dbContext.Client;

        using var reader = new StreamReader(command.CsvStream, Encoding.UTF8);
        var lineNumber = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            lineNumber++;
            if (lineNumber == 1 || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var fields = line.Split(',');
                if (fields.Length < CsvColumnCount)
                {
                    result.FailedCount++;
                    result.Failures.Add(new BatchImportFailureItem
                    {
                        Row = lineNumber,
                        Reason = Msg.User.CsvColumnInsufficient(CsvColumnCount)
                    });
                    continue;
                }

                var id = fields[0].Trim();
                var name = fields[1].Trim();
                var gender = fields[2].Trim();
                var departmentId = long.Parse(fields[3].Trim(), CultureInfo.InvariantCulture);
                var majorId = long.Parse(fields[4].Trim(), CultureInfo.InvariantCulture);
                var classId = long.Parse(fields[5].Trim(), CultureInfo.InvariantCulture);
                var grade = int.Parse(fields[6].Trim(), CultureInfo.InvariantCulture);

                if (await db.Queryable<Student>().AnyAsync(s => s.Id == id))
                {
                    result.FailedCount++;
                    result.Failures.Add(new BatchImportFailureItem
                    {
                        Row = lineNumber,
                        Reason = Msg.User.StudentIdExists(id)
                    });
                    continue;
                }

                var defaultPassword = id.Length >= DefaultPasswordTailLength
                    ? id[^DefaultPasswordTailLength..]
                    : id;

                var student = new Student
                {
                    Id = id, Name = name,
                    Password = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                    Gender = gender, DepartmentId = departmentId, MajorId = majorId,
                    ClassId = classId, Grade = grade, Status = 0,
                    CreateTime = DateTime.UtcNow
                };
                await db.Insertable(student).ExecuteCommandAsync(cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Failures.Add(new BatchImportFailureItem { Row = lineNumber, Reason = ex.Message });
                _logger.LogWarning(ex, "CSV 导入第 {Row} 行失败", lineNumber);
            }
        }

        _logger.LogInformation("CSV 批量导入完成：成功 {Success}，失败 {Failed}", result.SuccessCount, result.FailedCount);
        return ApiResponse<BatchImportResultDto>.Success(result);
    }
}
}