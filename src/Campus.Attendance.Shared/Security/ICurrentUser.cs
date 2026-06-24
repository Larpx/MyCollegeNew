using Campus.Attendance.Shared.Enums;

namespace Campus.Attendance.Shared.Security;

/// <summary>
/// 当前登录用户上下文接口
/// </summary>
public interface ICurrentUser
{
    /// <summary>用户ID</summary>
    string UserId { get; }

    /// <summary>用户名</summary>
    string UserName { get; }

    /// <summary>用户角色</summary>
    UserRole Role { get; }

    /// <summary>是否已认证</summary>
    bool IsAuthenticated { get; }
}
