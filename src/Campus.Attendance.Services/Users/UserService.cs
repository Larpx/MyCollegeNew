using System.Globalization;
using System.Text;
using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Users;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Campus.Attendance.Services.Users;

/// <summary>
/// 用户管理服务实现，封装学生与教师的增删改查、批量导入与密码修改
/// </summary>
public class UserService : IUserService
{
    /// <summary>CSV 导入默认密码取学号后 6 位</summary>
    private const int DefaultPasswordTailLength = 6;

    /// <summary>CSV 表头字段数（学号、姓名、性别、院系、专业、班级、年级）</summary>
    private const int CsvColumnCount = 7;

    private readonly IDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public UserService(IDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询学生列表，支持关键字、班级、专业、院系过滤
    /// </summary>
    public async Task<PagedResult<StudentResponseDto>> GetStudentsAsync(
        int pageIndex, int pageSize, string? keyword = null,
        long? classId = null, long? majorId = null, long? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var query = db.Queryable<Student, Department, Major, Class>((s, d, m, c) =>
                new JoinQueryInfos(
                    JoinType.Left, s.DepartmentId == d.Id,
                    JoinType.Left, s.MajorId == m.Id,
                    JoinType.Left, s.ClassId == c.Id))
            .Where((s, d, m, c) => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where((s, d, m, c) => s.Id.Contains(keyword) || s.Name.Contains(keyword));
        }

        if (classId.HasValue)
        {
            query = query.Where((s, d, m, c) => s.ClassId == classId.Value);
        }

        if (majorId.HasValue)
        {
            query = query.Where((s, d, m, c) => s.MajorId == majorId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where((s, d, m, c) => s.DepartmentId == departmentId.Value);
        }

        var total = await query.CountAsync();
        var rows = await query
            .Select<StudentResponseDto>((s, d, m, c) => new StudentResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Gender = s.Gender,
                DepartmentName = d.Name,
                MajorName = m.Name,
                ClassName = c.Name,
                Grade = s.Grade,
                Status = s.Status,
                Remark = s.Remark
            })
            .OrderBy(it => it.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<StudentResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 根据学号查询单个学生
    /// </summary>
    public async Task<StudentResponseDto?> GetStudentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<Student, Department, Major, Class>((s, d, m, c) =>
                new JoinQueryInfos(
                    JoinType.Left, s.DepartmentId == d.Id,
                    JoinType.Left, s.MajorId == m.Id,
                    JoinType.Left, s.ClassId == c.Id))
            .Where((s, d, m, c) => s.Id == id && !s.IsDeleted)
            .Select<StudentResponseDto>((s, d, m, c) => new StudentResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Gender = s.Gender,
                DepartmentName = d.Name,
                MajorName = m.Name,
                ClassName = c.Name,
                Grade = s.Grade,
                Status = s.Status,
                Remark = s.Remark
            })
            .FirstAsync();

        return dto;
    }

    /// <summary>
    /// 创建学生，密码使用 BCrypt 哈希
    /// </summary>
    public async Task<StudentResponseDto> CreateStudentAsync(StudentCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var exists = await db.Queryable<Student>().AnyAsync(s => s.Id == dto.Id);
        if (exists)
        {
            throw new BusinessException($"学号 {dto.Id} 已存在", 400);
        }

        var student = new Student
        {
            Id = dto.Id,
            Name = dto.Name,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Gender = dto.Gender,
            DepartmentId = dto.DepartmentId,
            MajorId = dto.MajorId,
            ClassId = dto.ClassId,
            Grade = dto.Grade,
            Status = 0,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(student).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("创建学生 {StudentId}", student.Id);

        return (await GetStudentByIdAsync(student.Id, cancellationToken))!;
    }

    /// <summary>
    /// 更新学生信息
    /// </summary>
    public async Task<StudentResponseDto> UpdateStudentAsync(string id, StudentUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == id && !s.IsDeleted);
        if (student is null)
        {
            throw new BusinessException($"学生 {id} 不存在", 404);
        }

        student.Name = dto.Name;
        student.Gender = dto.Gender;
        student.DepartmentId = dto.DepartmentId;
        student.MajorId = dto.MajorId;
        student.ClassId = dto.ClassId;
        student.Grade = dto.Grade;
        student.Status = dto.Status;
        student.Remark = dto.Remark;
        student.UpdateTime = DateTime.UtcNow;

        await db.Updateable(student).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新学生 {StudentId}", student.Id);

        return (await GetStudentByIdAsync(student.Id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除学生（IsDeleted=true）
    /// </summary>
    public async Task DeleteStudentAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == id && !s.IsDeleted);
        if (student is null)
        {
            throw new BusinessException($"学生 {id} 不存在", 404);
        }

        student.IsDeleted = true;
        student.UpdateTime = DateTime.UtcNow;
        await db.Updateable(student).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除学生 {StudentId}", student.Id);
    }

    /// <summary>
    /// 从 CSV 流批量导入学生，默认密码为学号后 6 位
    /// </summary>
    public async Task<BatchImportResultDto> BatchImportStudentsAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var result = new BatchImportResultDto();
        var db = _dbContext.Client;

        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var lineNumber = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            lineNumber++;
            // 跳过表头
            if (lineNumber == 1)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
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
                        Reason = $"字段数不足，期望 {CsvColumnCount} 个"
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
                        Reason = $"学号 {id} 已存在"
                    });
                    continue;
                }

                // 默认密码取学号后 6 位
                var defaultPassword = id.Length >= DefaultPasswordTailLength
                    ? id[^DefaultPasswordTailLength..]
                    : id;

                var student = new Student
                {
                    Id = id,
                    Name = name,
                    Password = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                    Gender = gender,
                    DepartmentId = departmentId,
                    MajorId = majorId,
                    ClassId = classId,
                    Grade = grade,
                    Status = 0,
                    CreateTime = DateTime.UtcNow
                };
                await db.Insertable(student).ExecuteCommandAsync(cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Failures.Add(new BatchImportFailureItem
                {
                    Row = lineNumber,
                    Reason = ex.Message
                });
                _logger.LogWarning(ex, "CSV 导入第 {Row} 行失败", lineNumber);
            }
        }

        _logger.LogInformation("CSV 批量导入完成：成功 {Success}，失败 {Failed}",
            result.SuccessCount, result.FailedCount);
        return result;
    }

    /// <summary>
    /// 分页查询教师列表，支持角色过滤
    /// </summary>
    public async Task<PagedResult<TeacherResponseDto>> GetTeachersAsync(
        int pageIndex, int pageSize, string? keyword = null,
        TeacherRole? role = null, long? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var query = db.Queryable<Teacher, Department, Major>((t, d, m) =>
                new JoinQueryInfos(
                    JoinType.Left, t.DepartmentId == d.Id,
                    JoinType.Left, t.MajorId == m.Id))
            .Where((t, d, m) => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where((t, d, m) => t.Id.Contains(keyword) || t.Name.Contains(keyword));
        }

        if (role.HasValue)
        {
            query = query.Where((t, d, m) => t.Role == role.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where((t, d, m) => t.DepartmentId == departmentId.Value);
        }

        var total = await query.CountAsync();
        var rows = await query
            .Select<TeacherResponseDto>((t, d, m) => new TeacherResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Gender = t.Gender,
                DepartmentName = d.Name,
                MajorName = m.Name,
                Role = t.Role,
                Remark = t.Remark
            })
            .OrderBy(it => it.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<TeacherResponseDto>.Create(rows, total, pageIndex, pageSize);
    }

    /// <summary>
    /// 根据工号查询单个教师
    /// </summary>
    public async Task<TeacherResponseDto?> GetTeacherByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var dto = await db.Queryable<Teacher, Department, Major>((t, d, m) =>
                new JoinQueryInfos(
                    JoinType.Left, t.DepartmentId == d.Id,
                    JoinType.Left, t.MajorId == m.Id))
            .Where((t, d, m) => t.Id == id && !t.IsDeleted)
            .Select<TeacherResponseDto>((t, d, m) => new TeacherResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Gender = t.Gender,
                DepartmentName = d.Name,
                MajorName = m.Name,
                Role = t.Role,
                Remark = t.Remark
            })
            .FirstAsync();

        return dto;
    }

    /// <summary>
    /// 创建教师，密码使用 BCrypt 哈希
    /// </summary>
    public async Task<TeacherResponseDto> CreateTeacherAsync(TeacherCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var exists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.Id);
        if (exists)
        {
            throw new BusinessException($"工号 {dto.Id} 已存在", 400);
        }

        var teacher = new Teacher
        {
            Id = dto.Id,
            Name = dto.Name,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Gender = dto.Gender,
            DepartmentId = dto.DepartmentId,
            MajorId = dto.MajorId,
            Role = dto.Role,
            CreateTime = DateTime.UtcNow
        };
        await db.Insertable(teacher).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("创建教师 {TeacherId}", teacher.Id);

        return (await GetTeacherByIdAsync(teacher.Id, cancellationToken))!;
    }

    /// <summary>
    /// 更新教师信息
    /// </summary>
    public async Task<TeacherResponseDto> UpdateTeacherAsync(string id, TeacherUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == id && !t.IsDeleted);
        if (teacher is null)
        {
            throw new BusinessException($"教师 {id} 不存在", 404);
        }

        teacher.Name = dto.Name;
        teacher.Gender = dto.Gender;
        teacher.DepartmentId = dto.DepartmentId;
        teacher.MajorId = dto.MajorId;
        teacher.Role = dto.Role;
        teacher.Remark = dto.Remark;
        teacher.UpdateTime = DateTime.UtcNow;

        await db.Updateable(teacher).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新教师 {TeacherId}", teacher.Id);

        return (await GetTeacherByIdAsync(teacher.Id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除教师
    /// </summary>
    public async Task DeleteTeacherAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == id && !t.IsDeleted);
        if (teacher is null)
        {
            throw new BusinessException($"教师 {id} 不存在", 404);
        }

        teacher.IsDeleted = true;
        teacher.UpdateTime = DateTime.UtcNow;
        await db.Updateable(teacher).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除教师 {TeacherId}", teacher.Id);
    }

    /// <summary>
    /// 修改密码，需校验旧密码
    /// </summary>
    public async Task ChangePasswordAsync(string userId, UserRole role, PasswordChangeDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        string? currentHash;
        Action updateAction;

        switch (role)
        {
            case UserRole.Admin:
            {
                // 管理员 UserId 为用户名（如 admin），按 Username 查找
                var admin = await db.Queryable<SystemUser>().FirstAsync(u => u.Username == userId && !u.IsDeleted);
                if (admin is null)
                {
                    throw new BusinessException("用户不存在", 404);
                }

                currentHash = admin.Password;
                updateAction = () =>
                {
                    admin.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    admin.UpdateTime = DateTime.UtcNow;
                    db.Updateable(admin).UpdateColumns(it => new { it.Password, it.UpdateTime }).ExecuteCommand();
                };
                break;
            }

            case UserRole.Teacher:
            case UserRole.Counselor:
            {
                var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == userId && !t.IsDeleted);
                if (teacher is null)
                {
                    throw new BusinessException("用户不存在", 404);
                }

                currentHash = teacher.Password;
                updateAction = () =>
                {
                    teacher.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    teacher.UpdateTime = DateTime.UtcNow;
                    db.Updateable(teacher).UpdateColumns(it => new { it.Password, it.UpdateTime }).ExecuteCommand();
                };
                break;
            }

            case UserRole.Student:
            {
                var student = await db.Queryable<Student>().FirstAsync(s => s.Id == userId && !s.IsDeleted);
                if (student is null)
                {
                    throw new BusinessException("用户不存在", 404);
                }

                currentHash = student.Password;
                updateAction = () =>
                {
                    student.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    student.UpdateTime = DateTime.UtcNow;
                    db.Updateable(student).UpdateColumns(it => new { it.Password, it.UpdateTime }).ExecuteCommand();
                };
                break;
            }

            default:
                throw new BusinessException("不支持的用户角色", 400);
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, currentHash))
        {
            throw new BusinessException("旧密码不正确", 400);
        }

        updateAction();
        _logger.LogInformation("用户 {UserId} 修改密码成功", userId);
    }
}
