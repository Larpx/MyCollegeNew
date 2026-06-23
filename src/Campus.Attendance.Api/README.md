# Campus.Attendance.Api

API 层，提供 RESTful HTTP 接口，供 Blazor Server 前端调用。

## 项目职责

- 暴露 RESTful 端点（`api/[controller]` 路由约定）
- 配置 JWT Bearer 认证与基于角色的策略授权
- 配置 Swagger/OpenAPI 文档（Development 环境）
- 全局异常处理与安全响应头中间件
- 启动时自动执行数据库建表与种子数据播种

## 关键目录与类型

| 目录 | 关键类型 | 说明 |
|------|---------|------|
| `Controllers/` | `AuthController` | 登录认证（POST /api/auth/login） |
| | `ProfileController` | 当前用户信息（GET /api/profile/me） |
| | `StudentsController`, `TeachersController` | 学生/教师管理 |
| | `DepartmentsController`, `MajorsController`, `ClassesController` | 组织架构管理 |
| | `CoursesController`, `SchedulesController` | 课程与排课管理 |
| | `SessionsController` | 考勤会话（创建/二维码/签到/点名） |
| | `LeavesController` | 请假申请与审批 |
| | `StatisticsController` | 出勤统计与报表导出 |
| `Middleware/` | `GlobalExceptionMiddleware` | 全局异常捕获，返回统一 ApiResponse |
| | `SecurityHeadersMiddleware` | 安全响应头（X-Content-Type-Options 等） |
| `Program.cs` | — | 服务注册、JWT 配置、启动时数据库初始化 |

## 依赖关系

- 引用 `Campus.Attendance.Core`、`Campus.Attendance.Models`、`Campus.Attendance.Services`
- NuGet 包：`Microsoft.AspNetCore.Authentication.JwtBearer`、`Swashbuckle.AspNetCore`
- 运行端口：开发 5144（HTTP）/ 7088（HTTPS），Docker 8080

## 配置

| 配置项 | 环境变量 | 说明 |
|--------|---------|------|
| `Db:ProviderType` | `Db__ProviderType` | 数据库类型（SQLite / MySQL） |
| `Db:ConnectionString` | `Db__ConnectionString` | 连接字符串 |
| `Jwt:SecretKey` | `Jwt__SecretKey` | JWT 签名密钥（≥32 字符） |
| `Jwt:Issuer` | `Jwt__Issuer` | 签发者 |
| `Jwt:Audience` | `Jwt__Audience` | 受众 |
| `Jwt:ExpireMinutes` | `Jwt__ExpireMinutes` | 过期时间（分钟） |
