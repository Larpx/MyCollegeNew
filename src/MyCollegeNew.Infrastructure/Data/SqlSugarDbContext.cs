using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Data;

/// <summary>
/// SqlSugar 数据库上下文实现，根据 DbConfig.ProviderType 动态创建 SQLite 或 MySQL 连接
/// </summary>
public sealed class SqlSugarDbContext : IDbContext
{
    private readonly ILogger<SqlSugarDbContext> _logger;
    private readonly SqlSugarClient _client;

    /// <summary>
    /// 构造函数，注入数据库配置与日志器，初始化 SqlSugar 客户端
    /// </summary>
    /// <param name="dbConfig">数据库配置（通过 IOptions 注入，支持环境变量覆盖）</param>
    /// <param name="logger">日志器</param>
    public SqlSugarDbContext(IOptions<DbConfig> dbConfig, ILogger<SqlSugarDbContext> logger)
    {
        _logger = logger;
        var config = dbConfig.Value;
        var dbType = ResolveDbType(config.ProviderType);

        _client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = dbType,
            ConnectionString = config.ConnectionString,
            IsAutoCloseConnection = true,
            MoreSettings = new ConnMoreSettings
            {
                IsAutoRemoveDataCache = true
            }
        });

        _logger.LogInformation("SqlSugar 数据库上下文已初始化，提供程序: {ProviderType}", config.ProviderType);
    }

    /// <summary>获取 SqlSugar 数据库客户端实例</summary>
    public ISqlSugarClient Client => _client;

    /// <summary>
    /// 将业务 ProviderType 枚举映射为 SqlSugar DbType
    /// </summary>
    /// <param name="providerType">业务数据库提供程序类型</param>
    /// <returns>SqlSugar 数据库类型</returns>
    /// <exception cref="ArgumentOutOfRangeException">未知的提供程序类型</exception>
    private static DbType ResolveDbType(ProviderType providerType) => providerType switch
    {
        ProviderType.SQLite => DbType.Sqlite,
        ProviderType.MySQL => DbType.MySql,
        _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "未知的数据库提供程序类型")
    };
}
