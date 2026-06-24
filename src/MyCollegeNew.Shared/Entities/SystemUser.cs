using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
/// <summary>
/// 系统用户实体，用于管理员登录系统后台
/// </summary>
[SugarTable("system_user")]
public class SystemUser : EntityBase
{
    /// <summary>系统用户主键，自增</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "系统用户主键")]
    public long Id { get; set; }

    /// <summary>登录用户名</summary>
    [SugarColumn(Length = 64, ColumnDescription = "登录用户名")]
    public string Username { get; set; } = string.Empty;

    /// <summary>登录密码（BCrypt 哈希值）</summary>
    [SugarColumn(Length = 128, ColumnDescription = "登录密码哈希")]
    public string Password { get; set; } = string.Empty;

    /// <summary>用户角色（当前仅 Admin）</summary>
    [SugarColumn(ColumnDescription = "用户角色")]
    public UserRole Role { get; set; }

    /// <summary>真实姓名</summary>
    [SugarColumn(Length = 32, ColumnDescription = "真实姓名")]
    public string RealName { get; set; } = string.Empty;
}
}