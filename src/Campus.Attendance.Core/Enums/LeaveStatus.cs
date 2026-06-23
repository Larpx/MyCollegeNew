namespace Campus.Attendance.Core.Enums;

/// <summary>
/// 请假审批状态枚举，标识请假申请的审批流转阶段
/// </summary>
public enum LeaveStatus
{
    /// <summary>待审批</summary>
    Pending,

    /// <summary>已通过</summary>
    Approved,

    /// <summary>已驳回</summary>
    Rejected
}
