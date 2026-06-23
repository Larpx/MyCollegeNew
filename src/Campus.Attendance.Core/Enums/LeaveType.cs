namespace Campus.Attendance.Core.Enums;

/// <summary>
/// 请假类型枚举，区分请假事由
/// </summary>
public enum LeaveType
{
    /// <summary>病假</summary>
    Sick,

    /// <summary>事假</summary>
    Personal,

    /// <summary>公假（学校官方活动）</summary>
    Official,

    /// <summary>其他事由</summary>
    Other
}
