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
- 安全响应头中间件（CSP、HSTS、X-Frame-Options、Referrer-Policy、Permissions-Policy）
- 响应压缩（Brotli + Gzip）、限流、OutputCache
- 启动时自动执行数据库建表与种子数据播种
- **JWT 黑名单机制**（`TokenRevocationService` + `OnTokenValidated` 实时校验，依赖 `IDistributedCache`/Redis）
- **IAuditService 审计日志服务**（记录登录成败、密码修改/重置、2FA 绑定/验证、用户增删、批量导入等敏感操作）
- **2FA 独立速率限制**（每 IP 每分钟 10 次，5 次失败锁定）+ **TOTP 重放保护**

## 认证与安全流程

### 登录流程
1. 客户端调用 `/api/v1/auth/login`，请求体必须包含 `CaptchaToken`（滑块验证码）
2. `LoginValidator` 通过 `NotEmpty()` 强制校验 `CaptchaToken`，`LoginHandler` 再次校验 captcha token 有效性
3. 用户不存在时执行 dummy BCrypt Verify，消除时序侧信道
4. 密码校验通过后签发 JWT（包含 `jti`、`system_user_id` 等 claim）
5. 若用户启用 2FA，返回 `requiresTwoFactor`，客户端走 2FA 验证流程
6. 若学生 `MustChangePassword = true`，返回 `mustChangePassword`，客户端跳转 `/force-change-password`

### 2FA 流程
- **启用**：`/api/v1/auth/2fa-setup` 生成 TOTP 密钥与二维码，密钥经 `IDistributedCache` 服务端会话存储，**不再通过非 HttpOnly Cookie 下发**
- **验证**：`/api/v1/auth/2fa-verify` 校验 TOTP 验证码：
  - 通过 `IDistributedCache` 记录已用验证码，**拒绝重放**
  - 独立速率限制（每 IP 每分钟 10 次），5 次失败锁定
- ~~`/auth/2fa-complete`~~：该死端点已在第二轮审计中删除（未验证即创建会话的风险）

### JWT 黑名单
- 登出端点 `/api/v1/auth/logout` 调用 `TokenRevocationService.RevokeAsync(jti, expireAt)` 将当前 token 的 `jti` 写入 `IDistributedCache`（TTL = token 剩余有效期）
- `OnTokenValidated` 事件实时检查 `jti` 是否在黑名单，命中则拒绝请求
- **依赖 Redis / `IDistributedCache`**，生产环境必须配置

### 审计日志
`IAuditService` 在以下场景记录审计事件（用户、IP、操作、结果、时间）：
- 登录成功 / 失败
- 密码修改 / 重置
- 2FA 绑定 / 验证
- 用户增删
- CSV 批量导入

## 关键 API 端点

### 认证（Auth）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/auth/login` | POST | 匿名 | 登录（强制校验滑块验证码 `CaptchaToken`） |
| `/api/v1/auth/logout` | POST | 已登录 | 登出，将当前 JWT 的 `jti` 加入黑名单 |
| `/api/v1/auth/force-change-password` | POST | 已登录 | 强制改密（学生随机密码首登 / `MustChangePassword` 场景） |
| `/api/v1/auth/2fa-setup` | POST | 已登录 | 启用 2FA，TOTP 密钥经服务端会话下发 |
| `/api/v1/auth/2fa-verify` | POST | 已登录 | 验证 2FA（含重放保护 + 速率限制） |
| `/api/v1/auth/change-password` | POST | 已登录 | 修改密码 |
| `/api/v1/auth/me` | GET | 已登录 | 获取当前用户信息 |

> 已删除端点：~~`/api/v1/auth/2fa-complete`~~（H-2 修复，避免未验证即创建会话）

### 用户管理（Users）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/users/students` | GET/POST | 管理员 | 学生列表 / 创建学生 |
| `/api/v1/users/students/{id}` | GET/PUT/DELETE | 管理员 | 学生详情 / 更新 / 删除 |
| `/api/v1/users/students/batch-import` | POST | 管理员 | CSV 批量导入学生（详见下方） |
| `/api/v1/users/teachers` | GET/POST | 管理员 | 教师列表 / 创建教师 |

### 学生 CSV 批量导入

`POST /api/v1/users/students/batch-import`（`multipart/form-data`，字段 `file`）

第二轮安全审计后的行为变更：

- **密码策略**：不再使用统一默认密码，改用 `RandomNumberGenerator` 为每位学生生成 **12 位随机密码**
- **强制改密标记**：`Student` 实体新增 `MustChangePassword` 字段，导入后置为 `true`，学生首次登录将被引导至 `/force-change-password`
- **响应体新增字段**：
  - `GeneratedPasswords`：`Dictionary<string, string>`，学号 → 随机密码清单（仅管理员可见，需通过安全渠道下发给学生）
  - `MustChangePasswordCount`：标记为强制改密的学生数
- **异常脱敏**：导入过程中发生异常时，响应仅返回通用错误信息（`ProblemDetails`），**不暴露 `ex.Message`** 给客户端（M-6 修复）
- **审计**：调用 `IAuditService` 记录批量导入操作

## 密码策略

通过 FluentValidation 扩展 `ApplyPasswordPolicy()` 统一约束密码复杂度（L-1 修复）：

- 最小长度 **8 位**
- 必须包含 **大写字母**（A-Z）
- 必须包含 **小写字母**（a-z）
- 必须包含 **数字**（0-9）

适用端点：`/auth/change-password`、`/auth/force-change-password`、用户创建/重置密码等。

## 安全配置

### 安全响应头
`SecurityHeadersMiddleware` 统一下发以下响应头（L-4 修复）：

| 响应头 | 取值 |
|--------|------|
| `Content-Security-Policy` | 默认 `default-src 'self'`，按页面需要放宽 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains`（生产环境） |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | 限制摄像头/麦克风/地理位置等敏感能力 |

### CORS 配置
第二轮审计后 CORS 改用 **白名单** 模式（M-7 修复）：

- `appsettings.json` 中 `Cors:AllowedOrigins` **置空**，不再使用通配符
- 允许的来源通过环境变量 `Cors__AllowedOrigins:0`、`Cors__AllowedOrigins:1`... 注入
- 启动时校验白名单非空（生产环境），若为空且非 Development 环境将抛出启动异常

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

> 第二轮安全审计后，`appsettings.json` 中的 `Db:ConnectionString`、`appsettings.Development.json` 中的 `Jwt:SecretKey` 与 `Cors:AllowedOrigins` 均已置空，启动时会校验关键配置非空，**生产环境必须通过环境变量或 user-secrets 注入**。

| 配置项 | 环境变量 | 说明 |
|--------|---------|------|
| `Db:ProviderType` | `Db__ProviderType` | 数据库类型（SQLite / MySQL） |
| `Db:ConnectionString` | `Db__ConnectionString` | 连接字符串（**H-5：appsettings 置空，必须注入**） |
| `Jwt:SecretKey` | `Jwt__SecretKey` | JWT 签名密钥（≥32 字符，**L-5：Development 置空，DEBUG 自动生成临时密钥，建议用 user-secrets**） |
| `Jwt:Issuer` | `Jwt__Issuer` | 签发者 |
| `Jwt:Audience` | `Jwt__Audience` | 受众 |
| `Jwt:ExpireMinutes` | `Jwt__ExpireMinutes` | 过期时间（分钟） |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins:0` ... | CORS 白名单（**M-7：appsettings 置空，必须注入**） |
| `ConnectionStrings__redis` | `ConnectionStrings__redis` | Redis 连接字符串（**JWT 黑名单 / TOTP 重放保护依赖此缓存**） |
