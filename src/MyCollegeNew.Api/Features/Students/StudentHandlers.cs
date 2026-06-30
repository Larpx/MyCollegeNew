using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using SqlSugar;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        /// <summary>L-2 修复：CSV 导入随机初始密码长度（12 位，含大小写字母与数字）</summary>
        private const int RandomPasswordLength = 12;

        /// <summary>CSV 表头字段数</summary>
        private const int CsvColumnCount = 7;

        private readonly IDbContext _dbContext;
        private readonly IAuditService _auditService;
        private readonly ILogger<StudentHandlers> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="auditService">审计日志服务（M-5：记录批量导入）</param>
        /// <param name="logger">日志器</param>
        public StudentHandlers(IDbContext dbContext, IAuditService auditService, ILogger<StudentHandlers> logger)
        {
            _dbContext = dbContext;
            _auditService = auditService;
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
                .OrderBy((s, d, m, c) => s.Id)
                .Select<StudentResponseDto>((s, d, m, c) => new StudentResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Gender = s.Gender,
                    DepartmentId = s.DepartmentId,
                    MajorId = s.MajorId,
                    ClassId = s.ClassId,
                    DepartmentName = d.Name,
                    MajorName = m.Name,
                    ClassName = c.Name,
                    Grade = s.Grade,
                    Status = s.Status,
                    Remark = s.Remark
                })
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
                    Id = s.Id,
                    Name = s.Name,
                    Gender = s.Gender,
                    DepartmentId = s.DepartmentId,
                    MajorId = s.MajorId,
                    ClassId = s.ClassId,
                    DepartmentName = d.Name,
                    MajorName = m.Name,
                    ClassName = c.Name,
                    Grade = s.Grade,
                    Status = s.Status,
                    Remark = s.Remark
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

            return ApiResponse<object>.Success("删除成功");
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

                    // L-2 修复：不再使用学号后 6 位作为默认密码，改为生成随机初始密码
                    // 同时标记 MustChangePassword = true，强制学生首次登录修改密码
                    var initialPassword = GenerateRandomPassword();

                    var student = new Student
                    {
                        Id = id,
                        Name = name,
                        Password = BCrypt.Net.BCrypt.HashPassword(initialPassword),
                        Gender = gender,
                        DepartmentId = departmentId,
                        MajorId = majorId,
                        ClassId = classId,
                        Grade = grade,
                        Status = 0,
                        MustChangePassword = true,
                        CreateTime = DateTime.UtcNow
                    };
                    await db.Insertable(student).ExecuteCommandAsync(cancellationToken);
                    result.SuccessCount++;
                    // 将明文密码回传给管理员，由管理员通过安全渠道下发给学生
                    result.GeneratedPasswords.Add(new BatchImportPasswordItem
                    {
                        Id = id,
                        Name = name,
                        Password = initialPassword
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    // M-6 修复：不将 ex.Message 返回客户端（可能包含表名/列名/约束名等内部实现细节）
                    // 仅返回通用错误提示，详细信息保留在服务端日志中便于运维排查
                    result.Failures.Add(new BatchImportFailureItem { Row = lineNumber, Reason = $"第 {lineNumber} 行数据格式错误或已存在" });
                    _logger.LogWarning(ex, "CSV 导入第 {Row} 行失败", lineNumber);
                }
            }

            _logger.LogInformation("CSV 批量导入完成：成功 {Success}，失败 {Failed}", result.SuccessCount, result.FailedCount);
            // M-5：审计日志记录批量导入结果
            await _auditService.LogAsync("批量导入学生", $"成功{result.SuccessCount}条,失败{result.FailedCount}条", cancellationToken);
            return ApiResponse<BatchImportResultDto>.Success(result);
        }

        /// <summary>
        /// L-2 修复：生成密码学安全的随机初始密码（大小写字母 + 数字，共 12 位）
        /// 使用 RandomNumberGenerator 避免弱随机数导致的可预测性
        /// </summary>
        /// <returns>随机初始密码明文</returns>
        private static string GenerateRandomPassword()
        {
            // 字符集：大写字母 + 小写字母 + 数字
            const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            var allChars = upperCase + lowerCase + digits;

            // 至少各取 1 个大写、1 个小写、1 个数字，满足 L-1 密码复杂度要求
            Span<char> buffer = stackalloc char[RandomPasswordLength];
            buffer[0] = upperCase[RandomNumberGenerator.GetInt32(upperCase.Length)];
            buffer[1] = lowerCase[RandomNumberGenerator.GetInt32(lowerCase.Length)];
            buffer[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

            // 剩余位置从全字符集随机抽取
            for (var i = 3; i < RandomPasswordLength; i++)
            {
                buffer[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
            }

            // Fisher-Yates 洗牌避免前三位固定为大小写+数字
            for (var i = RandomPasswordLength - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }

            return new string(buffer);
        }
    }
}