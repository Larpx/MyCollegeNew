using Campus.Attendance.Web.Components;
using Campus.Attendance.Web.Services;
using Campus.Attendance.Shared.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;

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

// HttpClient 调用后端 API
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenService>();

// 注册 JWT 校验服务（Shared.Security.ITokenService → Infrastructure.Auth.TokenService）
builder.Services.AddSingleton<ITokenService, Campus.Attendance.Infrastructure.Auth.TokenService>();

builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();
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
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
