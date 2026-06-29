namespace Larpx.PersonalTools.MyCollegeNew.Shared.Constants
{
    /// <summary>
    /// 调换课申请 SLA（服务等级协议）相关常量
    /// 集中管理避免魔法数字，供 SwapHandlers 拼装响应 DTO 与 SwapSlaExpirationService 后台扫描共享
    /// </summary>
    public static class CourseSwapSlaConstants
    {
        /// <summary>SLA 时长（小时）：申请创建后多少小时内代课人未确认则视为逾期</summary>
        public const int SlaHours = 48;

        /// <summary>即将逾期阈值（小时）：剩余处理时间小于等于该值时标记为"即将逾期"</summary>
        public const int ExpiringSoonHours = 12;

        /// <summary>后台扫描间隔（分钟）：SwapSlaExpirationService 处理过期申请的轮询周期</summary>
        public const int ExpirationScanIntervalMinutes = 30;
    }
}
