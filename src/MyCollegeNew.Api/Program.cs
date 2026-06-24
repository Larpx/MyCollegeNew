using FluentValidation;
using Larpx.PersonalTools.MyCollegeNew.Api.Behaviors;
using Larpx.PersonalTools.MyCollegeNew.Api.Exceptions;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Attendance;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Leave;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Organization;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Profile.ChangePassword;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Statistics;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Students;
using Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers;
using Larpx.PersonalTools.MyCollegeNew.Api.Middleware;
using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth;
using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Data;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

namespace Larpx.PersonalTools.MyCollegeNew.Api
{
    /// <summary>
    /// 应用程序主入口
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 应用程序主入口点
        /// </summary>
        /// <param name="args">命令行参数</param>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // .NET Aspire 服务默认配置：服务发现、OpenTelemetry、健康检查、弹性策略
            builder.AddServiceDefaults();

            // Serilog 结构化日志
            builder.Host.UseSerilog((context, config) =>
            {
                config.ReadFrom.Configuration(context.Configuration);
            });

            // 注册数据库配置（IOptions<DbConfig>），连接字符串支持环境变量 Db__ConnectionString 覆盖
            builder.Services.Configure<DbConfig>(builder.Configuration.GetSection("Db"));

            // 注册 JWT 配置（IOptions<JwtConfig>），SecretKey 支持环境变量 Jwt__SecretKey 覆盖
            builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));

            // 注册数据库上下文与初始化器（Scoped：每请求独立连接上下文）
            builder.Services.AddScoped<IDbContext, SqlSugarDbContext>();
            builder.Services.AddScoped<DbInitializer>();

            // 注册 HttpContext 访问器与当前用户上下文（Scoped）
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

            // 注册令牌服务（Singleton：无状态服务）
            builder.Services.AddSingleton<ITokenService, TokenService>();

            // MediatR CQRS：注册所有 Handler 所在程序集
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // FluentValidation：注册所有 Validator 所在程序集
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            // 配置 JWT Bearer 认证
            var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>()
                ?? throw new InvalidOperationException("未配置 Jwt 节点");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtConfig.Issuer,
                        ValidAudience = jwtConfig.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            // 配置基于角色的授权策略
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireTeacher", policy => policy.RequireRole("Teacher", "Counselor"));
                options.AddPolicy("RequireStudent", policy => policy.RequireRole("Student"));
                options.AddPolicy("RequireCounselor", policy => policy.RequireRole("Counselor"));
            });

            // 全局异常处理器（IExceptionHandler 替代自定义中间件）
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // OpenAPI 文档生成
            builder.Services.AddOpenApi();

            // 速率限制
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                });
            });

            // Distributed Cache - Redis（生产环境）或 Memory（开发环境）
            var redisConnectionString = builder.Configuration.GetConnectionString("redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
            }
            else
            {
                builder.Services.AddDistributedMemoryCache();
            }

            // 输出缓存
            builder.Services.AddOutputCache(options =>
            {
                options.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromMinutes(5)));
            });

            // API 版本控制
            builder.Services.AddApiVersioning();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // 响应压缩
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            var app = builder.Build();

            // 启动时执行数据库自动建表与种子数据播种
            using (var scope = app.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
                await initializer.InitializeAsync();
                await initializer.SeedAsync();
            }

            // 全局异常处理（替代自定义中间件）
            app.UseExceptionHandler();

            // .NET Aspire 健康检查端点
            app.MapDefaultEndpoints();

            // 安全头中间件
            app.UseMiddleware<SecurityHeadersMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                // Scalar OpenAPI 文档（替代 Swashbuckle）
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseCors();
            app.UseRateLimiter();
            app.UseOutputCache();

            app.UseAuthentication();
            app.UseAuthorization();

            // Minimal API 端点映射（VSA Feature Slices）
            var api = app.MapGroup("/api/v1")
                .RequireRateLimiting("fixed");

            api.MapLoginEndpoint();
            api.MapChangePasswordEndpoint();
            api.MapStudentEndpoints();
            api.MapTeacherEndpoints();
            api.MapOrganizationEndpoints();
            api.MapCourseEndpoints();
            api.MapAttendanceEndpoints();
            api.MapLeaveEndpoints();
            api.MapStatisticsEndpoints();

            await app.RunAsync();
        }
    }
}