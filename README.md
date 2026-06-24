# 校园考勤管理系统

基于 **.NET 10 + Blazor Server + SqlSugar** 的校园考勤管理系统，支持管理员、教师（任课教师/辅导员）、学生三端功能，涵盖二维码签到、一键点名、请假审批、统计报表等核心场景。

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
- **学生管理**：学生信息维护、班级分配、批量导入
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
| ORM | SqlSugar Core | 5.1.4 | CodeFirst 自动建表，支持 SQLite / MySQL 切换 |
| 数据库 | SQLite / MySQL 8.0 | — | 开发零配置（SQLite），生产用 MySQL |
| 前端 | Blazor Server | — | 服务端渲染，实时交互，移动端适配 |
| 认证 | JWT Bearer Token | — | 无状态认证，BCrypt 密码哈希 |
| API | ASP.NET Core Web API | — | RESTful 风格，Swagger 文档 |
| 二维码 | QRCoder | 1.8.0 | 纯 C# 生成二维码 PNG |
| Excel | ClosedXML | 0.105.0 | 统计报表导出 .xlsx |
| 密码哈希 | BCrypt.Net-Next | 4.2.0 | BCrypt + 盐值 |
| 测试 | xUnit + Moq | 2.9.3 / 4.20.72 | AAA 模式单元测试 |
| 部署 | Docker + Docker Compose | — | 多阶段构建，三容器编排 |

---

## 项目结构

```
my-college-project/
├── src/                                    # 源代码
│   ├── Campus.Attendance.sln               # 解决方案文件
│   ├── Directory.Build.props               # 统一编译属性（TreatWarningsAsErrors、Nullable、GenerateDoc）
│   │
│   ├── Campus.Attendance.Core/             # 核心层：实体、枚举、接口、配置、异常、响应
│   │   ├── Configuration/                  #   数据库配置（DbConfig、IDbContext）
│   │   ├── Constants/                      #   常量定义（AttendanceConstants、MessageConstants）
│   │   ├── Entities/                       #   数据实体（12 个实体类）
│   │   ├── Enums/                          #   枚举类型（UserRole、AttendanceStatus、LeaveStatus 等）
│   │   ├── Exceptions/                     #   自定义异常（BusinessException）
│   │   ├── Responses/                      #   统一响应（ApiResponse<T>、PagedResult<T>）
│   │   └── Security/                       #   安全接口（ICurrentUser、ITokenService、JwtConfig）
│   │
│   ├── Campus.Attendance.Models/           # 模型层：请求/响应 DTO
│   │   ├── Attendance/                     #   考勤相关 DTO
│   │   ├── Auth/                           #   认证相关 DTO
│   │   ├── Courses/                        #   课程与排课 DTO
│   │   ├── Leave/                          #   请假相关 DTO
│   │   ├── Organization/                   #   组织架构 DTO
│   │   ├── Statistics/                     #   统计报表 DTO
│   │   └── Users/                          #   用户管理 DTO
│   │
│   ├── Campus.Attendance.Services/         # 服务层：业务逻辑实现
│   │   ├── Attendance/                     #   考勤服务（会话、签到、点名、二维码）
│   │   ├── Auth/                           #   认证服务（登录、令牌、当前用户）
│   │   ├── Courses/                        #   课程与排课服务
│   │   ├── Data/                           #   数据初始化（DbInitializer、SqlSugarDbContext）
│   │   ├── Leave/                          #   请假服务
│   │   ├── Organization/                   #   组织架构服务
│   │   ├── Statistics/                     #   统计报表服务
│   │   └── Users/                          #   用户管理服务
│   │
│   ├── Campus.Attendance.Api/              # API 层：RESTful 端点
│   │   ├── Controllers/                    #   12 个控制器
│   │   ├── Middleware/                     #   全局异常处理 + 安全头中间件
│   │   └── Program.cs                      #   启动配置（JWT、Swagger、DI 注册）
│   │
│   ├── Campus.Attendance.Web/              # Web 层：Blazor Server UI
│   │   ├── Components/
│   │   │   ├── Layout/                     #   布局组件（Admin/Teacher/Student/Login）
│   │   │   ├── Pages/                      #   页面组件（Admin/Teacher/Student 三端）
│   │   │   └── Ui/                         #   通用 UI 组件（Badge、Button、Card、Modal 等）
│   │   ├── Services/                       #   前端服务（ApiClient、TokenService、AuthStateProvider）
│   │   └── wwwroot/                        #   静态资源（CSS 设计系统、Bootstrap）
│   │
│   └── Campus.Attendance.Tests/            # 测试层：xUnit 单元测试
│       ├── Attendance/                     #   考勤服务测试
│       ├── Auth/                           #   认证服务测试
│       ├── Courses/                        #   课程与排课测试
│       ├── Extensions/                     #   扩展方法测试
│       ├── Leave/                          #   请假服务测试
│       ├── Organization/                   #   组织架构测试
│       ├── Statistics/                     #   统计服务测试
│       └── Users/                          #   用户服务测试
│
├── legacy/                                 # 原 WebForms 遗留代码（已归档，不参与编译）
├── docker/                                 # Docker 部署说明
├── docs/                                   # 项目文档
│   ├── architecture.md                     #   架构说明
│   ├── spec.md                             #   需求规格
│   ├── checklist.md                        #   检查清单
│   └── tasks.md                            #   任务跟踪
├── docker-compose.yml                      # Docker Compose 编排文件
├── .dockerignore                           # Docker 构建忽略列表
├── .gitignore
├── LICENSE                                 # MIT 许可证
└── 需求分析.docx                            # 原始需求文档
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

# 运行 Web 项目（Blazor Server，默认 SQLite 数据库）
dotnet run --project src/Campus.Attendance.Web

# 运行 API 项目（可选，Web 通过 HttpClient 调用 API）
dotnet run --project src/Campus.Attendance.Api

# 运行单元测试
dotnet test src/Campus.Attendance.sln
```

