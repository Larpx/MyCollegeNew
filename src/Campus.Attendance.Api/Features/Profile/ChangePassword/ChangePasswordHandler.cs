using Campus.Attendance.Shared.Configuration;
using Campus.Attendance.Shared.Entities;
using Campus.Attendance.Shared.Enums;
using Campus.Attendance.Shared.Responses;
using Campus.Attendance.Shared.Security;
using MediatR;
using Microsoft.Extensions.Logging;

using Msg = Campus.Attendance.Shared.Constants.MessageConstants;

namespace Campus.Attendance.Api.Features.Profile.ChangePassword;

/// <summary>
/// 修改密码处理器，需校验旧密码
/// </summary>
public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<object>>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<ChangePasswordHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public ChangePasswordHandler(IDbContext dbContext, ILogger<ChangePasswordHandler> logger)
    {
        _dbContext = dbContext;
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
                var admin = await db.Queryable<SystemUser>().FirstAsync(u => u.Username == command.UserId && !u.IsDeleted, cancellationToken);
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
                    return db.Updateable(student).UpdateColumns(it => new { it.Password, it.UpdateTime }).ExecuteCommandAsync(cancellationToken);
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
        return ApiResponse<object>.Success( "密码修改成功");
    }
}
