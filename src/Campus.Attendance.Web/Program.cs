using Campus.Attendance.Web.Components;
using Campus.Attendance.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// 注册 Blazor Server 服务
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 配置 HttpClient 调用后端 API，基址从 appsettings.json 的 Api:BaseUrl 读取
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// 注册 IApiClient 与 ApiClient（Scoped，每用户独立实例）
builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());

// 注册前端 Token 服务（Scoped）
builder.Services.AddScoped<TokenService>();

// 注册自定义认证状态提供器与认证授权
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to adjust this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
