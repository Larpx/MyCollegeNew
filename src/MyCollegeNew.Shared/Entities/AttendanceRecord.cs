using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 考勤记录实体，记录学生在某次考勤会话中的出勤结果
    /// </summary>
    [SugarTable("attendance_record")]
    public class AttendanceRecord : EntityBase
    {
        /// <summary>考勤记录主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "考勤记录主键")]
        public long Id { get; set; }

        /// <summary>考勤会话 Id</summary>
        [SugarColumn(ColumnDescription = "考勤会话 Id")]
        public long SessionId { get; set; }

        /// <summary>学生学号（关联 Student.Id）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "学生学号")]
        public string StudentId { get; set; } = string.Empty;

        /// <summary>学生姓名（冗余字段，便于查询展示）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "学生姓名")]
        public string StudentName { get; set; } = string.Empty;

        /// <summary>考勤状态（Present/Late/Absent/Leave）</summary>
        [SugarColumn(ColumnDescription = "考勤状态")]
        public AttendanceStatus Status { get; set; }

        /// <summary>实际签到时间（UTC），缺勤时为空</summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "签到时间")]
        public DateTime? CheckInTime { get; set; }

        /// <summary>备注</summary>
        [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}