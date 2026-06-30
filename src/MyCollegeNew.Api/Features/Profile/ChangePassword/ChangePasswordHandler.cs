using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;

using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Profile.ChangePassword
{
    /// <summary>
    /// 修改密码处理器，需校验旧密码
    /// </summary>
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<object>>
    {
        private readonly IDbContext _dbContext;
        private readonly IAuditService _auditService;
        private readonly ILogger<ChangePasswordHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="auditService">审计日志服务（M-5：记录密码修改）</param>
        /// <param name="logger">日志器</param>
        public ChangePasswordHandler(IDbContext dbContext, IAuditService auditService, ILogger<ChangePasswordHandler> logger)
        {
            _dbContext = dbContext;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// 处理修改密码命令
        /// </summary>
        public async Task<ApiResponse<object>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            string? currentHash;
            Func<Task> updateAction;

            switch (command.Role)
            {
                case UserRole.Admin:
                    {
                        // H-4 修复：旧实现按 u.Username == command.UserId 匹配，
                        // 但 command.UserId 来自 ICurrentUser.UserId（= SystemUser.Id 数字字符串），永不命中
                        // 现按 Id 匹配，command.UserId 即 SystemUser.Id 字符串化
                        // 兼容支持：若 command.UserId 可解析为 long，按 Id 查询；否则按 Username 查询（向后兼容旧 JWT）
                        var admin = long.TryParse(command.UserId, out var adminId)
                            ? await db.Queryable<SystemUser>().FirstAsync(u => u.Id == adminId && !u.IsDeleted, cancellationToken)
                            : await db.Queryable<SystemUser>().FirstAsync(u => u.Username == command.UserId && !u.IsDeleted, cancellationToken);
                        if (admin is null)
                        {
                            return ApiResponse<object>.Fail(Msg.Auth.UserNotFound, 404);
                        }

                        currentHash = admin.Password;
                        updateAction = () =>
                        {
                            admin.Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.NewPassword);
                            admin.UpdateTime = DateTime.UtcNow;
                            return db.Updateable(admin).UpdateColumns(it => new { it.Password, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);
                        };
                        break;
                    }

                case UserRole.Teacher:
                case UserRole.Counselor:
                    {
                        var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == command.UserId && !t.IsDeleted, cancellationToken);
                        if (teacher is null)
                        {
                            return ApiResponse<object>.Fail(Msg.Auth.UserNotFound, 404);
                        }

                        currentHash = teacher.Password;
                        updateAction = () =>
                        {
                            teacher.Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.NewPassword);
                            teacher.UpdateTime = DateTime.UtcNow;
                            return db.Updateable(teacher).UpdateColumns(it => new { it.Password, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);
                        };
                        break;
                    }

                case UserRole.Student:
                    {
                        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == command.UserId && !s.IsDeleted, cancellationToken);
                        if (student is null)
                        {
                            return ApiResponse<object>.Fail(Msg.Auth.UserNotFound, 404);
                        }

                        currentHash = student.Password;
                        updateAction = () =>
                        {
                            student.Password = BCrypt.Net.BCrypt.HashPassword(command.Dto.NewPassword);
                            student.UpdateTime = DateTime.UtcNow;
                            // L-2 修复：学生通过个人中心改密时同步清除强制改密标记
                            student.MustChangePassword = false;
                            return db.Updateable(student).UpdateColumns(it => new { it.Password, it.UpdateTime, it.MustChangePassword }).ExecuteCommandAsync(cancellationToken);
                        };
                        break;
                    }

                default:
                    return ApiResponse<object>.Fail(Msg.Auth.UnsupportedRole, 400);
            }

            if (!BCrypt.Net.BCrypt.Verify(command.Dto.OldPassword, currentHash))
            {
                return ApiResponse<object>.Fail(Msg.Auth.OldPasswordIncorrect, 400);
            }

            await updateAction();
            _logger.LogInformation("用户 {UserId} 修改密码成功", command.UserId);
            // M-5：审计日志记录密码修改（已认证场景，从 ICurrentUser 读取操作者）
            await _auditService.LogAsync("修改密码", command.UserId, cancellationToken);
            return ApiResponse<object>.Success("密码修改成功");
        }
    }
}