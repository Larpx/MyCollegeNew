using Campus.Attendance.Shared.Enums;
using Campus.Attendance.Shared.Features.Users;
using Campus.Attendance.Shared.Responses;
using Campus.Attendance.Shared.Security;
using MediatR;

namespace Campus.Attendance.Api.Features.Profile.ChangePassword;

/// <summary>
/// 修改密码命令
/// </summary>
/// <param name="Dto">密码修改请求</param>
/// <param name="UserId">当前用户ID</param>
/// <param name="Role">当前用户角色</param>
public record ChangePasswordCommand(PasswordChangeDto Dto, string UserId, UserRole Role) : IRequest<ApiResponse<object>>;
