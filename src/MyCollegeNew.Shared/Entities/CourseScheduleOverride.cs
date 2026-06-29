using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 课表代课覆盖：调换课生效后，按周次覆盖原课表 TeacherId
    /// </summary>
    [SugarTable("course_schedule_override")]
    public class CourseScheduleOverride : EntityBase
    {
        /// <summary>覆盖层主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "覆盖层主键")]
        public long Id { get; set; }

        /// <summary>原排课 Id（关联 CourseSchedule.Id）</summary>
        [SugarColumn(ColumnDescription = "原排课 Id")]
        public long ScheduleId { get; set; }

        /// <summary>代课教师工号（生效期间内为此教师）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "代课教师工号")]
        public string SubstituteTeacherId { get; set; } = string.Empty;

        /// <summary>覆盖生效起始周</summary>
        [SugarColumn(ColumnDescription = "覆盖生效起始周")]
        public int StartWeek { get; set; }

        /// <summary>覆盖生效结束周</summary>
        [SugarColumn(ColumnDescription = "覆盖生效结束周")]
        public int EndWeek { get; set; }

        /// <summary>关联调换课申请 Id（关联 CourseSwapRequest.Id）</summary>
        [SugarColumn(ColumnDescription = "关联调换课申请 Id")]
        public long SwapRequestId { get; set; }
    }
}