### 开发端口

| 项目 | HTTP | HTTPS |
|------|------|-------|
| Web（Blazor Server） | `http://localhost:5249` | `https://localhost:7250` |
| Api（Web API） | `http://localhost:5144` | `https://localhost:7088` |

> 开发环境下使用 SQLite（`DataSource=attendance.db`），数据库文件自动创建，无需手动配置。启动时 `DbInitializer` 自动执行 CodeFirst 建表与种子数据播种。

---

## 配置说明

### API 项目配置（`Campus.Attendance.Api/appsettings.json`）

```json
{
  "Db": {
    "ProviderType": "MySQL",                          // 数据库类型：SQLite / MySQL
    "ConnectionString": "Server=localhost;Port=3306;Database=attendance;Uid=root;Pwd=root;"
  },
  "Jwt": {
    "Issuer": "Campus.Attendance.Api",                // JWT 签发者
    "Audience": "Campus.Attendance.Client",            // JWT 受众
    "SecretKey": "your-secret-key-here",               // JWT 签名密钥（生产环境务必通过环境变量覆盖）
    "ExpireMinutes": 120                               // 令牌过期时间（分钟）
  }
}
```

### Web 项目配置（`Campus.Attendance.Web/appsettings.json`）

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5000"                 // 后端 API 地址
  }
}
```

### 环境变量覆盖

所有配置项均可通过环境变量覆盖，格式为双下划线分隔层级：

| 环境变量 | 说明 |
|----------|------|
| `Db__ProviderType` | 数据库类型（`SQLite` / `MySQL`） |
| `Db__ConnectionString` | 数据库连接字符串 |
| `Jwt__SecretKey` | JWT 签名密钥 |
| `Jwt__Issuer` | JWT 签发者 |
| `Jwt__Audience` | JWT 受众 |
| `Api__BaseUrl` | API 基地址（Web 项目） |

### 数据库切换

| 环境 | ProviderType | 连接字符串 | 说明 |
|------|-------------|-----------|------|
| Development | `SQLite` | `DataSource=attendance.db` | 零配置，文件自动创建 |
| Production | `MySQL` | `Server=...;Port=3306;Database=attendance;...` | 生产级数据库 |

SqlSugar CodeFirst 自动建表，启动时由 `DbInitializer.InitializeAsync()` 执行，无需手动迁移。

---

## API 文档

启动 API 项目后，访问 Swagger 文档：`http://localhost:5144/swagger`

