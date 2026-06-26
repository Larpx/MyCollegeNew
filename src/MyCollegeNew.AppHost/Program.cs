using Projects;

namespace Larpx.PersonalTools.MyCollegeNew.AppHost
{
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

            // 注：开发环境当前无 Docker，不启动 Redis 容器。
            // API 项目 Program.cs 检测到 ConnectionStrings:redis 为空时，
            // 会自动回退到 DistributedMemoryCache（进程内分布式缓存）。
            // 生产环境如需启用 Redis，可在此恢复 builder.AddRedis("redis")
            // 并通过 .WithReference(redis) 注入到 api 服务。

            // SQL Server 数据库（生产环境使用，开发环境用 SQLite）
            // var sqlServer = builder.AddSqlServer("sqlserver").AddDatabase("attendance");

            // API 服务
            var api = builder.AddProject<MyCollegeNew_Api>("api")
                .WithEnvironment("Db__ProviderType", "SQLite")  // 开发环境默认 SQLite
                .WithEnvironment("Db__ConnectionString", "DataSource=attendance.db");

            // Web 前端服务
            builder.AddProject<MyCollegeNew_Web>("web")
                .WithReference(api);

            // 管理员端服务
            builder.AddProject<MyCollegeNew_Admin>("Admin")
                .WithReference(api);

            builder.Build().Run();
        }
    }
}
