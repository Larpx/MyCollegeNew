using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure
{
/// <summary>
/// 测试用数据库上下文，使用 SQLite 临时文件数据库，手动建表以绕过 BIGINT AUTOINCREMENT 限制
/// </summary>
public class TestDbContext : IDbContext, IDisposable
{
    private readonly SqlSugarClient _client;

    /// <summary>
    /// 构造函数，初始化 SQLite 临时文件数据库并手动建表
    /// </summary>
    public TestDbContext()
    {
        // 使用临时文件避免 SQLite 内存数据库连接管理复杂性
        var tempFile = Path.Combine(Path.GetTempPath(), $"attendance_test_{Guid.NewGuid():N}.db");
        _client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"DataSource={tempFile}",
            IsAutoCloseConnection = true
        });

        CreateTables();
        TempFilePath = tempFile;
    }

    /// <summary>临时数据库文件路径，释放时删除</summary>
    private string TempFilePath { get; }

    /// <summary>获取 SqlSugar 数据库客户端实例</summary>
    public ISqlSugarClient Client => _client;

    /// <summary>
    /// 手动创建所有实体表，使用 INTEGER PRIMARY KEY AUTOINCREMENT 避免 SQLite 类型限制
    /// </summary>
    private void CreateTables()
    {
        var db = _client;

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS department (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS major (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                DepartmentId INTEGER NOT NULL,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS class (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                MajorId INTEGER NOT NULL,
                Grade INTEGER NOT NULL,
                CounselorId TEXT NOT NULL,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS student (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Password TEXT NOT NULL,
                Gender TEXT NOT NULL,
                DepartmentId INTEGER NOT NULL,
                MajorId INTEGER NOT NULL,
                ClassId INTEGER NOT NULL,
                Grade INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                Remark TEXT,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS teacher (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Password TEXT NOT NULL,
                Gender TEXT NOT NULL,
                DepartmentId INTEGER NOT NULL,
                MajorId INTEGER,
                Role INTEGER NOT NULL,
                Remark TEXT,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS course (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                TeacherId TEXT NOT NULL,
                Credit REAL NOT NULL,
                Remark TEXT,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS course_schedule (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER NOT NULL,
                ClassId INTEGER NOT NULL,
                TeacherId TEXT NOT NULL,
                DayOfWeek INTEGER NOT NULL,
                StartSection INTEGER NOT NULL,
                EndSection INTEGER NOT NULL,
                StartWeek INTEGER NOT NULL,
                EndWeek INTEGER NOT NULL,
                Classroom TEXT NOT NULL,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS attendance_session (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER NOT NULL,
                ClassId INTEGER NOT NULL,
                TeacherId TEXT NOT NULL,
                ScheduleId INTEGER,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Status INTEGER NOT NULL,
                QrToken TEXT,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS attendance_record (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionId INTEGER NOT NULL,
                StudentId TEXT NOT NULL,
                StudentName TEXT NOT NULL,
                Status INTEGER NOT NULL,
                CheckInTime TEXT,
                Remark TEXT,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS leave_request (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId TEXT NOT NULL,
                CounselorId TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                LeaveType INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                Status INTEGER NOT NULL,
                ReviewRemark TEXT,
                ReviewTime TEXT,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS system_user (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Password TEXT NOT NULL,
                Role INTEGER NOT NULL,
                RealName TEXT NOT NULL,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """);

        db.Ado.ExecuteCommand("""
            CREATE TABLE IF NOT EXISTS audit_log (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                UserRole INTEGER NOT NULL,
                Action TEXT NOT NULL,
                Target TEXT,
                IpAddress TEXT,
                CreateTime TEXT NOT NULL
            );
            """);
    }

    /// <summary>
    /// 释放数据库连接资源并删除临时文件
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
        if (File.Exists(TempFilePath))
        {
            try { File.Delete(TempFilePath); }
            catch { /* 忽略删除失败 */ }
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 测试用 JWT 配置工厂
/// </summary>
public static class TestJwtConfigFactory
{
    /// <summary>测试用 SecretKey（长度满足 32 字符要求）</summary>
    public const string TestSecretKey = "Larpx.PersonalTools.MyCollegeNew.Test.SecretKey.2026";

    /// <summary>
    /// 创建 IOptions&lt;JwtConfig&gt; 测试实例
    /// </summary>
    public static IOptions<JwtConfig> Create()
        => Options.Create(new JwtConfig
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SecretKey = TestSecretKey,
            ExpireMinutes = 60
        });
}
}