### 统一响应格式

```json
{
  "code": 200,
  "message": "操作成功",
  "data": { ... }
}
```

### 端点概览

| 控制器 | 路由前缀 | 说明 | 权限 |
|--------|----------|------|------|
| AuthController | `api/auth` | 登录、登出、当前用户信息 | 公开 / 已认证 |
| SessionsController | `api/sessions` | 考勤会话、二维码生成、签到、点名、关闭 | 教师/辅导员/学生 |
| LeavesController | `api/leaves` | 请假申请、审批、记录查询 | 学生/辅导员 |
| StatisticsController | `api/statistics` | 全局统计、排名、趋势、Excel 导出 | 管理员/教师/学生 |
| DepartmentsController | `api/departments` | 院系管理 | 管理员 |
| MajorsController | `api/majors` | 专业管理 | 管理员 |
| ClassesController | `api/classes` | 班级管理 | 管理员 |
| StudentsController | `api/students` | 学生管理 | 管理员 |
| TeachersController | `api/teachers` | 教师管理 | 管理员 |
| CoursesController | `api/courses` | 课程管理 | 管理员/教师 |
| SchedulesController | `api/schedules` | 排课管理 | 管理员/教师 |
| ProfileController | `api/profile` | 个人信息修改 | 已认证 |

### 核心端点示例

```
POST   api/auth/login                        # 用户登录
GET    api/auth/profile                       # 获取当前用户信息

POST   api/sessions                           # 创建考勤会话（教师）
GET    api/sessions/active                    # 进行中的会话（教师）
GET    api/sessions/history                   # 历史会话（教师，分页）
POST   api/sessions/{id}/qrcode               # 生成二维码（教师）
POST   api/sessions/{id}/checkin              # 学生签到
POST   api/sessions/{id}/roll-call-all        # 一键点名（教师）
POST   api/sessions/{id}/manual-checkin       # 手动补签（教师）
POST   api/sessions/{id}/close                # 关闭会话（教师）
POST   api/sessions/random-pick/{classId}     # 随机点名（教师）

POST   api/leaves                             # 提交请假（学生）
GET    api/leaves/my                          # 我的请假记录（学生）
GET    api/leaves/counselor                   # 辅导员待审批列表
POST   api/leaves/{id}/approve                # 审批通过（辅导员）
POST   api/leaves/{id}/reject                 # 审批驳回（辅导员）

GET    api/statistics/overview                # 全局统计（管理员）
GET    api/statistics/department-ranking      # 院系排名（管理员）
GET    api/statistics/attendance-trend        # 出勤趋势（管理员）
GET    api/statistics/class/{classId}         # 班级统计
GET    api/statistics/student/{studentId}     # 学生统计
GET    api/statistics/export/session/{id}     # 导出会话考勤 Excel
GET    api/statistics/export/class/{classId}  # 导出班级考勤 Excel
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

### 架构

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   web:8080  │────▶│  api:8080   │────▶│  db:3306    │
│  (Blazor)   │     │  (Web API)  │     │  (MySQL)    │
└─────────────┘     └─────────────┘     └─────────────┘
     :8080                :5000               :3306
```

### 一键启动

```bash
# 构建镜像并启动容器
docker-compose up -d --build

# 查看容器状态
docker-compose ps

# 查看日志
docker-compose logs -f

# 停止并删除容器
docker-compose down

# 停止并删除容器 + 数据卷
docker-compose down -v
```

### 服务说明

| 服务 | 镜像 | 端口映射 | 健康检查 |
|------|------|----------|----------|
| `db` | `mysql:8.0` | `3306:3306` | `mysqladmin ping` |
| `api` | 自建（多阶段构建） | `5000:8080` | `curl http://localhost:8080` |
| `web` | 自建（多阶段构建） | `8080:8080` | `curl http://localhost:8080` |

