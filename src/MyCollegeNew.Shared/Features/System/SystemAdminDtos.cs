namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.System
{
    /// <summary>系统信息 DTO，反映运行进程与运行环境的实时信息</summary>
    public class SystemInfoDto
    {
        public string MachineName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string Uptime { get; set; } = string.Empty;
        public long WorkingSet { get; set; }
        public int CpuCount { get; set; }
    }

    /// <summary>健康状态 DTO，汇总数据库连通性、磁盘使用与各业务表记录数</summary>
    public class HealthStatusDto
    {
        public bool DatabaseHealthy { get; set; }
        public int DatabaseLatencyMs { get; set; }
        public long DiskFreeBytes { get; set; }
        public long DiskTotalBytes { get; set; }
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalDepartments { get; set; }
    }

    /// <summary>缓存清理结果 DTO</summary>
    public class CacheClearResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ClearedAt { get; set; }
    }

    /// <summary>单条日志条目 DTO，用于日志查看面板</summary>
    public class LogEntryDto
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }

    /// <summary>日志查询参数 DTO，支持按级别/分类/关键字/时间范围过滤</summary>
    public class LogQueryDto
    {
        /// <summary>日志级别过滤（INFO/WARN/ERROR 等），为空表示不过滤</summary>
        public string? Level { get; set; }

        /// <summary>分类过滤，为空表示不过滤</summary>
        public string? Category { get; set; }

        /// <summary>关键字过滤（匹配消息与异常文本），为空表示不过滤</summary>
        public string? Keyword { get; set; }

        /// <summary>返回条数上限，默认 100，最大 500</summary>
        public int Limit { get; set; } = 100;

        /// <summary>起始时间（UTC）</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>结束时间（UTC）</summary>
        public DateTime? EndTime { get; set; }
    }
}
