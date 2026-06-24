using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Redis 缓存服务
var redis = builder.AddRedis("redis");

// SQL Server 数据库（生产环境使用，开发环境用 SQLite）
// var sqlServer = builder.AddSqlServer("sqlserver").AddDatabase("attendance");

// API 服务
var api = builder.AddProject<Campus_Attendance_Api>("api")
    .WithReference(redis)
    .WithEnvironment("Db__ProviderType", "SQLite")  // 开发环境默认 SQLite
    .WithEnvironment("Db__ConnectionString", "DataSource=attendance.db");

// Web 前端服务
builder.AddProject<Campus_Attendance_Web>("web")
    .WithReference(api);

builder.Build().Run();