启动顺序：`db`（healthy）→ `api`（healthy）→ `web`

### 访问地址

- 前端界面：`http://localhost:8080`
- API 接口：`http://localhost:5000`

> 部署详情请参阅 [docker/README.md](docker/README.md)。

---

## 开发指南

### 代码规范

- **命名约定**：类/方法 PascalCase，参数/变量 camelCase，私有字段 `_camelCase`，异步方法 `Async` 后缀，接口 `I` 前缀
- **依赖注入**：构造函数注入，配置使用 `IOptions<T>`
- **异步优先**：公共方法异步优先，禁止同步阻塞异步方法
- **时间处理**：统一使用 `DateTime.UtcNow`
- **错误处理**：使用 `BusinessException` 抛出业务异常，系统异常禁止暴露 `ex.Message`
- **日志记录**：使用 `ILogger<T>`，结构化日志 `_logger.LogInformation("用户 {UserId} 执行操作", userId)`
- **编译要求**：`TreatWarningsAsErrors=true`，`Nullable=enable`，`GenerateDocumentationFile=true`，0 错误 0 警告

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

- **框架**：xUnit + Moq
- **模式**：AAA（Arrange-Act-Assert）
- **命名**：`Method_Scenario_ExpectedResult`
- **覆盖**：业务服务层全覆盖，包含边界条件和异常场景
- **运行**：`dotnet test src/Campus.Attendance.sln`

---

## 项目架构

### 分层架构

```
┌──────────────────────────────────────────────────┐
│                  Campus.Attendance.Web            │
│              Blazor Server（UI 层）                │
│    Pages / Layout / Ui Components / ApiClient     │
└──────────────────────┬───────────────────────────┘
                       │ HttpClient
                       ▼
┌──────────────────────────────────────────────────┐
│                  Campus.Attendance.Api            │
│              Web API（控制器层）                    │
│    Controllers / Middleware / Swagger              │
└──────────────────────┬───────────────────────────┘
                       │ DI
                       ▼
┌──────────────────────────────────────────────────┐
│              Campus.Attendance.Services           │
│                服务层（业务逻辑）                    │
│    Attendance / Auth / Leave / Statistics / ...    │
└──────────────────────┬───────────────────────────┘
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
┌──────────────┐ ┌──────────┐ ┌──────────────┐
│    Core      │ │  Models  │ │  SqlSugar    │
│ 实体/枚举/接口│ │  DTO 层  │ │   DbContext  │
└──────────────┘ └──────────┘ └──────────────┘
```

### 依赖关系

```
Api ──▶ Core + Models + Services
Web ──▶ Core + Models + Services
Services ──▶ Core + Models
Models ──▶ Core
Tests ──▶ Core + Models + Services
Core ──▶ （无外部依赖，仅 SqlSugarCore）
```

### 认证流程

```
1. 用户提交用户名/密码 → POST api/auth/login
2. AuthService 验证凭据（BCrypt 哈希比对）
3. TokenService 签发 JWT（包含 UserId、Role、过期时间）
4. 客户端存储 Token，后续请求携带 Authorization: Bearer {token}
5. API 层 JwtBearer 中间件验证令牌，注入 ICurrentUser 上下文
6. 控制器通过 [Authorize(Roles = "...")] 实现角色权限控制
```

### 安全设计

- **密码存储**：BCrypt 哈希 + 自动盐值，不可逆
- **JWT 认证**：无状态令牌，ClockSkew 1 分钟，支持 Issuer/Audience/Lifetime 签名验证
- **安全响应头**：`X-Content-Type-Options: nosniff`、`X-Frame-Options: DENY`、`X-XSS-Protection: 1`
- **全局异常处理**：系统异常禁止暴露 `ex.Message`，统一返回通用提示
- **角色授权策略**：`RequireAdmin`、`RequireTeacher`、`RequireStudent`、`RequireCounselor`
- **数据隔离**：学生只能查看自己的统计数据，教师只能操作自己的考勤会话

---

## 许可证

[MIT License](LICENSE) &copy; 2026 Larpx
