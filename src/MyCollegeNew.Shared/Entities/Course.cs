using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
/// <summary>
/// 课程实体，由任课教师开设，关联班级形成课表
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

    /// <summary>任课教师工号（关联 Teacher.Id）</summary>
    [SugarColumn(Length = 32, ColumnDescription = "任课教师工号")]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>学分</summary>
    [SugarColumn(ColumnDescription = "学分")]
    public decimal Credit { get; set; }

    /// <summary>备注</summary>
    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
}