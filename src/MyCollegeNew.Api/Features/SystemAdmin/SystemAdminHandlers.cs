using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.System;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemAdmin
{
    /// <summary>
    /// 系统运维管理处理器：系统信息、健康状态、缓存清理、日志查看
    /// </summary>
    public class SystemAdminHandlers :
        IRequestHandler<GetSystemInfoQuery, ApiResponse<SystemInfoDto>>,
        IRequestHandler<GetHealthStatusQuery, ApiResponse<HealthStatusDto>>,
        IRequestHandler<ClearCacheCommand, ApiResponse<CacheClearResultDto>>,
        IRequestHandler<GetLogsQuery, ApiResponse<List<LogEntryDto>>>
    {
        // 最近一次缓存清理时间在分布式缓存中的键，便于多实例协同感知
        private const string CacheLastClearedKey = "system:cache:last-cleared";

        // 日志行解析正则：匹配 "时间戳 [级别] 其余内容"，容忍多种常见格式
        private static readonly Regex LogLinePattern = new(
            @"^(?<ts>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:[.,]\d+)?(?:Z|[+-]\d{2}:?\d{2})?)\s+\[(?<level>[^\]]+)\]\s*(?<rest>.*)$",
            RegexOptions.Compiled);

        private readonly IDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SystemAdminHandlers> _logger;
        private readonly IDistributedCache _distributedCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="env">Web 主机环境</param>
        /// <param name="logger">日志器</param>
        /// <param name="distributedCache">分布式缓存</param>
        public SystemAdminHandlers(
            IDbContext dbContext,
            IWebHostEnvironment env,
            ILogger<SystemAdminHandlers> logger,
            IDistributedCache distributedCache)
        {
            _dbContext = dbContext;
            _env = env;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        /// <summary>查询系统信息：机器、运行环境、进程内存与运行时长</summary>
        public Task<ApiResponse<SystemInfoDto>> Handle(GetSystemInfoQuery _, CancellationToken cancellationToken)
        {
            using var process = Process.GetCurrentProcess();
            var startTime = process.StartTime;

            var dto = new SystemInfoDto
            {
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.VersionString,
                DotNetVersion = Environment.Version.ToString(),
                AppName = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown",
                AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
                Environment = _env.EnvironmentName,
                StartTime = startTime,
                Uptime = (DateTime.Now - startTime).ToString(@"d\.hh\:mm\:ss"),
                WorkingSet = process.WorkingSet64,
                CpuCount = Environment.ProcessorCount
            };

            return Task.FromResult(ApiResponse<SystemInfoDto>.Success(dto));
        }

        /// <summary>查询系统健康状态：数据库连通性、磁盘使用、各业务表记录数</summary>
        public async Task<ApiResponse<HealthStatusDto>> Handle(GetHealthStatusQuery _, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;

            // 数据库连通性检测：执行 SELECT 1 并计时
            var stopwatch = Stopwatch.StartNew();
            bool databaseHealthy;
            try
            {
                await db.Ado.GetIntAsync("SELECT 1");
                databaseHealthy = true;
            }
            catch (Exception ex)
            {
                databaseHealthy = false;
                _logger.LogError(ex, "数据库健康检查失败");
            }
            stopwatch.Stop();

            // 业务表记录数（仅当数据库健康时统计，失败回退为 0）
            var totalUsers = 0;
            var totalStudents = 0;
            var totalTeachers = 0;
            var totalDepartments = 0;
            if (databaseHealthy)
            {
                try
                {
                    totalUsers = await db.Queryable<SystemUser>().Where(u => !u.IsDeleted).CountAsync();
                    totalStudents = await db.Queryable<Student>().Where(s => !s.IsDeleted).CountAsync();
                    totalTeachers = await db.Queryable<Teacher>().Where(t => !t.IsDeleted).CountAsync();
                    totalDepartments = await db.Queryable<Department>().Where(d => !d.IsDeleted).CountAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "统计业务表记录数失败");
                }
            }

            // 磁盘使用：取 C 盘可用空间与总容量，失败回退为 0
            long diskFreeBytes = 0;
            long diskTotalBytes = 0;
            try
            {
                var drive = Array.Find(DriveInfo.GetDrives(), d => d.IsReady && d.Name == @"C:\");
                if (drive is not null)
                {
                    diskFreeBytes = drive.AvailableFreeSpace;
                    diskTotalBytes = drive.TotalSize;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取磁盘信息失败");
            }

            return ApiResponse<HealthStatusDto>.Success(new HealthStatusDto
            {
                DatabaseHealthy = databaseHealthy,
                DatabaseLatencyMs = (int)stopwatch.ElapsedMilliseconds,
                DiskFreeBytes = diskFreeBytes,
                DiskTotalBytes = diskTotalBytes,
                TotalUsers = totalUsers,
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalDepartments = totalDepartments
            });
        }

        /// <summary>
        /// 清除缓存：分布式缓存（DistributedMemoryCache / Redis）不支持批量清空，
        /// 此处记录最近一次清理时间到分布式缓存，便于多实例协同感知。
        /// </summary>
        public async Task<ApiResponse<CacheClearResultDto>> Handle(ClearCacheCommand _, CancellationToken cancellationToken)
        {
            try
            {
                var clearedAt = DateTime.UtcNow;
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                };
                await _distributedCache.SetStringAsync(
                    CacheLastClearedKey,
                    clearedAt.ToString("O"),
                    options,
                    cancellationToken);

                return ApiResponse<CacheClearResultDto>.Success(new CacheClearResultDto
                {
                    Success = true,
                    Message = "缓存清理已完成",
                    ClearedAt = clearedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存失败");
                return ApiResponse<CacheClearResultDto>.Success(new CacheClearResultDto
                {
                    Success = false,
                    Message = "清除缓存失败",
                    ClearedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>查询日志：读取 Logs 目录下最新日志文件，按级别/分类/关键字/时间范围过滤</summary>
        public async Task<ApiResponse<List<LogEntryDto>>> Handle(GetLogsQuery query, CancellationToken cancellationToken)
        {
            var dto = query.Dto ?? new LogQueryDto();
            // 限制返回条数：默认 100，最大 500，最小 1
            var limit = dto.Limit <= 0 ? 100 : Math.Min(dto.Limit, 500);

            var levelFilter = NormalizeLevel(dto.Level);
            var categoryFilter = dto.Category?.Trim();
            var keywordFilter = dto.Keyword?.Trim();

            var logsDir = Path.Combine(_env.ContentRootPath, "Logs");
            if (!Directory.Exists(logsDir))
            {
                return ApiResponse<List<LogEntryDto>>.Success(new List<LogEntryDto>());
            }

            try
            {
                var dirInfo = new DirectoryInfo(logsDir);
                var logFile = dirInfo.GetFiles("*.log", SearchOption.TopDirectoryOnly)
                    .Concat(dirInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                if (logFile is null)
                {
                    return ApiResponse<List<LogEntryDto>>.Success(new List<LogEntryDto>());
                }

                var lines = await File.ReadAllLinesAsync(logFile.FullName, cancellationToken);
                var entries = ParseLogEntries(lines);

                var result = entries
                    .Where(e => string.IsNullOrEmpty(levelFilter)
                        || NormalizeLevel(e.Level) == levelFilter)
                    .Where(e => string.IsNullOrEmpty(categoryFilter)
                        || e.Category.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase))
                    .Where(e => string.IsNullOrEmpty(keywordFilter)
                        || e.Message.Contains(keywordFilter, StringComparison.OrdinalIgnoreCase)
                        || (e.Exception?.Contains(keywordFilter, StringComparison.OrdinalIgnoreCase) ?? false)
                        || e.Category.Contains(keywordFilter, StringComparison.OrdinalIgnoreCase))
                    .Where(e => !dto.StartTime.HasValue || e.Timestamp >= dto.StartTime.Value)
                    .Where(e => !dto.EndTime.HasValue || e.Timestamp <= dto.EndTime.Value)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                return ApiResponse<List<LogEntryDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取日志文件失败：{LogDir}", logsDir);
                return ApiResponse<List<LogEntryDto>>.Success(new List<LogEntryDto>());
            }
        }

        /// <summary>
        /// 将日志文件行解析为日志条目列表：匹配时间戳的行作为新条目起始，
        /// 不匹配的行视为上一条目的异常堆栈续行，追加到 Exception 字段。
        /// </summary>
        /// <param name="lines">日志文件所有行</param>
        /// <returns>解析后的日志条目列表</returns>
        private static List<LogEntryDto> ParseLogEntries(string[] lines)
        {
            var entries = new List<LogEntryDto>();
            LogEntryDto? current = null;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var match = LogLinePattern.Match(line);
                if (match.Success)
                {
                    if (current is not null)
                    {
                        entries.Add(current);
                    }
                    current = BuildEntry(match);
                }
                else if (current is not null)
                {
                    // 不匹配时间戳格式的行视为异常堆栈续行
                    current.Exception = string.IsNullOrEmpty(current.Exception)
                        ? line
                        : current.Exception + Environment.NewLine + line;
                }
            }
            if (current is not null)
            {
                entries.Add(current);
            }
            return entries;
        }

        /// <summary>根据正则匹配结果构建单条日志条目，并尝试从内容中提取分类</summary>
        /// <param name="match">正则匹配结果</param>
        /// <returns>日志条目</returns>
        private static LogEntryDto BuildEntry(Match match)
        {
            var timestamp = DateTime.TryParse(match.Groups["ts"].Value, out var parsed) ? parsed : DateTime.UtcNow;
            var level = match.Groups["level"].Value.ToUpperInvariant();
            var rest = match.Groups["rest"].Value;

            // 启发式提取分类：内容形如 "Larpx.Xxx.Handlers: 消息" 时，前缀视为分类名
            var category = string.Empty;
            var message = rest;
            var colon = rest.IndexOf(':');
            if (colon > 0 && colon <= 128)
            {
                var prefix = rest.Substring(0, colon).Trim();
                if (prefix.Contains('.') && !prefix.Contains(' '))
                {
                    category = prefix;
                    message = rest.Substring(colon + 1).TrimStart();
                }
            }

            return new LogEntryDto
            {
                Timestamp = timestamp,
                Level = level,
                Category = category,
                Message = message
            };
        }

        /// <summary>
        /// 将日志级别归一化为统一形式，兼容 Serilog 三字母缩写（INF/WRN/ERR/DBG）与全称（INFO/WARN/ERROR）。
        /// 空值返回空字符串表示不过滤。
        /// </summary>
        /// <param name="level">原始级别字符串</param>
        /// <returns>归一化后的级别</returns>
        private static string NormalizeLevel(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                return string.Empty;
            }

            var upper = level.Trim().ToUpperInvariant();
            return upper switch
            {
                "INF" or "INFORMATION" or "INFO" => "INFO",
                "WRN" or "WARN" or "WARNING" => "WARN",
                "ERR" or "ERROR" => "ERROR",
                "DBG" or "DEBUG" => "DEBUG",
                "FTL" or "FATAL" => "FATAL",
                _ => upper
            };
        }
    }
}
