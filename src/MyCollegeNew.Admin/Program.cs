using Larpx.PersonalTools.MyCollegeNew.Admin.Components;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Larpx.PersonalTools.MyCollegeNew.Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

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
            var apiBaseAddress = apiBase + "api/v1/";

            // 类型客户端（供 Blazor 组件注入 IApiClient 使用）
            builder.Services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseAddress);
            });

            // 命名客户端（供 Program.cs 中的代理端点使用）
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseAddress);
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
            // 注册分布式缓存（开发环境使用内存实现，生产环境可改用 Redis）
            // 用途：临时存储 2FA 绑定阶段的 TOTP 密钥与二维码，避免通过非 HttpOnly Cookie 传输（M-4 修复）
            builder.Services.AddDistributedMemoryCache();
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
                var captchaToken = context.Request.Form["CaptchaToken"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    context.Response.Redirect("/login?error=empty");
                    return;
                }

                // 滑块验证码 token 必须由前端验证通过后提交，BFF 透传至 API 端强制校验
                if (string.IsNullOrWhiteSpace(captchaToken))
                {
                    context.Response.Redirect("/login?error=captcha");
                    return;
                }

                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var request = new LoginRequest { Username = username, Password = password, CaptchaToken = captchaToken };
                    var apiResponse = await httpClient.PostAsJsonAsync("login", request);

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        context.Response.Redirect("/login?error=invalid");
                        return;
                    }

                    var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResult>>();
                    if (result?.Data is null)
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

                    // 需要二次验证：将临时令牌存入 Cookie 并跳转
                    if (result.Data.RequiresTwoFactor && !string.IsNullOrEmpty(result.Data.TwoFactorToken))
                    {
                        context.Response.Cookies.Append("2fa_token", result.Data.TwoFactorToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(5),
                            Path = "/"
                        });
                        context.Response.Cookies.Append("2fa_has_secret", result.Data.HasTwoFactorSecret ? "1" : "0", new CookieOptions
                        {
                            HttpOnly = false,
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(5),
                            Path = "/"
                        });
                        context.Response.Redirect("/two-factor");
                        return;
                    }

                    if (string.IsNullOrEmpty(result.Data.Token))
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

            // 验证码端点：代理到后端 API（无需登录，无需 antiforgery）
            // 注意：API 端点注册在 /api/v1/captcha/slider（无 auth 前缀），代理路径需对应
            app.MapGet("/auth/captcha/slider", async (IHttpClientFactory httpClientFactory) =>
            {
                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var apiResponse = await httpClient.GetAsync("captcha/slider");

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        return Results.StatusCode((int)apiResponse.StatusCode);
                    }

                    var content = await apiResponse.Content.ReadAsStringAsync();
                    return Results.Text(content, "application/json");
                }
                catch
                {
                    return Results.StatusCode(503);
                }
            }).DisableAntiforgery();

            app.MapPost("/auth/captcha/slider/verify", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
            {
                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var body = await context.Request.ReadFromJsonAsync<SliderCaptchaVerifyRequest>();
                    if (body is null)
                    {
                        return Results.BadRequest();
                    }

                    var apiResponse = await httpClient.PostAsJsonAsync("captcha/slider/verify", body);
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    return Results.Text(content, "application/json");
                }
                catch
                {
                    return Results.StatusCode(503);
                }
            }).DisableAntiforgery();

            // 注：原 /auth/2fa-complete 端点已删除（H-2 修复）
            // 该端点未经验证即创建认证会话，存在 IDOR 风险；
            // 实际 2FA 流程已由 /auth/2fa-verify 与 /auth/2fa-bind 端点完成（两者均通过 API 验证 TOTP 后再签发 Cookie）

            // 2FA setup 端点：未绑定用户获取二维码
            // 安全说明：TOTP 密钥与二维码改用 IDistributedCache 服务端会话存储，避免通过非 HttpOnly Cookie 暴露给 XSS（M-4 修复）
            app.MapPost("/auth/2fa-setup", async (HttpContext context, IHttpClientFactory httpClientFactory, IDistributedCache cache) =>
            {
                var twoFactorToken = context.Request.Form["TwoFactorToken"].FirstOrDefault();
                if (string.IsNullOrEmpty(twoFactorToken))
                {
                    context.Response.Redirect("/login?error=expired");
                    return;
                }

                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var request = new TwoFactorSetupRequest { TwoFactorToken = twoFactorToken };
                    var apiResponse = await httpClient.PostAsJsonAsync("2fa/setup", request);
                    var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse<TwoFactorSetupResult>>();

                    if (result?.Data is null)
                    {
                        context.Response.Redirect("/two-factor?error=setup-failed");
                        return;
                    }

                    // 将 QR 与密钥写入服务端缓存（键为 2FA 临时令牌），TTL 与令牌有效期对齐（5 分钟）
                    // 前端 TwoFactor.razor 通过查询参数 setup=ready 触发从缓存读取并渲染
                    var cacheKey = $"2fa:setup:{twoFactorToken}";
                    var payload = JsonSerializer.Serialize(result.Data);
                    await cache.SetStringAsync(cacheKey, payload, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                    context.Response.Redirect("/two-factor?setup=ready");
                }
                catch
                {
                    context.Response.Redirect("/two-factor?error=setup-failed");
                }
            });

            // 2FA verify 端点：已绑定用户验证码校验
            app.MapPost("/auth/2fa-verify", async (HttpContext context, IHttpClientFactory httpClientFactory, IDistributedCache cache) =>
            {
                var twoFactorToken = context.Request.Form["TwoFactorToken"].FirstOrDefault();
                var code = context.Request.Form["Code"].FirstOrDefault();

                if (string.IsNullOrEmpty(twoFactorToken) || string.IsNullOrEmpty(code))
                {
                    context.Response.Redirect("/login?error=expired");
                    return;
                }

                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var request = new TwoFactorVerifyRequest { TwoFactorToken = twoFactorToken, Code = code };
                    var apiResponse = await httpClient.PostAsJsonAsync("2fa/verify", request);

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        context.Response.Redirect("/two-factor?error=invalid");
                        return;
                    }

                    var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResult>>();
                    if (result?.Data is null || string.IsNullOrEmpty(result.Data.Token))
                    {
                        context.Response.Redirect("/two-factor?error=invalid");
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

                    // 清理 2FA 临时 Cookie 与服务端缓存（TOTP 密钥不再写入 Cookie，M-4 修复）
                    var cookieOptions = new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax };
                    context.Response.Cookies.Delete("2fa_token", cookieOptions);
                    context.Response.Cookies.Delete("2fa_has_secret", cookieOptions);
                    await cache.RemoveAsync($"2fa:setup:{twoFactorToken}");

                    context.Response.Redirect("/admin/dashboard");
                }
                catch
                {
                    context.Response.Redirect("/two-factor?error=invalid");
                }
            });

            // 2FA bind 端点：未绑定用户首次绑定 TOTP
            app.MapPost("/auth/2fa-bind", async (HttpContext context, IHttpClientFactory httpClientFactory, IDistributedCache cache) =>
            {
                var twoFactorToken = context.Request.Form["TwoFactorToken"].FirstOrDefault();
                var code = context.Request.Form["Code"].FirstOrDefault();
                var secret = context.Request.Form["Secret"].FirstOrDefault();

                if (string.IsNullOrEmpty(twoFactorToken) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(secret))
                {
                    context.Response.Redirect("/login?error=expired");
                    return;
                }

                try
                {
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
                    var request = new TwoFactorBindRequest { TwoFactorToken = twoFactorToken, Code = code, Secret = secret };
                    var apiResponse = await httpClient.PostAsJsonAsync("2fa/bind", request);

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        context.Response.Redirect("/two-factor?error=invalid");
                        return;
                    }

                    var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResult>>();
                    if (result?.Data is null || string.IsNullOrEmpty(result.Data.Token))
                    {
                        context.Response.Redirect("/two-factor?error=invalid");
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

                    // 清理 2FA 临时 Cookie 与服务端缓存（TOTP 密钥不再写入 Cookie，M-4 修复）
                    var cookieOptions = new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax };
                    context.Response.Cookies.Delete("2fa_token", cookieOptions);
                    context.Response.Cookies.Delete("2fa_has_secret", cookieOptions);
                    await cache.RemoveAsync($"2fa:setup:{twoFactorToken}");

                    context.Response.Redirect("/admin/dashboard");
                }
                catch
                {
                    context.Response.Redirect("/two-factor?error=invalid");
                }
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
