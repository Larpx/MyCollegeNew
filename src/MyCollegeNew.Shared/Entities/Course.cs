using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 课程模板实体，可由系主任发布或教师主动开课申请，经接课分配后形成实际授课关系
    /// </summary>
    [SugarTable("course")]
    public class Course : EntityBase
    {
        /// <summary>课程主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "课程主键")]
        public long Id { get; set; }

        /// <summary>课程名称</summary>
        [SugarColumn(Length = 64, ColumnDescription = "课程名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>默认任课教师工号（关联 Teacher.Id，由接课分配填充）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "任课教师工号")]
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>创建者工号（系主任发布课程模板时为系主任工号；教师主动开课时为申请人工号）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "创建者工号")]
        public string CreatorId { get; set; } = string.Empty;

        /// <summary>课程状态（Draft 草稿 / OpenForPick 开放接课 / Closed 已关闭接课）</summary>
        [SugarColumn(ColumnDescription = "课程状态")]
        public CourseStatus Status { get; set; }

        /// <summary>学分</summary>
        [SugarColumn(ColumnDescription = "学分")]
        public decimal Credit { get; set; }

        /// <summary>备注</summary>
        [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}