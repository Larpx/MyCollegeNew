# 校园考勤管理系统

基于 **.NET 10 + ASP.NET Core 10** 的校园考勤管理系统，采用**垂直切片架构 (VSA) + CQRS + Minimal APIs**，支持管理员、教师（任课教师/辅导员）、学生三端功能，涵盖二维码签到、一键点名、请假审批、统计报表等核心场景。

> 特别感谢乔富强老师、刘明老师、赵旭老师、王刚老师、古月老师给予的支持和帮助。由衷感谢大学三年来，所有同学、老师、辅导员、管理员，为我的学习、工作、生活提供了重要的支持和帮助。

---

## 目录

- [功能特性](#功能特性)
- [技术栈](#技术栈)
- [项目结构](#项目结构)
- [快速开始](#快速开始)
- [配置说明](#配置说明)
- [API 文档](#api-文档)
- [默认账号](#默认账号)
- [Docker 部署](#docker-部署)
- [开发指南](#开发指南)
- [项目架构](#项目架构)
- [许可证](#许可证)

---

## 功能特性

### 管理员

- **系统仪表盘**：全局数据概览（学生数、教师数、课程数、出勤率）
- **组织架构管理**：院系 → 专业 → 班级 → 学生四级组织树的增删改查
- **教师管理**：教师账号创建、角色分配（任课教师/辅导员）、批量操作
- **学生管理**：学生信息维护、班级分配、CSV 批量导入
- **课程管理**：课程创建、学分设置、排课管理
- **统计报表**：院系出勤率排名、出勤趋势分析、Excel 导出

### 教师（任课教师 + 辅导员）

- **考勤会话**：创建考勤会话、查看进行中/历史会话
- **二维码签到**：动态生成二维码，学生扫码完成签到
- **一键点名**：一键标记全班出勤，避免逐个操作
- **随机点名**：随机抽取学生点名，避免重复点名同一学生
- **手动补签**：为缺勤学生手动修改考勤状态
- **请假审批**（辅导员）：审批/驳回学生请假申请，审批通过自动联动考勤记录
- **考勤统计**：按课程/班级维度统计出勤率，支持 Excel 导出

### 学生

- **扫码签到**：扫描教师生成的二维码完成签到
- **请假申请**：提交请假申请，查看审批状态
- **个人考勤**：查看个人出勤记录与统计
- **课程表**：查看已排课程与上课时间

---

## 技术栈

| 层次 | 技术 | 版本 | 说明 |
|------|------|------|------|
| 运行时 | .NET | 10.0 | 最新版本 |
| 架构 | 垂直切片架构 (VSA) | — | 按业务功能切分，高内聚低耦合 |
| API 风格 | Minimal APIs | — | 轻量级路由注册，配合 VSA |
| CQRS | MediatR | 12.5 | 命令查询职责分离 |
| 数据验证 | FluentValidation | 11.11 | 替代 DataAnnotations，管道自动校验 |
| 对象映射 | Mapster | 1.2 | 编译时生成，性能优于 AutoMapper |
| ORM | SqlSugar Core | 5.1.4 | CodeFirst 自动建表，支持 SQLite / MySQL 切换 |
| 数据库 | SQLite / MySQL 8.0 | — | 开发零配置（SQLite），生产用 MySQL |
| 前端 | Blazor Web App | — | 静态 SSR + 交互式 Server |
| 认证 | JWT Bearer + HttpOnly Cookie | — | BFF 简化模式，防止 XSS 窃取 Token |
| 密码哈希 | BCrypt.Net-Next | 4.2.0 | BCrypt + 盐值 |
| API 文档 | Scalar | 2.6 | 现代化 OpenAPI UI |
| 日志 | Serilog | 4.3 | 结构化日志 + 异步写入 |
| 缓存 | IDistributedCache + Redis | — | 生产 Redis，开发内存缓存 |
| 监控 | OpenTelemetry | — | Traces + Metrics → Aspire Dashboard |
| 编排 | .NET Aspire | 9.3 | 本地服务编排 + 服务发现 |
| 二维码 | QRCoder | 1.8.0 | 纯 C# 生成二维码 PNG |
| Excel | ClosedXML | 0.105.0 | 统计报表导出 .xlsx |
| 测试 | xUnit | 2.9.3 | 55 个单元测试 |
| 部署 | Docker + Docker Compose | — | 多阶段构建，多容器编排 (Linux) |

---

## 项目结构

```
my-college-project/
├── src/                                         # 源代码
│   ├── Campus.Attendance.sln                    # 解决方案文件
│   ├── Directory.Build.props                    # 统一编译属性
│   │
│   ├── Campus.Attendance.Shared/                # 共享内核
│   │   ├── Entities/                            #   数据实体（11 张表）
│   │   ├── Enums/                               #   枚举类型
│   │   ├── Exceptions/                          #   自定义异常
│   │   ├── Constants/                           #   常量定义
│   │   ├── Configuration/                       #   配置强类型（DbConfig、JwtConfig）
│   │   ├── Security/                            #   安全接口（ICurrentUser、ITokenService）
│   │   ├── Responses/                           #   统一响应（ApiResponse<T>、PagedResult<T>）
│   │   ├── Contracts/                           #   共享基类（PagedQuery）
│   │   └── Features/                            #   按功能组织的 DTO
│   │       ├── Attendance/                      #     考勤相关 DTO
│   │       ├── Auth/                            #     认证相关 DTO
│   │       ├── Courses/                         #     课程与排课 DTO
│   │       ├── Leave/                           #     请假相关 DTO
│   │       ├── Organization/                    #     组织架构 DTO
│   │       ├── Statistics/                      #     统计报表 DTO
│   │       └── Users/                           #     用户管理 DTO
│   │
│   ├── Campus.Attendance.Infrastructure/        # 基础设施层
│   │   ├── Data/                                #   SqlSugar 上下文 + 数据库初始化
│   │   └── Auth/                                #   TokenService + CurrentUserService
│   │
│   ├── Campus.Attendance.Api/                   # API 入口项目 (Minimal APIs)
│   │   ├── Features/                            #   垂直切片核心目录
│   │   │   ├── Auth/                            #     认证切片（登录/登出/个人信息）
│   │   │   ├── Users/                           #     用户管理切片（学生/教师 CRUD）
│   │   │   ├── Organization/                    #     组织架构切片（院系/专业/班级）
│   │   │   ├── Courses/                         #     课程与排课切片
│   │   │   ├── Attendance/                      #     考勤切片（会话/签到/点名/二维码）
│   │   │   ├── Leave/                           #     请假切片（申请/审批）
│   │   │   └── Statistics/                      #     统计切片（统计/趋势/导出）
│   │   ├── Behaviors/                           #   MediatR 管道行为（校验）
│   │   ├── ExceptionHandler/                    #   全局异常处理 (IExceptionHandler)
│   │   └── Program.cs                           #   启动配置
│   │
│   ├── Campus.Attendance.Web/                   # Blazor Web App
│   │   ├── Components/
│   │   │   ├── Layout/                          #   布局组件
│   │   │   ├── Pages/                           #   页面组件（Admin/Teacher/Student 三端）
│   │   │   └── Ui/                              #   通用 UI 组件库
│   │   ├── Services/                            #   前端服务（ApiClient、TokenService）
│   │   └── wwwroot/                             #   静态资源（CSS 设计系统）
│   │
│   ├── Campus.Attendance.Tests/                 # 测试项目
│   │   ├── Infrastructure/                      #   测试基础设施（TestDbContext）
│   │   └── */                                   #   按功能组织的测试类
│   │
│   ├── Campus.Attendance.AppHost/               # .NET Aspire 编排器
│   └── Campus.Attendance.ServiceDefaults/       # Aspire 服务默认配置
│
├── docker/                                      # Docker 部署说明（已合并至本文档）
├── docs/                                        # 项目文档
│   ├── 最佳实践需求文档.md                        #   架构规范与最佳实践
│   ├── architecture.md                          #   架构说明
│   ├── spec.md                                  #   需求规格
│   └── tasks.md                                 #   任务跟踪
├── docker-compose.yml                           # Docker Compose 编排文件
├── .dockerignore                                # Docker 构建忽略列表
├── .gitignore
├── LICENSE                                      # MIT 许可证
└── 需求分析.docx                                 # 原始需求文档
```

---

## 快速开始

### 环境要求

| 工具 | 版本 | 说明 |
|------|------|------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 | 必需 |
| [MySQL](https://dev.mysql.com/downloads/mysql/) | 8.0+ | 仅 Release 模式需要，开发模式使用 SQLite |
| [Docker](https://www.docker.com/) | 20+ | 仅 Docker 部署时需要 |

### 克隆与运行

```bash
# 克隆仓库
git clone https://github.com/Larpx/my-college-project.git
cd my-college-project

# 还原 NuGet 依赖
dotnet restore src/Campus.Attendance.sln

# 构建解决方案（0 错误 0 警告）
dotnet build src/Campus.Attendance.sln

# 运行 API 项目
dotnet run --project src/Campus.Attendance.Api

# 运行 Web 项目（Blazor Server）
dotnet run --project src/Campus.Attendance.Web

# 运行单元测试（55 个测试）
dotnet test src/Campus.Attendance.Tests
```

### Aspire 本地编排（推荐）

```bash
# 使用 Aspire 统一启动 API + Web + Redis
dotnet run --project src/Campus.Attendance.AppHost
```

启动后打开 Aspire Dashboard 查看服务状态和追踪信息。

### 开发端口

| 项目 | HTTP | HTTPS | 说明 |
|------|------|-------|------|
| API | `http://localhost:5144` | `https://localhost:7088` | RESTful API + Scalar 文档 |
| Web | `http://localhost:5249` | `https://localhost:7250` | Blazor Web App |
| AppHost | `http://localhost:15096` | `https://localhost:17179` | Aspire Dashboard |

> 开发环境下使用 SQLite（`DataSource=attendance.db`），数据库文件自动创建，无需手动配置。启动时自动执行 CodeFirst 建表与种子数据播种。

---

## 配置说明

### API 项目配置（`Campus.Attendance.Api/appsettings.json`）

```json
{
  "Db": {
    "ProviderType": "MySQL",
    "ConnectionString": "Server=localhost;Port=3306;Database=attendance;Uid=root;Pwd=root;"
  },
  "Jwt": {
    "Issuer": "Campus.Attendance.Api",
    "Audience": "Campus.Attendance.Client",
    "SecretKey": "your-secret-key-at-least-32-chars",
    "ExpireMinutes": 120
  }
}
```

### Web 项目配置（`Campus.Attendance.Web/appsettings.json`）

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### 环境变量覆盖

所有配置项均可通过环境变量覆盖，格式为双下划线分隔层级：

| 环境变量 | 说明 |
|----------|------|
| `Db__ProviderType` | 数据库类型（`SQLite` / `MySQL`） |
| `Db__ConnectionString` | 数据库连接字符串 |
| `Jwt__SecretKey` | JWT 签名密钥（≥32 字符） |
| `Jwt__Issuer` | JWT 签发者 |
| `Jwt__Audience` | JWT 受众 |
| `Api__BaseUrl` | API 基地址（Web 项目） |
| `ConnectionStrings__redis` | Redis 连接字符串 |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry 导出端点 |

### 数据库切换

| 环境 | ProviderType | 连接字符串 | 说明 |
|------|-------------|-----------|------|
| Development | `SQLite` | `DataSource=attendance.db` | 零配置，文件自动创建 |
| Production | `MySQL` | `Server=...;Port=3306;Database=attendance;...` | 生产级数据库 |

SqlSugar CodeFirst 自动建表，启动时由 `DbInitializer.InitializeAsync()` 执行，无需手动迁移。

---

## API 文档

启动 API 项目后，访问 Scalar API 文档：

```
https://localhost:7088/scalar/v1
```

### API 版本

所有 API 端点使用 URL 路径版本控制：`/api/v1/...`

### 统一响应格式

```json
{
  "code": 200,
  "message": "操作成功",
  "data": { ... }
}
```

### 端点概览

| 模块 | 路由前缀 | 说明 | 权限 |
|------|----------|------|------|
| Auth | `api/v1/auth` | 登录、登出、当前用户信息 | 公开 / 已认证 |
| Attendance | `api/v1/sessions` | 考勤会话、二维码生成、签到、点名、关闭 | 教师/辅导员/学生 |
| Leave | `api/v1/leaves` | 请假申请、审批、记录查询 | 学生/辅导员 |
| Statistics | `api/v1/statistics` | 全局统计、排名、趋势、Excel 导出 | 管理员/教师/学生 |
| Organization | `api/v1/departments`, `/majors`, `/classes` | 组织架构管理 | 管理员 |
| Users | `api/v1/students`, `/teachers` | 学生/教师管理 | 管理员 |
| Courses | `api/v1/courses`, `/schedules` | 课程与排课管理 | 管理员/教师 |
| Profile | `api/v1/profile` | 修改密码 | 已认证 |

### 核心端点示例

```
POST   api/v1/auth/login                        # 用户登录
GET    api/v1/auth/profile                       # 获取当前用户信息

POST   api/v1/sessions                           # 创建考勤会话（教师）
GET    api/v1/sessions/active                    # 进行中的会话（教师）
GET    api/v1/sessions/history                   # 历史会话（教师，分页）
POST   api/v1/sessions/{id}/qrcode               # 生成二维码（教师）
POST   api/v1/sessions/{id}/checkin              # 学生签到
POST   api/v1/sessions/{id}/roll-call-all        # 一键点名（教师）
POST   api/v1/sessions/{id}/manual-checkin       # 手动补签（教师）
POST   api/v1/sessions/{id}/close                # 关闭会话（教师）
POST   api/v1/sessions/random-pick/{classId}     # 随机点名（教师）

POST   api/v1/leaves                             # 提交请假（学生）
GET    api/v1/leaves/my                          # 我的请假记录（学生）
GET    api/v1/leaves/counselor                   # 辅导员待审批列表
POST   api/v1/leaves/{id}/approve                # 审批通过（辅导员）
POST   api/v1/leaves/{id}/reject                 # 审批驳回（辅导员）

GET    api/v1/statistics/overview                # 全局统计（管理员）
GET    api/v1/statistics/department-ranking      # 院系排名（管理员）
GET    api/v1/statistics/attendance-trend        # 出勤趋势（管理员）
GET    api/v1/statistics/class/{classId}         # 班级统计
GET    api/v1/statistics/student/{studentId}     # 学生统计
GET    api/v1/statistics/export/session/{id}     # 导出会话考勤 Excel
GET    api/v1/statistics/export/class/{classId}  # 导出班级考勤 Excel
```

---

## 默认账号

| 角色 | 用户名 | 密码 | 说明 |
|------|--------|------|------|
| 管理员 | `admin` | `123456` | 系统管理员，拥有全部权限 |
| 任课教师 | `T001` | `123456` | 示例任课教师（张老师，高等数学） |
| 辅导员 | `T002` | `123456` | 示例辅导员（李老师，软工2201） |
| 学生 | `20220101` | `220101` | 示例学生（王同学），密码为学号后 6 位 |

> 种子数据由 `DbInitializer.SeedAsync()` 自动播种，重复执行不会产生重复数据。

---

## Docker 部署

### 前置要求

- [Docker](https://docs.docker.com/get-docker/) 24.0+
- [Docker Compose](https://docs.docker.com/compose/install/) v2.20+

### 架构概览

容器编排包含四个服务：

| 服务 | 镜像 | 端口 | 说明 |
|------|------|------|------|
| `redis` | redis:7-alpine | 6379 | Redis 分布式缓存 |
| `db` | mysql:8.0 | 3306 | MySQL 数据库，数据持久化至命名卷 |
| `api` | dotnet/aspnet:10.0 | 5000 → 8080 | RESTful API 后端，启动时自动建表与播种 |
| `web` | dotnet/aspnet:10.0 | 8080 → 8080 | Blazor Web App 前端 |

启动顺序：`db`（健康检查通过）→ `api`（健康检查通过）→ `web`

### 架构图

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  web:8080    │────▶│  api:8080    │────▶│  db:3306     │     │  redis:6379  │
│  (Blazor)    │     │  (Minimal API)│     │  (MySQL)     │◀────│  (Cache)     │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
     :8080                  :5000               :3306                  :6379
```

### 一键启动

```bash
# 构建镜像并启动容器（含 Redis）
docker-compose up -d --build

# 查看容器状态
docker-compose ps

# 查看日志
docker-compose logs -f

# 查看某服务日志
docker-compose logs -f api
docker-compose logs -f web

# 停止容器（保留数据）
docker-compose stop

# 停止并删除容器（保留数据卷）
docker-compose down
```

首次构建约需 3-5 分钟（还原 NuGet + 编译发布）。启动完成后访问：

```
http://localhost:8080
```

### 环境变量说明

#### db 服务

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `MYSQL_ROOT_PASSWORD` | root | MySQL root 密码 |
| `MYSQL_DATABASE` | attendance | 自动创建的数据库名 |

#### api 服务

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `Db__ProviderType` | MySQL | 数据库提供程序（MySQL / SQLite） |
| `Db__ConnectionString` | Server=db;Port=3306;... | 数据库连接字符串，`db` 为容器服务名 |
| `Jwt__SecretKey` | Campus.Attendance.SecretKey... | JWT 签名密钥，生产环境务必修改为随机字符串 |
| `ASPNETCORE_ENVIRONMENT` | Production | 运行环境 |
| `ASPNETCORE_URLS` | http://+:8080 | 监听地址 |
| `ConnectionStrings__redis` | redis:6379 | Redis 连接字符串 |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | — | OpenTelemetry 导出端点（可选） |

#### web 服务

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `Api__BaseUrl` | http://api:8080 | 后端 API 基址，`api` 为容器服务名 |
| `ASPNETCORE_ENVIRONMENT` | Production | 运行环境 |
| `ASPNETCORE_URLS` | http://+:8080 | 监听地址 |

### 数据持久化

MySQL 数据通过 Docker 命名卷 `mysql_data` 持久化。容器删除后数据不会丢失。

```bash
# 查看数据卷
docker volume inspect my-college-project_mysql_data
```

### 自动迁移

`api` 容器启动时自动执行：

1. `DbInitializer.InitializeAsync()` — SqlSugar CodeFirst 自动建表
2. `DbInitializer.SeedAsync()` — 播种默认管理、示例院系/专业/班级/教师/学生/课程

无需手动执行迁移命令。

### 完全清理（含数据）

```bash
# 停止并删除容器、网络、数据卷
docker-compose down -v

# 清理构建缓存，重新构建
docker-compose build --no-cache
docker-compose up -d
```

### 修改代码后重新构建

```bash
# 仅重新构建指定服务
docker-compose up -d --build api web
```

### 生产环境注意事项

1. **修改 JWT 密钥**：通过环境变量 `JWT_SECRET_KEY` 注入至少 32 字符的随机字符串
2. **修改 MySQL 密码**：通过环境变量覆盖 `Db__ConnectionString` 中的密码
3. **移除端口暴露**：生产环境移除 `db` 和 `redis` 服务的 `ports` 映射，仅保留内部网络通信
4. **配置反向代理**：在 `web` 服务前配置 Nginx 反向代理，启用 HTTPS
5. **资源限制**：根据服务器配置添加 `deploy.resources.limits` 限制 CPU 和内存
6. **日志收集**：配置 Serilog sink 写入外部日志系统（如 Elasticsearch）

### 故障排查

**容器启动失败**
```bash
docker-compose ps           # 查看容器状态
docker-compose logs api     # 查看 API 日志
docker-compose logs web     # 查看 Web 日志
```

**数据库连接失败**
1. 确认 `db` 容器健康：`docker-compose ps db` 状态应为 `healthy`
2. 确认连接字符串中 `Server=db` 与服务名一致

**Web 无法访问 API**
1. 确认 `api` 容器健康：`docker-compose ps api`
2. 确认 `web` 服务的 `Api__BaseUrl=http://api:8080` 与环境变量一致

---

## 开发指南

### 代码规范

- **命名约定**：类/方法 PascalCase，参数/变量 camelCase，私有字段 `_camelCase`，异步方法 `Async` 后缀，接口 `I` 前缀
- **依赖注入**：构造函数注入，配置使用 `IOptions<T>`
- **异步优先**：公共方法异步优先，所有 Handler 和 Endpoint 接收 `CancellationToken`
- **时间处理**：统一使用 `DateTime.UtcNow`
- **错误处理**：业务异常使用 `BusinessException`，系统异常统一返回 `ProblemDetails` (RFC 7807)
- **日志记录**：使用 `ILogger<T>`，结构化日志
- **编译要求**：`TreatWarningsAsErrors=true`，`Nullable=enable`，`GenerateDocumentationFile=true`，0 错误 0 警告

### 架构原则

- **垂直切片**：每个功能模块（Auth/Attendance/Leave 等）代码集中在同一目录下
- **CQRS**：Command 和 Query 分离，通过 MediatR 中介者模式解耦
- **防重复**：共享代码提取到 `Shared` 项目，禁止跨切片复制粘贴
- **切片间通信**：通过 MediatR 发送请求，禁止直接引用内部 Handler

### 提交规范

```
feat: 新增功能
fix: 修复缺陷
refactor: 重构代码
docs: 文档更新
test: 测试相关
chore: 构建/工具变更
```

### 测试规范

- **框架**：xUnit
- **模式**：AAA（Arrange-Act-Assert）
- **命名**：`Method_Scenario_ExpectedResult`
- **覆盖**：Handler 业务逻辑全覆盖，包含边界条件和异常场景
- **运行**：`dotnet test src/Campus.Attendance.Tests`

---

## 项目架构

### 垂直切片架构 (VSA)

```
┌──────────────────────────────────────────────────┐
│              Campus.Attendance.Web                │
│           Blazor Web App（静态 SSR + 交互 Server） │
│    Pages / Layout / Ui Components / ApiClient     │
│        JWT 存储在 HttpOnly Cookie (BFF 模式)       │
└──────────────────────┬───────────────────────────┘
                       │ HttpClient + Bearer Token
                       ▼
┌──────────────────────────────────────────────────┐
│              Campus.Attendance.Api                │
│            Minimal APIs + 垂直切片                  │
│  ┌───────────────────────────────────────────┐   │
│  │  Features/                                │   │
│  │  ├── Auth/         (Login/Logout/Profile) │   │
│  │  ├── Users/        (Student/Teacher CRUD) │   │
│  │  ├── Organization/ (Dept/Major/Class)     │   │
│  │  ├── Courses/      (Course/Schedule)      │   │
│  │  ├── Attendance/   (Session/CheckIn/QR)   │   │
│  │  ├── Leave/        (Apply/Approve)        │   │
│  │  └── Statistics/   (Stats/Trend/Export)   │   │
│  └───────────────────────────────────────────┘   │
│  MediatR + FluentValidation + Mapster             │
└──────────────────────┬───────────────────────────┘
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
│   Shared     │ │Infrastructure│ │  ServiceDefaults │
│ 实体/DTO/枚举 │ │SqlSugar/Redis│ │  OTEL/健康检查     │
│ 异常/常量/接口 │ │Token/用户上下文│ │  服务发现/弹性     │
└──────────────┘ └──────────────┘ └──────────────────┘
```

### 依赖关系

```
Api ──▶ Shared + Infrastructure + ServiceDefaults
Web ──▶ Shared + Infrastructure + ServiceDefaults
Infrastructure ──▶ Shared
Tests ──▶ Shared + Infrastructure + Api
AppHost ──▶ Api + Web（编排引用）
```

### 认证流程（BFF 模式）

```
1. 用户提交用户名/密码 → POST /api/v1/auth/login
2. AuthHandler 验证凭据（BCrypt 哈希比对）
3. TokenService 签发 JWT（包含 UserId、Role、过期时间）
4. 服务端将 JWT 写入 HttpOnly Cookie（campus_attendance_token）
5. 后续请求自动携带 Cookie，服务端拦截器提取 Token 并附加 Authorization: Bearer 头
6. API 层 JwtBearer 中间件验证令牌，注入 ICurrentUser 上下文
7. Endpoint 通过 RequireAuthorization 策略实现角色权限控制
```

### 安全设计

- **密码存储**：BCrypt 哈希 + 自动盐值，不可逆
- **JWT 认证**：无状态令牌，ClockSkew 1 分钟，支持 Issuer/Audience/Lifetime 签名验证
- **BFF 模式**：JWT 存储在 HttpOnly Cookie，禁止 JavaScript 访问，防止 XSS 窃取
- **安全响应头**：`X-Content-Type-Options: nosniff`、`X-Frame-Options: DENY`、`X-XSS-Protection: 1`
- **全局异常处理**：系统异常禁止暴露细节，返回标准 ProblemDetails (RFC 7807)
- **角色授权策略**：`RequireAdmin`、`RequireTeacher`、`RequireStudent`、`RequireCounselor`
- **数据隔离**：学生只能查看自己的统计数据，教师只能操作自己的考勤会话
- **防伪令牌**：Blazor 表单使用内置 `Antiforgery` 中间件
- **输入验证**：FluentValidation 管道自动校验，失败返回 400 + 校验错误详情
- **限流**：使用 `System.Threading.RateLimiting` 中间件
- **响应压缩**：Brotli + Gzip
- **CORS**：明确配置白名单，HTTPS 强制重定向 + HSTS

---

## 许可证

[MIT License](LICENSE) &copy; 2026 Larpx
