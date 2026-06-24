using SqlSugar;

namespace Campus.Attendance.Shared.Entities;

/// <summary>
/// 班级实体，隶属于专业，由辅导员管理
/// </summary>
[SugarTable("class")]
public class Class : EntityBase
{
    /// <summary>班级主键，自增</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "班级主键")]
    public long Id { get; set; }

    /// <summary>班级名称</summary>
    [SugarColumn(Length = 64, ColumnDescription = "班级名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>所属专业 Id</summary>
    [SugarColumn(ColumnDescription = "所属专业 Id")]
    public long MajorId { get; set; }

    /// <summary>年级（入学年份，如 2022）</summary>
    [SugarColumn(ColumnDescription = "年级")]
    public int Grade { get; set; }

    /// <summary>辅导员工号（关联 Teacher.Id）</summary>
    [SugarColumn(Length = 32, ColumnDescription = "辅导员工号")]
    public string CounselorId { get; set; } = string.Empty;
}
