namespace Campus.Attendance.Core.Enums;

/// <summary>
/// 考勤会话状态枚举，标识考勤会话的生命周期阶段
/// </summary>
public enum SessionStatus
{
    /// <summary>进行中，允许学生签到</summary>
    Active,

    /// <summary>已关闭，停止签到</summary>
    Closed
}
