using System.Text;
using Campus.Attendance.Api.Middleware;
using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Services.Attendance;
using Campus.Attendance.Services.Auth;
using Campus.Attendance.Services.Courses;
using Campus.Attendance.Services.Data;
using Campus.Attendance.Services.Leave;
using Campus.Attendance.Services.Organization;
using Campus.Attendance.Services.Statistics;
using Campus.Attendance.Services.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

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

// 注册认证与令牌服务
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

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

// Add services to the container.
builder.Services.AddControllers();

// 配置 Swagger，支持 Bearer Token 输入
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Campus Attendance API",
        Version = "v1",
        Description = "考勤管理系统 API 文档"
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "请输入 JWT 令牌，格式：Bearer {token}"
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 启动时执行数据库自动建表与种子数据播种
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
    await initializer.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
