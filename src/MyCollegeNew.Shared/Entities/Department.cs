using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities;

/// <summary>
/// 院系实体，组织架构顶层
/// </summary>
[SugarTable("department")]
public class Department : EntityBase
{
    /// <summary>院系主键，自增</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "院系主键")]
    public long Id { get; set; }

    /// <summary>院系名称</summary>
    [SugarColumn(Length = 64, ColumnDescription = "院系名称")]
    public string Name { get; set; } = string.Empty;
}
