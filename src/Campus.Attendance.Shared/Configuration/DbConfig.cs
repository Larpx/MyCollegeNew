using SqlSugar;

namespace Campus.Attendance.Shared.Configuration;

/// <summary>
/// 数据库提供程序类型枚举
/// </summary>
public enum ProviderType
{
    /// <summary>SQLite</summary>
    SQLite,
    /// <summary>MySQL</summary>
    MySQL
}

/// <summary>
/// 数据库连接配置
/// </summary>
public class DbConfig
{
    /// <summary>数据库提供程序类型，默认 SQLite</summary>
    public ProviderType ProviderType { get; set; } = ProviderType.SQLite;

    /// <summary>数据库连接字符串</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
