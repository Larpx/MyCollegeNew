using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Organization;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using SqlSugar;

using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Organization
{
    /// <summary>
    /// 组织架构相关处理器
    /// </summary>
    public class OrganizationHandlers :
        IRequestHandler<GetDepartmentsQuery, ApiResponse<List<DepartmentResponseDto>>>,
        IRequestHandler<GetDepartmentByIdQuery, ApiResponse<DepartmentResponseDto>>,
        IRequestHandler<CreateDepartmentCommand, ApiResponse<DepartmentResponseDto>>,
        IRequestHandler<UpdateDepartmentCommand, ApiResponse<DepartmentResponseDto>>,
        IRequestHandler<DeleteDepartmentCommand, ApiResponse<object>>,
        IRequestHandler<GetMajorsByDepartmentQuery, ApiResponse<List<MajorResponseDto>>>,
        IRequestHandler<GetMajorByIdQuery, ApiResponse<MajorResponseDto>>,
        IRequestHandler<CreateMajorCommand, ApiResponse<MajorResponseDto>>,
        IRequestHandler<UpdateMajorCommand, ApiResponse<MajorResponseDto>>,
        IRequestHandler<DeleteMajorCommand, ApiResponse<object>>,
        IRequestHandler<GetClassesByMajorQuery, ApiResponse<List<ClassResponseDto>>>,
        IRequestHandler<GetClassByIdQuery, ApiResponse<ClassResponseDto>>,
        IRequestHandler<CreateClassCommand, ApiResponse<ClassResponseDto>>,
        IRequestHandler<UpdateClassCommand, ApiResponse<ClassResponseDto>>,
        IRequestHandler<DeleteClassCommand, ApiResponse<object>>,
        IRequestHandler<GetOrganizationTreeQuery, ApiResponse<List<OrganizationTreeNodeDto>>>
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<OrganizationHandlers> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志器</param>
        public OrganizationHandlers(IDbContext dbContext, ILogger<OrganizationHandlers> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>查询所有院系</summary>
        public async Task<ApiResponse<List<DepartmentResponseDto>>> Handle(GetDepartmentsQuery _, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var departments = await db.Queryable<Department>().Where(d => !d.IsDeleted).OrderBy(d => d.Id).ToListAsync();
            if (departments.Count == 0)
            {
                return ApiResponse<List<DepartmentResponseDto>>.Success(new List<DepartmentResponseDto>());
            }

            var departmentIds = departments.Select(d => d.Id).ToList();
            var majorCounts = await db.Queryable<Major>()
                .Where(m => !m.IsDeleted && departmentIds.Contains(m.DepartmentId))
                .GroupBy(m => m.DepartmentId)
                .Select(m => new { m.DepartmentId, Count = SqlFunc.AggregateCount(m.Id) })
                .ToListAsync();
            var majorCountMap = majorCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

            var studentCounts = await db.Queryable<Student>()
                .Where(s => !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
                .GroupBy(s => s.DepartmentId)
                .Select(s => new { s.DepartmentId, Count = SqlFunc.AggregateCount(s.Id) })
                .ToListAsync();
            var studentCountMap = studentCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

            var result = departments.Select(d => new DepartmentResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                MajorCount = majorCountMap.GetValueOrDefault(d.Id, 0),
                StudentCount = studentCountMap.GetValueOrDefault(d.Id, 0)
            }).ToList();

            return ApiResponse<List<DepartmentResponseDto>>.Success(result);
        }

        /// <summary>根据Id查询院系</summary>
        public async Task<ApiResponse<DepartmentResponseDto>> Handle(GetDepartmentByIdQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var department = await db.Queryable<Department>().Where(d => d.Id == query.Id && !d.IsDeleted).FirstAsync();
            if (department is null)
            {
                return ApiResponse<DepartmentResponseDto>.Fail(Msg.Common.EntityNotFound($"院系 {query.Id}"), 404);
            }

            var majorCount = await db.Queryable<Major>().Where(m => !m.IsDeleted && m.DepartmentId == query.Id).CountAsync();
            var studentCount = await db.Queryable<Student>().Where(s => !s.IsDeleted && s.DepartmentId == query.Id).CountAsync();

            return ApiResponse<DepartmentResponseDto>.Success(new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                MajorCount = majorCount,
                StudentCount = studentCount
            });
        }

        /// <summary>创建院系</summary>
        public async Task<ApiResponse<DepartmentResponseDto>> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var department = new Department { Name = command.Dto.Name, CreateTime = DateTime.UtcNow };
            var id = await db.Insertable(department).ExecuteReturnIdentityAsync();
            _logger.LogInformation("创建院系 {DepartmentId} ({DepartmentName})", id, command.Dto.Name);
            return await Handle(new GetDepartmentByIdQuery(id), cancellationToken);
        }

        /// <summary>更新院系</summary>
        public async Task<ApiResponse<DepartmentResponseDto>> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var department = await db.Queryable<Department>().FirstAsync(d => d.Id == command.Id && !d.IsDeleted);
            if (department is null)
            {
                return ApiResponse<DepartmentResponseDto>.Fail(Msg.Common.EntityNotFound($"院系 {command.Id}"), 404);
            }

            department.Name = command.Dto.Name;
            department.UpdateTime = DateTime.UtcNow;
            await db.Updateable(department).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("更新院系 {DepartmentId}", command.Id);
            return await Handle(new GetDepartmentByIdQuery(command.Id), cancellationToken);
        }

        /// <summary>删除院系</summary>
        public async Task<ApiResponse<object>> Handle(DeleteDepartmentCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var department = await db.Queryable<Department>().FirstAsync(d => d.Id == command.Id && !d.IsDeleted);
            if (department is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"院系 {command.Id}"), 404);
            }

            var hasMajors = await db.Queryable<Major>().AnyAsync(m => m.DepartmentId == command.Id && !m.IsDeleted);
            if (hasMajors)
            {
                return ApiResponse<object>.Fail(Msg.Organization.DepartmentHasMajors(command.Id), 400);
            }

            department.IsDeleted = true;
            department.UpdateTime = DateTime.UtcNow;
            await db.Updateable(department).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("软删除院系 {DepartmentId}", command.Id);
            return ApiResponse<object>.Success("删除成功");
        }

        /// <summary>按院系查询专业</summary>
        public async Task<ApiResponse<List<MajorResponseDto>>> Handle(GetMajorsByDepartmentQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var majors = await db.Queryable<Major, Department>((m, d) =>
                    new JoinQueryInfos(JoinType.Left, m.DepartmentId == d.Id))
                .Where((m, d) => !m.IsDeleted && m.DepartmentId == query.DepartmentId)
                .OrderBy((m, d) => m.Id)
                .Select((m, d) => new MajorResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    DepartmentId = m.DepartmentId,
                    DepartmentName = d.Name
                })
                .ToListAsync();

            if (majors.Count > 0)
            {
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
            }

            return ApiResponse<List<MajorResponseDto>>.Success(majors);
        }

        /// <summary>根据Id查询专业</summary>
        public async Task<ApiResponse<MajorResponseDto>> Handle(GetMajorByIdQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var major = await db.Queryable<Major, Department>((m, d) =>
                    new JoinQueryInfos(JoinType.Left, m.DepartmentId == d.Id))
                .Where((m, d) => m.Id == query.Id && !m.IsDeleted)
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
                return ApiResponse<MajorResponseDto>.Fail(Msg.Common.EntityNotFound($"专业 {query.Id}"), 404);
            }

            major.ClassCount = await db.Queryable<Class>().Where(c => !c.IsDeleted && c.MajorId == query.Id).CountAsync();
            return ApiResponse<MajorResponseDto>.Success(major);
        }

        /// <summary>创建专业</summary>
        public async Task<ApiResponse<MajorResponseDto>> Handle(CreateMajorCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var departmentExists = await db.Queryable<Department>().AnyAsync(d => d.Id == command.Dto.DepartmentId && !d.IsDeleted);
            if (!departmentExists)
            {
                return ApiResponse<MajorResponseDto>.Fail(Msg.Common.EntityNotFound($"院系 {command.Dto.DepartmentId}"), 404);
            }

            var major = new Major { Name = command.Dto.Name, DepartmentId = command.Dto.DepartmentId, CreateTime = DateTime.UtcNow };
            var id = await db.Insertable(major).ExecuteReturnIdentityAsync();
            _logger.LogInformation("创建专业 {MajorId}（院系 {DepartmentId}）", id, command.Dto.DepartmentId);
            return await Handle(new GetMajorByIdQuery(id), cancellationToken);
        }

        /// <summary>更新专业</summary>
        public async Task<ApiResponse<MajorResponseDto>> Handle(UpdateMajorCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var major = await db.Queryable<Major>().FirstAsync(m => m.Id == command.Id && !m.IsDeleted);
            if (major is null)
            {
                return ApiResponse<MajorResponseDto>.Fail(Msg.Common.EntityNotFound($"专业 {command.Id}"), 404);
            }

            var departmentExists = await db.Queryable<Department>().AnyAsync(d => d.Id == command.Dto.DepartmentId && !d.IsDeleted);
            if (!departmentExists)
            {
                return ApiResponse<MajorResponseDto>.Fail(Msg.Common.EntityNotFound($"院系 {command.Dto.DepartmentId}"), 404);
            }

            major.Name = command.Dto.Name;
            major.DepartmentId = command.Dto.DepartmentId;
            major.UpdateTime = DateTime.UtcNow;
            await db.Updateable(major).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("更新专业 {MajorId}", command.Id);
            return await Handle(new GetMajorByIdQuery(command.Id), cancellationToken);
        }

        /// <summary>删除专业</summary>
        public async Task<ApiResponse<object>> Handle(DeleteMajorCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var major = await db.Queryable<Major>().FirstAsync(m => m.Id == command.Id && !m.IsDeleted);
            if (major is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"专业 {command.Id}"), 404);
            }

            var hasClasses = await db.Queryable<Class>().AnyAsync(c => c.MajorId == command.Id && !c.IsDeleted);
            if (hasClasses)
            {
                return ApiResponse<object>.Fail(Msg.Organization.MajorHasClasses(command.Id), 400);
            }

            major.IsDeleted = true;
            major.UpdateTime = DateTime.UtcNow;
            await db.Updateable(major).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("软删除专业 {MajorId}", command.Id);
            return ApiResponse<object>.Success("删除成功");
        }

        /// <summary>按专业查询班级</summary>
        public async Task<ApiResponse<List<ClassResponseDto>>> Handle(GetClassesByMajorQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var classes = await db.Queryable<Class, Major, Teacher>((c, m, t) =>
                    new JoinQueryInfos(JoinType.Left, c.MajorId == m.Id, JoinType.Left, c.CounselorId == t.Id))
                .Where((c, m, t) => !c.IsDeleted && c.MajorId == query.MajorId)
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

            if (classes.Count > 0)
            {
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
            }

            return ApiResponse<List<ClassResponseDto>>.Success(classes);
        }

        /// <summary>根据Id查询班级</summary>
        public async Task<ApiResponse<ClassResponseDto>> Handle(GetClassByIdQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var cls = await db.Queryable<Class, Major, Teacher>((c, m, t) =>
                    new JoinQueryInfos(JoinType.Left, c.MajorId == m.Id, JoinType.Left, c.CounselorId == t.Id))
                .Where((c, m, t) => c.Id == query.Id && !c.IsDeleted)
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
                return ApiResponse<ClassResponseDto>.Fail(Msg.Common.EntityNotFound($"班级 {query.Id}"), 404);
            }

            cls.StudentCount = await db.Queryable<Student>().Where(s => !s.IsDeleted && s.ClassId == query.Id).CountAsync();
            return ApiResponse<ClassResponseDto>.Success(cls);
        }

        /// <summary>创建班级</summary>
        public async Task<ApiResponse<ClassResponseDto>> Handle(CreateClassCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var majorExists = await db.Queryable<Major>().AnyAsync(m => m.Id == command.Dto.MajorId && !m.IsDeleted);
            if (!majorExists)
            {
                return ApiResponse<ClassResponseDto>.Fail(Msg.Common.EntityNotFound($"专业 {command.Dto.MajorId}"), 404);
            }

            var counselorExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.CounselorId && !t.IsDeleted);
            if (!counselorExists)
            {
                return ApiResponse<ClassResponseDto>.Fail(Msg.Common.EntityNotFound($"辅导员 {command.Dto.CounselorId}"), 404);
            }

            var entity = new Class
            {
                Name = command.Dto.Name,
                MajorId = command.Dto.MajorId,
                Grade = command.Dto.Grade,
                CounselorId = command.Dto.CounselorId,
                CreateTime = DateTime.UtcNow
            };
            var id = await db.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("创建班级 {ClassId}（专业 {MajorId}）", id, command.Dto.MajorId);
            return await Handle(new GetClassByIdQuery(id), cancellationToken);
        }

        /// <summary>更新班级</summary>
        public async Task<ApiResponse<ClassResponseDto>> Handle(UpdateClassCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var entity = await db.Queryable<Class>().FirstAsync(c => c.Id == command.Id && !c.IsDeleted);
            if (entity is null)
            {
                return ApiResponse<ClassResponseDto>.Fail(Msg.Common.EntityNotFound($"班级 {command.Id}"), 404);
            }

            var majorExists = await db.Queryable<Major>().AnyAsync(m => m.Id == command.Dto.MajorId && !m.IsDeleted);
            if (!majorExists)
            {
                return ApiResponse<ClassResponseDto>.Fail(Msg.Common.EntityNotFound($"专业 {command.Dto.MajorId}"), 404);
            }

            var counselorExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == command.Dto.CounselorId && !t.IsDeleted);
            if (!counselorExists)
            {
                return ApiResponse<ClassResponseDto>.Fail(Msg.Common.EntityNotFound($"辅导员 {command.Dto.CounselorId}"), 404);
            }

            entity.Name = command.Dto.Name;
            entity.MajorId = command.Dto.MajorId;
            entity.Grade = command.Dto.Grade;
            entity.CounselorId = command.Dto.CounselorId;
            entity.UpdateTime = DateTime.UtcNow;
            await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("更新班级 {ClassId}", command.Id);
            return await Handle(new GetClassByIdQuery(command.Id), cancellationToken);
        }

        /// <summary>删除班级</summary>
        public async Task<ApiResponse<object>> Handle(DeleteClassCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var entity = await db.Queryable<Class>().FirstAsync(c => c.Id == command.Id && !c.IsDeleted);
            if (entity is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"班级 {command.Id}"), 404);
            }

            entity.IsDeleted = true;
            entity.UpdateTime = DateTime.UtcNow;
            await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("软删除班级 {ClassId}", command.Id);
            return ApiResponse<object>.Success("删除成功");
        }

        /// <summary>查询组织树</summary>
        public async Task<ApiResponse<List<OrganizationTreeNodeDto>>> Handle(GetOrganizationTreeQuery _, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var departments = await db.Queryable<Department>().Where(d => !d.IsDeleted).OrderBy(d => d.Id).ToListAsync();
            if (departments.Count == 0)
            {
                return ApiResponse<List<OrganizationTreeNodeDto>>.Success(new List<OrganizationTreeNodeDto>());
            }

            var departmentIds = departments.Select(d => d.Id).ToList();
            var majors = await db.Queryable<Major>().Where(m => !m.IsDeleted && departmentIds.Contains(m.DepartmentId)).OrderBy(m => m.Id).ToListAsync();
            var majorIds = majors.Select(m => m.Id).ToList();
            var classes = majorIds.Count > 0
                ? await db.Queryable<Class>().Where(c => !c.IsDeleted && majorIds.Contains(c.MajorId)).OrderBy(c => c.Id).ToListAsync()
                : new List<Class>();

            var counselorIds = classes.Select(c => c.CounselorId).Distinct().ToList();
            var counselorNameMap = new Dictionary<string, string>();
            if (counselorIds.Count > 0)
            {
                var counselors = await db.Queryable<Teacher>().Where(t => counselorIds.Contains(t.Id)).Select(t => new { t.Id, t.Name }).ToListAsync();
                counselorNameMap = counselors.ToDictionary(x => x.Id, x => x.Name);
            }

            var departmentStudentCounts = await db.Queryable<Student>()
                .Where(s => !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
                .GroupBy(s => s.DepartmentId)
                .Select(s => new { s.DepartmentId, Count = SqlFunc.AggregateCount(s.Id) })
                .ToListAsync();
            var departmentStudentMap = departmentStudentCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

            var classStudentCounts = majorIds.Count > 0
                ? await db.Queryable<Student>()
                    .Where(s => !s.IsDeleted && majorIds.Contains(s.MajorId))
                    .GroupBy(s => s.ClassId)
                    .Select(s => new { s.ClassId, Count = SqlFunc.AggregateCount(s.Id) })
                    .ToListAsync()
                : [];
            var classStudentMap = classStudentCounts.Count > 0
                ? classStudentCounts.ToDictionary(x => x.ClassId, x => x.Count)
                : new Dictionary<long, int>();

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

            return ApiResponse<List<OrganizationTreeNodeDto>>.Success(tree);
        }
    }
}