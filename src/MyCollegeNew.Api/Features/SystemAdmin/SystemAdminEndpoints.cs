using Larpx.PersonalTools.MyCollegeNew.Shared.Features.System;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemAdmin
{
    /// <summary>
    /// 系统运维管理端点映射
    /// </summary>
    public static class SystemAdminEndpoints
    {
        /// <summary>
        /// 映射系统运维管理相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapSystemAdminEndpoints(this RouteGroupBuilder group)
        {
            // 查询系统信息
            group.MapGet("/system/info", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSystemInfoQuery());
                return Results.Ok(result);
            })
            .WithName("GetSystemInfo").WithSummary("查询系统信息").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<SystemInfoDto>>(StatusCodes.Status200OK);

            // 查询系统健康状态
            group.MapGet("/system/health", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetHealthStatusQuery());
                return Results.Ok(result);
            })
            .WithName("GetHealthStatus").WithSummary("查询系统健康状态").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<HealthStatusDto>>(StatusCodes.Status200OK);

            // 清除缓存
            group.MapPost("/system/cache/clear", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new ClearCacheCommand());
                return Results.Ok(result);
            })
            .WithName("ClearCache").WithSummary("清除缓存").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<CacheClearResultDto>>(StatusCodes.Status200OK);

            // 查询日志（按级别/分类/关键字/时间范围过滤）
            group.MapGet("/system/logs", async ([AsParameters] LogQueryDto query, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetLogsQuery(query));
                return Results.Ok(result);
            })
            .WithName("GetLogs").WithSummary("查询日志").RequireAuthorization("RequireAdmin")
            .Produces<ApiResponse<List<LogEntryDto>>>(StatusCodes.Status200OK);

            return group;
        }
    }
}
