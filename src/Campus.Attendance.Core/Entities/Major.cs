using SqlSugar;

namespace Campus.Attendance.Core.Entities;

/// <summary>
/// 专业实体，隶属于院系
/// </summary>
[SugarTable("major")]
public class Major : EntityBase
{
    /// <summary>专业主键，自增</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "专业主键")]
    public long Id { get; set; }

    /// <summary>专业名称</summary>
    [SugarColumn(Length = 64, ColumnDescription = "专业名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [SugarColumn(ColumnDescription = "所属院系 Id")]
    public long DepartmentId { get; set; }
}
