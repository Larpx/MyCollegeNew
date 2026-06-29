namespace Larpx.PersonalTools.MyCollegeNew.Shared.Enums
{
    /// <summary>
    /// 接课分配状态枚举
    /// </summary>
    public enum AssignmentStatus
    {
        /// <summary>待系主任确认</summary>
        Pending,
        /// <summary>已生效（系主任审批通过）</summary>
        Active,
        /// <summary>已撤回（教师主动撤回或系主任驳回）</summary>
        Withdrawn
    }
}
