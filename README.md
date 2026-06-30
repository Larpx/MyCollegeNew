# 校园考勤管理系统

基于 **.NET 10 + ASP.NET Core 10** 的校园考勤管理系统，采用**垂直切片架构 (VSA) + CQRS + Minimal APIs**，支持管理员、教师（任课教师/辅导员）、学生三端功能。

> 特别感谢乔富强老师、刘明老师、赵旭老师、王刚老师、古月老师给予的支持和帮助。

---

## 功能特性

### 管理员
- 系统仪表盘：全局数据概览（学生数、教师数、课程数、出勤率）
- 组织架构管理：院系 → 专业 → 班级 → 学生四级组织树
- 教师管理：账号创建、角色分配（任课教师/辅导员）
- 学生管理：信息维护、班级分配、CSV 批量导入（自动生成 12 位随机密码并标记 `MustChangePassword`，首次登录强制改密）
- 课程管理：课程创建、学分设置、排课管理
- 统计报表：院系出勤率排名、趋势分析、Excel 导出

### 教师（任课教师 + 辅导员）
- 考勤会话：创建会话、查看进行中/历史会话
- 二维码签到：动态生成二维码（30 秒刷新），学生扫码签到
- 一键点名：一键标记全班出勤
- 随机点名：随机抽取学生，避免连续重复
- 手动补签：为缺勤学生手动修改状态
- 请假审批（辅导员）：审批/驳回学生请假，通过后自动联动考勤记录
- 考勤统计：按课程/班级统计出勤率，Excel 导出

### 学生
- 扫码签到：扫描教师生成的二维码完成签到
- 请假申请：提交请假、查看审批状态
- 个人考勤：查看出勤记录与统计
- 课程表：查看已排课程与上课时间

---

## 技术栈

| 层次 | 技术 |
|------|------|
| 运行时 | .NET 10 |
| 架构 | 垂直切片架构 (VSA) + CQRS |
| API | Minimal APIs |
| 中介者 | MediatR |
| 校验 | FluentValidation |
| 映射 | Mapster |
| ORM | SqlSugar Core（SQLite / MySQL） |
| 前端 | Blazor Web App（SSR + InteractiveServer） |
| 认证 | JWT Bearer + HttpOnly Cookie (BFF) |
| 文档 | Scalar |
| 日志 | Serilog |
| 缓存 | IDistributedCache + Redis |
| 监控 | OpenTelemetry |
| 编排 | .NET Aspire |
| 测试 | xUnit（55 个测试） |
| 部署 | Docker Compose（Linux） |

---

## 安全特性

本项目已通过两轮安全审计修复，第二轮共修复 **17 个漏洞**（5 HIGH / 7 MEDIUM / 5 LOW），关键能力如下：

### 认证与会话
- **登录滑块验证码强制校验**：`LoginRequest.CaptchaToken` 改为必填，Handler 强制校验 captcha token
- **JWT 黑名单机制**：新增 `TokenRevocationService` + `/auth/logout` 端点 + `jti` claim，`OnTokenValidated` 实时检查黑名单
- **2FA 重放保护**：TOTP 验证码使用 `IDistributedCache` 记录已用验证码，防止重放攻击
- **2FA 独立速率限制**：每 IP 每分钟 10 次，5 次失败锁定
- **TOTP 密钥服务端会话存储**：二维码/密钥改用 `IDistributedCache`，不再通过非 HttpOnly Cookie 传输
- **时序侧信道防护**：登录流程用户不存在时执行 dummy BCrypt Verify
- **删除 `/auth/2fa-complete` 死端点**：消除未验证即创建会话的风险

### 授权与角色
- **ICurrentUser 新增 SystemUserId**：JWT 新增 `system_user_id` claim，解决 Admin 角色类型混淆
- **ChangePasswordHandler Admin 分支**：兼容 Id/Username 查询

### 密码策略
- **密码复杂度增强**：新增 `ApplyPasswordPolicy()` FluentValidation 扩展，要求至少 8 位、含大小写字母和数字
- **学生默认随机密码**：CSV 批量导入使用 `RandomNumberGenerator` 生成 12 位随机密码，`Student` 实体新增 `MustChangePassword` 字段，首次登录强制改密
- **强制改密端点**：新增 `/auth/force-change-password` 端点与 `/force-change-password` 页面

### 审计与日志
- **IAuditService 审计日志服务**：记录登录成败、密码修改/重置、2FA 绑定/验证、用户增删、批量导入等敏感操作

