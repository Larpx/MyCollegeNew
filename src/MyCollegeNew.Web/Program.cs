using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Larpx.PersonalTools.MyCollegeNew.Web.Components;
using Larpx.PersonalTools.MyCollegeNew.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Larpx.PersonalTools.MyCollegeNew.Web
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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // .NET Aspire 服务默认配置：服务发现、OpenTelemetry、健康检查、弹性策略
            builder.AddServiceDefaults();

            // Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Async(w => w.Console())
                .CreateLogger();

            builder.Host.UseSerilog();

            // Blazor Server - 默认 SSR，仅高交互组件使用 InteractiveServer
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // HttpClient 调用后端 API（BaseAddress 末尾需带 /，追加 api/v1 路由前缀）
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5144";
            var apiBase = apiBaseUrl.EndsWith("/") ? apiBaseUrl : apiBaseUrl + "/";
            builder.Services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBase + "api/v1/");
            });

            builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());
            builder.Services.AddHttpContextAccessor();

            // 注册 JWT 配置（IOptions<JwtConfig>），供 TokenService 使用
            builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));

            builder.Services.AddScoped<TokenService>();

            // 注册 JWT 校验服务（Shared.Security.ITokenService → Infrastructure.Auth.TokenService）
            builder.Services.AddSingleton<ITokenService, Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth.TokenService>();

            builder.Services.AddScoped<CustomAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
            });
            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            // .NET Aspire 健康检查端点
            app.MapDefaultEndpoints();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapStaticAssets();

            // 登录端点：接收表单提交，调用后端 API，写入认证 Cookie，重定向
            app.MapPost("/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
            {
                var username = context.Request.Form["Username"].FirstOrDefault();
                var password = context.Request.Form["Password"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    context.Response.Redirect("/login?error=empty");
                    return;
                }

                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var request = new LoginRequest { Username = username, Password = password };
                    var apiResponse = await httpClient.PostAsJsonAsync("login", request);

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        context.Response.Redirect("/login?error=invalid");
                        return;
                    }

                    var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResult>>();
                    if (result?.Data is null || string.IsNullOrEmpty(result.Data.Token))
                    {
                        context.Response.Redirect("/login?error=invalid");
                        return;
                    }

                    // 用户端不允许 Admin 角色登录，请使用管理员端
                    if (result.Data.Role == "Admin")
                    {
                        context.Response.Redirect("/login?error=invalid");
                        return;
                    }

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.Data.UserId),
                        new Claim(ClaimTypes.Name, result.Data.UserName),
                        new Claim(ClaimTypes.Role, result.Data.Role),
                        new Claim("token", result.Data.Token)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    var redirectUrl = result.Data.Role switch
                    {
                        "Teacher" => "/teacher/dashboard",
                        "Counselor" => "/teacher/dashboard",
                        "Student" => "/student/home",
                        _ => "/"
                    };
                    context.Response.Redirect(redirectUrl);
                }
                catch
                {
                    context.Response.Redirect("/login?error=invalid");
                }
            });

            // 登出端点：清除认证 Cookie
            app.MapPost("/auth/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok();
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}