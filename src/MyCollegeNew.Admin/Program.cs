using Larpx.PersonalTools.MyCollegeNew.Admin.Components;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Larpx.PersonalTools.MyCollegeNew.Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Larpx.PersonalTools.MyCollegeNew.Admin
{
    /// <summary>
    /// 管理员端应用主入口
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

            // .NET Aspire 服务默认配置
            builder.AddServiceDefaults();

            // Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Async(w => w.Console())
                .CreateLogger();

            builder.Host.UseSerilog();

            // Blazor Server
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // HttpClient
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5144";
            var apiBase = apiBaseUrl.EndsWith("/") ? apiBaseUrl : apiBaseUrl + "/";
            builder.Services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBase + "api/v1/");
            });

            builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());
            builder.Services.AddHttpContextAccessor();

            // JWT 配置
            builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddSingleton<ITokenService, Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth.TokenService>();

            // 认证
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
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.MapDefaultEndpoints();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapStaticAssets();

            // 登录端点：仅允许 Admin 角色登录
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

                    // Admin 端仅允许 Admin 角色登录
                    if (result.Data.Role != "Admin")
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

                    context.Response.Redirect("/admin/dashboard");
                }
                catch
                {
                    context.Response.Redirect("/login?error=invalid");
                }
            });

            // 登出端点
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
