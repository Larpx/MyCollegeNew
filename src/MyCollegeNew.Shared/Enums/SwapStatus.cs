namespace Larpx.PersonalTools.MyCollegeNew.Shared.Enums
{
    /// <summary>
    /// 调换课申请状态枚举
    /// </summary>
    public enum SwapStatus
    {
        /// <summary>代课人待确认</summary>
        Pending,
        /// <summary>已生效（代课人确认接受）</summary>
        Accepted,
        /// <summary>已拒绝（代课人拒绝）</summary>
        Rejected,
        /// <summary>已撤销（原任课人撤销）</summary>
        Cancelled
    }
}
