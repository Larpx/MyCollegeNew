using Larpx.PersonalTools.MyCollegeNew.Shared.Features.System;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemAdmin
{
    /// <summary>查询系统信息（机器、运行环境、进程内存与运行时长等）</summary>
    public record GetSystemInfoQuery : IRequest<ApiResponse<SystemInfoDto>>;

    /// <summary>查询系统健康状态（数据库连通性、磁盘使用、各业务表记录数）</summary>
    public record GetHealthStatusQuery : IRequest<ApiResponse<HealthStatusDto>>;

    /// <summary>清除分布式缓存</summary>
    public record ClearCacheCommand : IRequest<ApiResponse<CacheClearResultDto>>;

    /// <summary>查询日志条目，按级别/分类/关键字/时间范围过滤</summary>
    public record GetLogsQuery(LogQueryDto Dto) : IRequest<ApiResponse<List<LogEntryDto>>>;
}
