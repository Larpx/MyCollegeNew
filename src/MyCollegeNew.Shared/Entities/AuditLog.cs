using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 操作日志实体，记录用户关键操作用于审计追溯，日志为不可变记录，仅含创建时间
    /// </summary>
    [SugarTable("audit_log")]
    public class AuditLog
    {
        /// <summary>日志主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "日志主键")]
        public long Id { get; set; }

        /// <summary>操作用户标识（学号/工号/用户名）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "操作用户标识")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>操作用户角色</summary>
        [SugarColumn(ColumnDescription = "操作用户角色")]
        public UserRole UserRole { get; set; }

        /// <summary>操作动作描述</summary>
        [SugarColumn(Length = 64, ColumnDescription = "操作动作")]
        public string Action { get; set; } = string.Empty;

        /// <summary>操作目标对象</summary>
        [SugarColumn(Length = 128, IsNullable = true, ColumnDescription = "操作目标")]
        public string? Target { get; set; }

        /// <summary>请求来源 IP 地址</summary>
        [SugarColumn(Length = 64, IsNullable = true, ColumnDescription = "IP 地址")]
        public string? IpAddress { get; set; }

        /// <summary>操作时间（UTC）</summary>
        [SugarColumn(IsOnlyIgnoreUpdate = true, ColumnDescription = "操作时间")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    }
}