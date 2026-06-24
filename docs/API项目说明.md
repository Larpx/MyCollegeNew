# API 项目说明

API 入口项目，基于 **Minimal APIs + 垂直切片架构 (VSA)** 提供 RESTful HTTP 接口。

## 项目职责

- 使用 Minimal APIs 按模块注册路由（`MapXxxEndpoints` 扩展方法）
- 配置 JWT Bearer 认证与基于角色的策略授权
- 集成 MediatR CQRS（Command/Query + Handler 分离）
- 集成 FluentValidation 管道自动校验（ValidationBehavior）
- 集成 Mapster 对象映射
- 集成 Serilog 结构化日志 + 异步写入
- 集成 Scalar OpenAPI 文档
- API 版本控制（URL 路径：`/api/v1/...`）
- 统一异常处理（`IExceptionHandler` + `ProblemDetails` RFC 7807）
- 安全响应头中间件
- 响应压缩（Brotli + Gzip）、限流、OutputCache
- 启动时自动执行数据库建表与种子数据播种

## 关键目录与类型

| 目录 | 说明 |
|------|------|
| `Features/Auth/` | 认证切片：登录、登出、个人信息 |
| `Features/Users/` | 用户管理切片：学生/教师 CRUD、批量导入、密码修改 |
| `Features/Organization/` | 组织架构切片：院系/专业/班级管理 |
| `Features/Courses/` | 课程与排课切片 |
| `Features/Attendance/` | 考勤切片：会话创建、二维码签到、一键点名、随机点名、手动补签 |
| `Features/Leave/` | 请假切片：申请、审批、记录查询 |
| `Features/Statistics/` | 统计切片：全局统计、院系排名、趋势分析、Excel 导出 |
| `Behaviors/` | MediatR 管道行为（FluentValidation 自动校验） |
| `ExceptionHandler/` | 全局异常处理器（`IExceptionHandler`） |
| `Program.cs` | 启动配置、DI 注册、中间件编排 |

## 依赖关系

- 引用 `Campus.Attendance.Shared`、`Campus.Attendance.Infrastructure`、`Campus.Attendance.ServiceDefaults`
- NuGet 包：`MediatR`、`FluentValidation`、`Mapster`、`Serilog`、`Scalar`、`Asp.Versioning`
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
| `ConnectionStrings__redis` | `ConnectionStrings__redis` | Redis 连接字符串 |
