using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities;

/// <summary>
/// 学生实体，主键为学号（非自增），参与考勤签到与请假申请
/// </summary>
[SugarTable("student")]
public class Student : EntityBase
{
    /// <summary>学号（主键，非自增）</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false, Length = 32, ColumnDescription = "学号")]
    public string Id { get; set; } = string.Empty;

    /// <summary>学生姓名</summary>
    [SugarColumn(Length = 32, ColumnDescription = "学生姓名")]
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

    /// <summary>所属专业 Id</summary>
    [SugarColumn(ColumnDescription = "所属专业 Id")]
    public long MajorId { get; set; }

    /// <summary>所属班级 Id</summary>
    [SugarColumn(ColumnDescription = "所属班级 Id")]
    public long ClassId { get; set; }

    /// <summary>年级（入学年份，如 2022）</summary>
    [SugarColumn(ColumnDescription = "年级")]
    public int Grade { get; set; }

    /// <summary>在读状态（0=在读, 1=休学, 2=毕业）</summary>
    [SugarColumn(ColumnDescription = "在读状态")]
    public int Status { get; set; }

    /// <summary>备注</summary>
    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
