using Projects;

namespace Larpx.PersonalTools.MyCollegeNew.AppHost;

/// <summary>
/// .NET Aspire 分布式应用程序入口
/// </summary>
public class Program
{
    /// <summary>
    /// 应用程序主入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Redis 缓存服务
        var redis = builder.AddRedis("redis");

        // SQL Server 数据库（生产环境使用，开发环境用 SQLite）
        // var sqlServer = builder.AddSqlServer("sqlserver").AddDatabase("attendance");

        // API 服务
        var api = builder.AddProject<MyCollegeNew_Api>("api")
            .WithReference(redis)
            .WithEnvironment("Db__ProviderType", "SQLite")  // 开发环境默认 SQLite
            .WithEnvironment("Db__ConnectionString", "DataSource=attendance.db");

        // Web 前端服务
        builder.AddProject<MyCollegeNew_Web>("web")
            .WithReference(api);

        builder.Build().Run();
    }
}