### 安全配置
- **安全头增强**：`SecurityHeadersMiddleware` 新增 CSP、HSTS、Referrer-Policy、Permissions-Policy
- **CORS 白名单**：`appsettings.json` 的 `Cors:AllowedOrigins` 置空，生产环境通过环境变量注入
- **数据库连接字符串置空**：`appsettings.json` 不再内置连接字符串，启动时校验，生产环境通过 `Db__ConnectionString` 注入
- **CSV 批量导入异常脱敏**：不返回 `ex.Message` 给客户端
- **JWT SecretKey 管理**：`appsettings.Development.json` 置空，DEBUG 模式自动生成随机密钥，建议使用 `dotnet user-secrets` 管理

### 关键 API 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/v1/auth/login` | POST | 登录（强制校验滑块验证码） |
| `/api/v1/auth/logout` | POST | 登出（JWT 加入黑名单） |
| `/api/v1/auth/force-change-password` | POST | 强制修改密码（首次登录/学生随机密码场景） |
| `/api/v1/auth/2fa-setup` | POST | 启用 2FA（密钥经服务端会话下发） |
| `/api/v1/auth/2fa-verify` | POST | 验证 2FA（含重放保护 + 速率限制） |
| `/api/v1/users/students/batch-import` | POST | 学生 CSV 批量导入（生成随机密码 + 强制改密标记） |

---

## 快速开始

```bash
git clone https://github.com/Larpx/my-college-project.git
cd my-college-project
dotnet restore src/Campus.Attendance.sln
dotnet build src/Campus.Attendance.sln

# 运行 API
dotnet run --project src/Campus.Attendance.Api

# 运行 Web
dotnet run --project src/Campus.Attendance.Web

# 运行测试
dotnet test src/Campus.Attendance.Tests

# Aspire 本地编排
dotnet run --project src/Campus.Attendance.AppHost

# Docker 部署
docker-compose up -d --build
```

### 开发环境配置（user-secrets）

第二轮安全审计后，`appsettings.json` 中的数据库连接字符串、`appsettings.Development.json` 中的 JWT SecretKey 与 CORS 白名单均已置空。本地开发建议使用 `dotnet user-secrets` 管理敏感配置：

```bash
# 初始化 user-secrets（csproj 已配置 UserSecretsId）
cd src/Campus.Attendance.Api
dotnet user-secrets set "Db:ConnectionString" "DataSource=attendance.db"
dotnet user-secrets set "Jwt:SecretKey" "<至少 32 字符的随机字符串>"
dotnet user-secrets set "Cors:AllowedOrigins:0" "https://localhost:7088"

# 同样为 Web 项目配置
cd ../Campus.Attendance.Web
dotnet user-secrets set "Jwt:SecretKey" "<与 API 一致的随机字符串>"
```

> DEBUG 模式下若未配置 JWT SecretKey，将自动生成临时随机密钥，仅用于本地调试。
> 详细说明请参阅 [docs/](docs/) 目录下的文档。

---

## 默认账号

> ⚠️ 以下为种子数据内置账号，仅用于开发/演示环境。生产环境务必通过环境变量修改默认密码并启用 2FA。

| 角色 | 用户名 | 密码 | 说明 |
|------|--------|------|------|
| 管理员 | `admin` | `123456` | 首次登录建议立即修改 |
| 任课教师 | `T001` | `123456` | 示例任课教师 |
| 辅导员 | `T002` | `123456` | 示例辅导员 |
| 学生 | `20220101` | `220101` | 种子数据账号（学号后 6 位） |

> 📌 **注意**：第二轮安全审计后，**CSV 批量导入**的学生账号不再使用统一默认密码，改为 `RandomNumberGenerator` 生成 12 位随机密码，并标记 `MustChangePassword = true`，首次登录将被引导至 `/force-change-password` 页面强制改密。

---

## 文档

| 文档 | 说明 |
|------|------|
| [需求分析](docs/需求分析.md) | 项目背景与需求分析 |
| [需求规格说明](docs/需求规格说明.md) | 功能需求与验收标准 |
| [系统架构说明](docs/系统架构说明.md) | VSA 架构、数据流、认证流程 |
| [最佳实践需求文档](docs/最佳实践需求文档.md) | 技术栈选型与架构规范 |
| [开发任务与进度](docs/开发任务与进度.md) | 任务分解与完成状态 |
| [验收清单](docs/验收清单.md) | 功能验收检查项 |
| [部署指南](docs/部署指南.md) | Docker 部署与运维 |
| [开发指南](docs/开发指南.md) | 开发环境搭建与规范 |

---

## 许可证

[MIT License](LICENSE) &copy; 2026 Larpx
