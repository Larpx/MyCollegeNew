using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 调换课申请实体：原任课教师发起，代课教师确认，仅换讲课人不换时间
    /// </summary>
    [SugarTable("course_swap_request")]
    public class CourseSwapRequest : EntityBase
    {
        /// <summary>调换课申请主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "调换课申请主键")]
        public long Id { get; set; }

        /// <summary>原排课 Id（关联 CourseSchedule.Id）</summary>
        [SugarColumn(ColumnDescription = "原排课 Id")]
        public long ScheduleId { get; set; }

        /// <summary>原任课教师工号（发起人）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "原任课教师工号")]
        public string OriginalTeacherId { get; set; } = string.Empty;

        /// <summary>代课教师工号（被委托人）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "代课教师工号")]
        public string SubstituteTeacherId { get; set; } = string.Empty;

        /// <summary>代课起始周次</summary>
        [SugarColumn(ColumnDescription = "代课起始周次")]
        public int StartWeek { get; set; }

        /// <summary>代课结束周次</summary>
        [SugarColumn(ColumnDescription = "代课结束周次")]
        public int EndWeek { get; set; }

        /// <summary>调换原因</summary>
        [SugarColumn(Length = 256, ColumnDescription = "调换原因")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>状态（Pending 代课人待确认 / Accepted 已生效 / Rejected 已拒绝 / Cancelled 已撤销）</summary>
        [SugarColumn(ColumnDescription = "调换课状态")]
        public SwapStatus Status { get; set; }

        /// <summary>代课人确认备注</summary>
        [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "代课人确认备注")]
        public string? SubstituteRemark { get; set; }

        /// <summary>代课人确认时间（UTC），未确认时为空</summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "代课人确认时间")]
        public DateTime? ConfirmedTime { get; set; }
    }
}
