using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// .NET Aspire 服务默认配置扩展方法
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 添加 Aspire 服务默认配置：服务发现、OpenTelemetry、健康检查、弹性策略
        /// </summary>
        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.ConfigureOpenTelemetry();
            builder.AddDefaultHealthChecks();
            builder.Services.AddServiceDiscovery();
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });
            return builder;
        }

        /// <summary>
        /// 配置 OpenTelemetry：追踪、指标、日志
        /// 包含 ASP.NET Core、HttpClient、SQL 数据库、Redis 缓存的链路追踪，
        /// 以及运行时 GC/JIT/线程池、进程内存/CPU 等指标
        /// </summary>
        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSqlClientInstrumentation()
                        .AddRedisInstrumentation();
                });

            builder.AddOpenTelemetryExporters();
            return builder;
        }

        /// <summary>
        /// 配置 OpenTelemetry 导出器：当 OTLP 端点配置时启用
        /// </summary>
        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }
            return builder;
        }

        /// <summary>
        /// 添加默认健康检查：自检 + 数据库连接探针
        /// </summary>
        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
            return builder;
        }

        /// <summary>
        /// 映射默认健康检查端点（仅开发环境）
        /// /health — 综合健康状态（含数据库等依赖）
        /// /alive  — 存活探针（仅进程自检，轻量级）
        /// /ready  — 就绪探针（含数据库连接等依赖就绪检查）
        /// </summary>
        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // 非开发环境不映射健康检查端点到公开路径
            if (!app.Environment.IsDevelopment()) return app;

            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
            app.MapHealthChecks("/ready", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("ready") || r.Tags.Contains("live")
            });
            return app;
        }
    }
}
