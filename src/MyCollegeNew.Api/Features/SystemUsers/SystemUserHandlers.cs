using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using SqlSugar;

using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemUsers
{
    /// <summary>
    /// 系统用户相关查询与命令处理器
    /// </summary>
    public class SystemUserHandlers :
        IRequestHandler<GetSystemUsersQuery, ApiResponse<PagedResult<SystemUserResponseDto>>>,
        IRequestHandler<GetSystemUserByIdQuery, ApiResponse<SystemUserResponseDto>>,
        IRequestHandler<CreateSystemUserCommand, ApiResponse<SystemUserResponseDto>>,
        IRequestHandler<UpdateSystemUserCommand, ApiResponse<SystemUserResponseDto>>,
        IRequestHandler<DeleteSystemUserCommand, ApiResponse<object>>,
        IRequestHandler<ResetSystemUserPasswordCommand, ApiResponse<object>>
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<SystemUserHandlers> _logger;
        private readonly ICurrentUser _currentUser;
        private readonly IAuditService _auditService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志器</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="auditService">审计日志服务（M-5：记录用户增删/重置密码）</param>
        public SystemUserHandlers(IDbContext dbContext, ILogger<SystemUserHandlers> logger, ICurrentUser currentUser, IAuditService auditService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUser = currentUser;
            _auditService = auditService;
        }

        /// <summary>分页查询系统用户列表</summary>
        public async Task<ApiResponse<PagedResult<SystemUserResponseDto>>> Handle(GetSystemUsersQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var q = db.Queryable<SystemUser>().Where(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                q = q.Where(u => u.Username.Contains(query.Keyword) || u.RealName.Contains(query.Keyword));
            }

            var total = await q.CountAsync();
            // 注意：OrderBy 必须在 Select 之前调用
            var rows = await q
                .OrderBy(u => u.Id, OrderByType.Desc)
                .Select(u => new SystemUserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    RealName = u.RealName,
                    Role = u.Role,
                    HasTwoFactor = !string.IsNullOrEmpty(u.TwoFactorSecret),
                    CreateTime = u.CreateTime,
                    UpdateTime = u.UpdateTime
                })
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResult<SystemUserResponseDto>>.Success(
                PagedResult<SystemUserResponseDto>.Create(rows, total, query.PageIndex, query.PageSize));
        }

        /// <summary>根据 Id 查询系统用户</summary>
        public async Task<ApiResponse<SystemUserResponseDto>> Handle(GetSystemUserByIdQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var dto = await db.Queryable<SystemUser>()
                .Where(u => u.Id == query.Id && !u.IsDeleted)
                .Select(u => new SystemUserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    RealName = u.RealName,
                    Role = u.Role,
                    HasTwoFactor = !string.IsNullOrEmpty(u.TwoFactorSecret),
                    CreateTime = u.CreateTime,
                    UpdateTime = u.UpdateTime
                })
                .FirstAsync();

            if (dto is null)
            {
                return ApiResponse<SystemUserResponseDto>.Fail(Msg.Common.EntityNotFound($"系统用户 {query.Id}"), 404);
            }

            return ApiResponse<SystemUserResponseDto>.Success(dto);
        }

        /// <summary>创建系统用户</summary>
        public async Task<ApiResponse<SystemUserResponseDto>> Handle(CreateSystemUserCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            // 用户名唯一性校验需包含已删除记录，避免删除后无法重建同名账号造成歧义
            var exists = await db.Queryable<SystemUser>().AnyAsync(u => u.Username == command.Dto.Username);
            if (exists)
            {
                return ApiResponse<SystemUserResponseDto>.Fail($"用户名 {command.Dto.Username} 已存在", 400);
            }

            var user = new SystemUser
            {
                Username = command.Dto.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.Password),
                Role = command.Dto.Role,
                RealName = command.Dto.RealName,
                TwoFactorSecret = null,
                CreateTime = DateTime.UtcNow
            };
            await db.Insertable(user).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("创建系统用户 {Username}", user.Username);
            // M-5：审计日志记录创建系统用户
            await _auditService.LogAsync("创建系统用户", $"username={user.Username},role={user.Role}", cancellationToken);

            return await Handle(new GetSystemUserByIdQuery(user.Id), cancellationToken);
        }

        /// <summary>更新系统用户</summary>
        public async Task<ApiResponse<SystemUserResponseDto>> Handle(UpdateSystemUserCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var user = await db.Queryable<SystemUser>().FirstAsync(u => u.Id == command.Id && !u.IsDeleted);
            if (user is null)
            {
                return ApiResponse<SystemUserResponseDto>.Fail(Msg.Common.EntityNotFound($"系统用户 {command.Id}"), 404);
            }

            user.RealName = command.Dto.RealName;
            user.Role = command.Dto.Role;
            user.UpdateTime = DateTime.UtcNow;

            await db.Updateable(user).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("更新系统用户 {UserId}", user.Id);

            return await Handle(new GetSystemUserByIdQuery(user.Id), cancellationToken);
        }

        /// <summary>删除系统用户（软删除）</summary>
        public async Task<ApiResponse<object>> Handle(DeleteSystemUserCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var user = await db.Queryable<SystemUser>().FirstAsync(u => u.Id == command.Id && !u.IsDeleted);
            if (user is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"系统用户 {command.Id}"), 404);
            }

            // 不允许删除当前登录账号（H-3 修复）：
            // 旧实现误将 user.Username 与 _currentUser.UserId 比较（前者是用户名，后者是 SystemUser.Id 字符串），恒为 false
            // 现通过 ICurrentUser.SystemUserId（仅 Admin 角色有值，对应 SystemUser.Id）正确比较
            if (_currentUser.SystemUserId.HasValue && user.Id == _currentUser.SystemUserId.Value)
            {
                return ApiResponse<object>.Fail("不可删除当前登录账号", 400);
            }

            user.IsDeleted = true;
            user.UpdateTime = DateTime.UtcNow;
            await db.Updateable(user).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("软删除系统用户 {UserId}", user.Id);
            // M-5：审计日志记录删除系统用户
            await _auditService.LogAsync("删除系统用户", $"id={user.Id},username={user.Username}", cancellationToken);

            return ApiResponse<object>.Success("删除成功");
        }

        /// <summary>重置系统用户密码</summary>
        public async Task<ApiResponse<object>> Handle(ResetSystemUserPasswordCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var user = await db.Queryable<SystemUser>().FirstAsync(u => u.Id == command.Id && !u.IsDeleted);
            if (user is null)
            {
                return ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"系统用户 {command.Id}"), 404);
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.NewPassword);
            // 重置密码后清空二次验证密钥，强制用户重新绑定 2FA
            user.TwoFactorSecret = null;
            user.UpdateTime = DateTime.UtcNow;
            await db.Updateable(user).ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("重置系统用户密码 {UserId}", user.Id);
            // M-5：审计日志记录重置用户密码（管理员重置他人密码属高危操作）
            await _auditService.LogAsync("重置系统用户密码", $"id={user.Id},username={user.Username}", cancellationToken);

            return ApiResponse<object>.Success("密码重置成功");
        }
    }
}
