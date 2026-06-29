using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 任课分配实体：记录某教师接某门课程模板在某班级/合班的开课关系
    /// </summary>
    [SugarTable("course_assignment")]
    public class CourseAssignment : EntityBase
    {
        /// <summary>任课分配主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "任课分配主键")]
        public long Id { get; set; }

        /// <summary>课程模板 Id（关联 Course.Id）</summary>
        [SugarColumn(ColumnDescription = "课程模板 Id")]
        public long CourseId { get; set; }

        /// <summary>任课教师工号（关联 Teacher.Id）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "任课教师工号")]
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>合班班级 Id 列表（逗号分隔，如 "1,2,3"）</summary>
        [SugarColumn(Length = 128, ColumnDescription = "合班班级 Id 列表")]
        public string ClassIds { get; set; } = string.Empty;

        /// <summary>学期标识（如 "2026-Spring"）</summary>
        [SugarColumn(Length = 16, ColumnDescription = "学期标识")]
        public string Semester { get; set; } = string.Empty;

        /// <summary>接课状态（Pending 待系主任确认 / Active 已生效 / Withdrawn 已撤回）</summary>
        [SugarColumn(ColumnDescription = "接课状态")]
        public AssignmentStatus Status { get; set; }

        /// <summary>接课申请理由（教师主动接课时填写）</summary>
        [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "接课申请理由")]
        public string? ApplyReason { get; set; }

        /// <summary>系主任审批备注</summary>
        [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "系主任审批备注")]
        public string? ReviewRemark { get; set; }
    }
}
