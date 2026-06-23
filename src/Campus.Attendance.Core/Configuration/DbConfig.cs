namespace Campus.Attendance.Core.Configuration;

/// <summary>
/// 数据库提供程序类型枚举，支持 SQLite 与 MySQL 两种数据库
/// </summary>
public enum ProviderType
{
    /// <summary>SQLite 数据库（开发环境默认）</summary>
    SQLite,

    /// <summary>MySQL 数据库（生产环境）</summary>
    MySQL
}

/// <summary>
/// 数据库连接配置，通过 IOptions&lt;DbConfig&gt; 注入，支持环境变量覆盖
/// </summary>
public class DbConfig
{
    /// <summary>数据库提供程序类型，默认 SQLite</summary>
    public ProviderType ProviderType { get; set; } = ProviderType.SQLite;

    /// <summary>数据库连接字符串，禁止硬编码，支持环境变量 Db__ConnectionString 覆盖</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
