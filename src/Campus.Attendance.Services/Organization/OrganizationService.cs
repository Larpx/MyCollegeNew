using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Models.Organization;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Campus.Attendance.Services.Organization;

/// <summary>
/// 组织架构管理服务实现，封装院系、专业、班级的增删改查与树形结构查询
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<OrganizationService> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public OrganizationService(IDbContext dbContext, ILogger<OrganizationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 查询所有院系（过滤软删除），并统计专业数与学生数
    /// </summary>
    public async Task<List<DepartmentResponseDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var departments = await db.Queryable<Department>()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Id)
            .ToListAsync();

        if (departments.Count == 0)
        {
            return new List<DepartmentResponseDto>();
        }

        var departmentIds = departments.Select(d => d.Id).ToList();
        var majorCounts = await db.Queryable<Major>()
            .Where(m => !m.IsDeleted && departmentIds.Contains(m.DepartmentId))
            .GroupBy(m => m.DepartmentId)
            .Select(m => new { m.DepartmentId, Count = SqlFunc.AggregateCount(m.Id) })
            .ToListAsync();
        var majorCountMap = majorCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

        // 学生按院系统计
        var studentCounts = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
            .GroupBy(s => s.DepartmentId)
            .Select(s => new { s.DepartmentId, Count = SqlFunc.AggregateCount(s.Id) })
            .ToListAsync();
        var studentCountMap = studentCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

        return departments.Select(d => new DepartmentResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            MajorCount = majorCountMap.GetValueOrDefault(d.Id, 0),
            StudentCount = studentCountMap.GetValueOrDefault(d.Id, 0)
        }).ToList();
    }

    /// <summary>
    /// 根据 Id 查询单个院系
    /// </summary>
    public async Task<DepartmentResponseDto?> GetDepartmentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var department = await db.Queryable<Department>()
            .Where(d => d.Id == id && !d.IsDeleted)
            .FirstAsync();

        if (department is null)
        {
            return null;
        }

        var majorCount = await db.Queryable<Major>()
            .Where(m => !m.IsDeleted && m.DepartmentId == id)
            .CountAsync();
        var studentCount = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && s.DepartmentId == id)
            .CountAsync();

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            MajorCount = majorCount,
            StudentCount = studentCount
        };
    }

    /// <summary>
    /// 创建院系
    /// </summary>
    public async Task<DepartmentResponseDto> CreateDepartmentAsync(DepartmentCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var department = new Department
        {
            Name = dto.Name,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(department).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建院系 {DepartmentId} ({DepartmentName})", id, dto.Name);

        return (await GetDepartmentByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 更新院系
    /// </summary>
    public async Task<DepartmentResponseDto> UpdateDepartmentAsync(long id, DepartmentUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var department = await db.Queryable<Department>().FirstAsync(d => d.Id == id && !d.IsDeleted);
        if (department is null)
        {
            throw new BusinessException($"院系 {id} 不存在", 404);
        }

        department.Name = dto.Name;
        department.UpdateTime = DateTime.UtcNow;
        await db.Updateable(department).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新院系 {DepartmentId}", id);

        return (await GetDepartmentByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除院系，删除前检查是否有关联专业
    /// </summary>
    public async Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var department = await db.Queryable<Department>().FirstAsync(d => d.Id == id && !d.IsDeleted);
        if (department is null)
        {
            throw new BusinessException($"院系 {id} 不存在", 404);
        }

        var hasMajors = await db.Queryable<Major>().AnyAsync(m => m.DepartmentId == id && !m.IsDeleted);
        if (hasMajors)
        {
            throw new BusinessException($"院系 {id} 下存在专业，无法删除", 400);
        }

        department.IsDeleted = true;
        department.UpdateTime = DateTime.UtcNow;
        await db.Updateable(department).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除院系 {DepartmentId}", id);
    }

    /// <summary>
    /// 按院系查询专业列表
    /// </summary>
    public async Task<List<MajorResponseDto>> GetMajorsByDepartmentAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var majors = await db.Queryable<Major, Department>((m, d) =>
                new JoinQueryInfos(JoinType.Left, m.DepartmentId == d.Id))
            .Where((m, d) => !m.IsDeleted && m.DepartmentId == departmentId)
            .OrderBy((m, d) => m.Id)
            .Select((m, d) => new MajorResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                DepartmentId = m.DepartmentId,
                DepartmentName = d.Name
            })
            .ToListAsync();

        if (majors.Count == 0)
        {
            return majors;
        }

        var majorIds = majors.Select(m => m.Id).ToList();
        var classCounts = await db.Queryable<Class>()
            .Where(c => !c.IsDeleted && majorIds.Contains(c.MajorId))
            .GroupBy(c => c.MajorId)
            .Select(c => new { c.MajorId, Count = SqlFunc.AggregateCount(c.Id) })
            .ToListAsync();
        var classCountMap = classCounts.ToDictionary(x => x.MajorId, x => x.Count);

        foreach (var major in majors)
        {
            major.ClassCount = classCountMap.GetValueOrDefault(major.Id, 0);
        }

        return majors;
    }

    /// <summary>
    /// 根据 Id 查询单个专业
    /// </summary>
    public async Task<MajorResponseDto?> GetMajorByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var major = await db.Queryable<Major, Department>((m, d) =>
                new JoinQueryInfos(JoinType.Left, m.DepartmentId == d.Id))
            .Where((m, d) => m.Id == id && !m.IsDeleted)
            .Select((m, d) => new MajorResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                DepartmentId = m.DepartmentId,
                DepartmentName = d.Name
            })
            .FirstAsync();

        if (major is null)
        {
            return null;
        }

        major.ClassCount = await db.Queryable<Class>()
            .Where(c => !c.IsDeleted && c.MajorId == id)
            .CountAsync();

        return major;
    }

    /// <summary>
    /// 创建专业，需校验所属院系存在
    /// </summary>
    public async Task<MajorResponseDto> CreateMajorAsync(MajorCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var departmentExists = await db.Queryable<Department>().AnyAsync(d => d.Id == dto.DepartmentId && !d.IsDeleted);
        if (!departmentExists)
        {
            throw new BusinessException($"院系 {dto.DepartmentId} 不存在", 404);
        }

        var major = new Major
        {
            Name = dto.Name,
            DepartmentId = dto.DepartmentId,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(major).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建专业 {MajorId}（院系 {DepartmentId}）", id, dto.DepartmentId);

        return (await GetMajorByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 更新专业
    /// </summary>
    public async Task<MajorResponseDto> UpdateMajorAsync(long id, MajorUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var major = await db.Queryable<Major>().FirstAsync(m => m.Id == id && !m.IsDeleted);
        if (major is null)
        {
            throw new BusinessException($"专业 {id} 不存在", 404);
        }

        var departmentExists = await db.Queryable<Department>().AnyAsync(d => d.Id == dto.DepartmentId && !d.IsDeleted);
        if (!departmentExists)
        {
            throw new BusinessException($"院系 {dto.DepartmentId} 不存在", 404);
        }

        major.Name = dto.Name;
        major.DepartmentId = dto.DepartmentId;
        major.UpdateTime = DateTime.UtcNow;
        await db.Updateable(major).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新专业 {MajorId}", id);

        return (await GetMajorByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除专业，删除前检查是否有关联班级
    /// </summary>
    public async Task DeleteMajorAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var major = await db.Queryable<Major>().FirstAsync(m => m.Id == id && !m.IsDeleted);
        if (major is null)
        {
            throw new BusinessException($"专业 {id} 不存在", 404);
        }

        var hasClasses = await db.Queryable<Class>().AnyAsync(c => c.MajorId == id && !c.IsDeleted);
        if (hasClasses)
        {
            throw new BusinessException($"专业 {id} 下存在班级，无法删除", 400);
        }

        major.IsDeleted = true;
        major.UpdateTime = DateTime.UtcNow;
        await db.Updateable(major).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除专业 {MajorId}", id);
    }

    /// <summary>
    /// 按专业查询班级列表
    /// </summary>
    public async Task<List<ClassResponseDto>> GetClassesByMajorAsync(long majorId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var classes = await db.Queryable<Class, Major, Teacher>((c, m, t) =>
                new JoinQueryInfos(
                    JoinType.Left, c.MajorId == m.Id,
                    JoinType.Left, c.CounselorId == t.Id))
            .Where((c, m, t) => !c.IsDeleted && c.MajorId == majorId)
            .OrderBy((c, m, t) => c.Id)
            .Select((c, m, t) => new ClassResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                MajorId = c.MajorId,
                MajorName = m.Name,
                Grade = c.Grade,
                CounselorId = c.CounselorId,
                CounselorName = t.Name
            })
            .ToListAsync();

        if (classes.Count == 0)
        {
            return classes;
        }

        var classIds = classes.Select(c => c.Id).ToList();
        var studentCounts = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && classIds.Contains(s.ClassId))
            .GroupBy(s => s.ClassId)
            .Select(s => new { s.ClassId, Count = SqlFunc.AggregateCount(s.Id) })
            .ToListAsync();
        var studentCountMap = studentCounts.ToDictionary(x => x.ClassId, x => x.Count);

        foreach (var cls in classes)
        {
            cls.StudentCount = studentCountMap.GetValueOrDefault(cls.Id, 0);
        }

        return classes;
    }

    /// <summary>
    /// 根据 Id 查询单个班级
    /// </summary>
    public async Task<ClassResponseDto?> GetClassByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var cls = await db.Queryable<Class, Major, Teacher>((c, m, t) =>
                new JoinQueryInfos(
                    JoinType.Left, c.MajorId == m.Id,
                    JoinType.Left, c.CounselorId == t.Id))
            .Where((c, m, t) => c.Id == id && !c.IsDeleted)
            .Select((c, m, t) => new ClassResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                MajorId = c.MajorId,
                MajorName = m.Name,
                Grade = c.Grade,
                CounselorId = c.CounselorId,
                CounselorName = t.Name
            })
            .FirstAsync();

        if (cls is null)
        {
            return null;
        }

        cls.StudentCount = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && s.ClassId == id)
            .CountAsync();

        return cls;
    }

    /// <summary>
    /// 创建班级，需校验所属专业与辅导员存在
    /// </summary>
    public async Task<ClassResponseDto> CreateClassAsync(ClassCreateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var majorExists = await db.Queryable<Major>().AnyAsync(m => m.Id == dto.MajorId && !m.IsDeleted);
        if (!majorExists)
        {
            throw new BusinessException($"专业 {dto.MajorId} 不存在", 404);
        }

        var counselorExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.CounselorId && !t.IsDeleted);
        if (!counselorExists)
        {
            throw new BusinessException($"辅导员 {dto.CounselorId} 不存在", 404);
        }

        var entity = new Class
        {
            Name = dto.Name,
            MajorId = dto.MajorId,
            Grade = dto.Grade,
            CounselorId = dto.CounselorId,
            CreateTime = DateTime.UtcNow
        };
        var id = await db.Insertable(entity).ExecuteReturnIdentityAsync();
        _logger.LogInformation("创建班级 {ClassId}（专业 {MajorId}）", id, dto.MajorId);

        return (await GetClassByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 更新班级
    /// </summary>
    public async Task<ClassResponseDto> UpdateClassAsync(long id, ClassUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var entity = await db.Queryable<Class>().FirstAsync(c => c.Id == id && !c.IsDeleted);
        if (entity is null)
        {
            throw new BusinessException($"班级 {id} 不存在", 404);
        }

        var majorExists = await db.Queryable<Major>().AnyAsync(m => m.Id == dto.MajorId && !m.IsDeleted);
        if (!majorExists)
        {
            throw new BusinessException($"专业 {dto.MajorId} 不存在", 404);
        }

        var counselorExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == dto.CounselorId && !t.IsDeleted);
        if (!counselorExists)
        {
            throw new BusinessException($"辅导员 {dto.CounselorId} 不存在", 404);
        }

        entity.Name = dto.Name;
        entity.MajorId = dto.MajorId;
        entity.Grade = dto.Grade;
        entity.CounselorId = dto.CounselorId;
        entity.UpdateTime = DateTime.UtcNow;
        await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("更新班级 {ClassId}", id);

        return (await GetClassByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// 软删除班级
    /// </summary>
    public async Task DeleteClassAsync(long id, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var entity = await db.Queryable<Class>().FirstAsync(c => c.Id == id && !c.IsDeleted);
        if (entity is null)
        {
            throw new BusinessException($"班级 {id} 不存在", 404);
        }

        entity.IsDeleted = true;
        entity.UpdateTime = DateTime.UtcNow;
        await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("软删除班级 {ClassId}", id);
    }

    /// <summary>
    /// 返回院系→专业→班级三级树形结构
    /// </summary>
    public async Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync(CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 一次性加载所有未删除的院系、专业、班级，减少数据库往返
        var departments = await db.Queryable<Department>()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Id)
            .ToListAsync();
        if (departments.Count == 0)
        {
            return new List<OrganizationTreeNodeDto>();
        }

        var departmentIds = departments.Select(d => d.Id).ToList();
        var majors = await db.Queryable<Major>()
            .Where(m => !m.IsDeleted && departmentIds.Contains(m.DepartmentId))
            .OrderBy(m => m.Id)
            .ToListAsync();
        var majorIds = majors.Select(m => m.Id).ToList();

        var classes = await db.Queryable<Class>()
            .Where(c => !c.IsDeleted && majorIds.Contains(c.MajorId))
            .OrderBy(c => c.Id)
            .ToListAsync();

        // 查询辅导员姓名映射（按需查询涉及的工号）
        var counselorIds = classes.Select(c => c.CounselorId).Distinct().ToList();
        var counselorNameMap = new Dictionary<string, string>();
        if (counselorIds.Count > 0)
        {
            var counselors = await db.Queryable<Teacher>()
                .Where(t => counselorIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();
            counselorNameMap = counselors.ToDictionary(x => x.Id, x => x.Name);
        }

        // 统计学生数（按院系、按班级）
        var departmentStudentCounts = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
            .GroupBy(s => s.DepartmentId)
            .Select(s => new { s.DepartmentId, Count = SqlFunc.AggregateCount(s.Id) })
            .ToListAsync();
        var departmentStudentMap = departmentStudentCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

        var classStudentCounts = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && majorIds.Contains(s.MajorId))
            .GroupBy(s => s.ClassId)
            .Select(s => new { s.ClassId, Count = SqlFunc.AggregateCount(s.Id) })
            .ToListAsync();
        var classStudentMap = classStudentCounts.ToDictionary(x => x.ClassId, x => x.Count);

        // 组装树形结构
        var majorsByDepartment = majors.GroupBy(m => m.DepartmentId).ToDictionary(g => g.Key, g => g.ToList());
        var classesByMajor = classes.GroupBy(c => c.MajorId).ToDictionary(g => g.Key, g => g.ToList());

        var tree = new List<OrganizationTreeNodeDto>();
        foreach (var department in departments)
        {
            var deptMajors = majorsByDepartment.GetValueOrDefault(department.Id, new List<Major>());
            var node = new OrganizationTreeNodeDto
            {
                Department = new DepartmentResponseDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    MajorCount = deptMajors.Count,
                    StudentCount = departmentStudentMap.GetValueOrDefault(department.Id, 0)
                }
            };

            foreach (var major in deptMajors)
            {
                var majorClasses = classesByMajor.GetValueOrDefault(major.Id, new List<Class>());
                var classDtos = majorClasses.Select(cls => new ClassResponseDto
                {
                    Id = cls.Id,
                    Name = cls.Name,
                    MajorId = cls.MajorId,
                    MajorName = major.Name,
                    Grade = cls.Grade,
                    CounselorId = cls.CounselorId,
                    CounselorName = counselorNameMap.GetValueOrDefault(cls.CounselorId, string.Empty),
                    StudentCount = classStudentMap.GetValueOrDefault(cls.Id, 0)
                }).ToList();

                node.Majors.Add(new MajorTreeNodeDto
                {
                    Major = new MajorResponseDto
                    {
                        Id = major.Id,
                        Name = major.Name,
                        DepartmentId = major.DepartmentId,
                        DepartmentName = department.Name,
                        ClassCount = classDtos.Count
                    },
                    Classes = classDtos
                });
            }

            tree.Add(node);
        }

        return tree;
    }
}
