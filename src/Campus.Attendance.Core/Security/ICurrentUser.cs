using Campus.Attendance.Core.Enums;

namespace Campus.Attendance.Core.Security;

/// <summary>
/// 当前登录用户上下文接口，封装从 HTTP 请求中解析出的用户身份信息
/// </summary>
public interface ICurrentUser
{
    /// <summary>用户ID（学号/工号/admin）</summary>
    string UserId { get; }

    /// <summary>用户名/真实姓名</summary>
    string UserName { get; }

    /// <summary>用户角色</summary>
    UserRole Role { get; }

    /// <summary>是否已认证</summary>
    bool IsAuthenticated { get; }
}
