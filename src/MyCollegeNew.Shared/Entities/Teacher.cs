using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
/// <summary>
/// 教师实体，主键为工号（非自增），区分任课教师与辅导员角色
/// </summary>
[SugarTable("teacher")]
public class Teacher : EntityBase
{
    /// <summary>工号（主键，非自增）</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false, Length = 32, ColumnDescription = "工号")]
    public string Id { get; set; } = string.Empty;

    /// <summary>教师姓名</summary>
    [SugarColumn(Length = 32, ColumnDescription = "教师姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>登录密码（BCrypt 哈希值）</summary>
    [SugarColumn(Length = 128, ColumnDescription = "登录密码哈希")]
    public string Password { get; set; } = string.Empty;

    /// <summary>性别（"男" 或 "女"）</summary>
    [SugarColumn(Length = 8, ColumnDescription = "性别")]
    public string Gender { get; set; } = string.Empty;

    /// <summary>所属院系 Id</summary>
    [SugarColumn(ColumnDescription = "所属院系 Id")]
    public long DepartmentId { get; set; }

    /// <summary>所属专业 Id（辅导员可为空）</summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "所属专业 Id")]
    public long? MajorId { get; set; }

    /// <summary>教师角色（任课教师 / 辅导员）</summary>
    [SugarColumn(ColumnDescription = "教师角色")]
    public TeacherRole Role { get; set; }

    /// <summary>备注</summary>
    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
}