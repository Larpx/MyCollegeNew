# 学生考勤管理系统 - 我的毕业设计重构

特别感谢乔富强老师、刘明老师、赵旭老师、王刚老师、古月老师给予我的支持和帮助。
由衷感谢大学三年来，所有我的同学、老师、辅导员、管理员，为我的学习、工作、生活提供了重要的支持和帮助。



基于 **.NET 10 + Blazor Server + SqlSugar** 的学生考勤管理系统，支持管理员、教师（任课教师/辅导员）、学生三端功能，涵盖二维码签到、一键点名、请假审批、统计报表等核心场景。

## 功能特性

- **三端角色**：管理员（系统管理）、教师（任课教师 + 辅导员）、学生（签到与请假）
- **二维码签到**：教师发起考勤会话后动态生成二维码，学生扫码完成签到
- **一键点名**：教师随机抽取学生点名，避免重复点名同一学生
- **请假审批**：学生提交请假申请，辅导员审批通过后自动联动考勤记录
- **统计报表**：按课程/班级/学生维度统计出勤率，支持 Excel 导出
- **移动端适配**：Blazor Server 响应式布局，支持手机浏览器访问
- **组织架构管理**：院系 → 专业 → 班级 → 学生四级组织树
- **JWT 认证**：基于 JWT Bearer Token 的无状态认证，BCrypt 密码哈希

## 技术栈

| 层次 | 技术 | 说明 |
|------|------|------|
| 运行时 | .NET 10 | 最新 LTS 版本 |
| ORM | SqlSugar Core | CodeFirst 自动建表，支持 SQLite / MySQL 切换 |
| 数据库 | SQLite（Debug）/ MySQL 8.0（Release） | 开发零配置，生产用 MySQL |
| 前端 | Blazor Server | 服务端渲染，实时交互 |
| 认证 | JWT Bearer Token | 无状态认证，BCrypt 密码哈希 |
| API | ASP.NET Core Web API | RESTful 风格，Swagger 文档 |
| 部署 | Docker + Docker Compose | 多阶段构建，三容器编排 |
| 二维码 | QRCoder | 纯 C# 生成二维码 PNG |
| Excel 导出 | ClosedXML | 统计报表导出 .xlsx |

## 项目结构

```
my-college-project/
├── src/                                # 源代码
│   ├── Campus.Attendance.sln           # 解决方案文件
│   ├── Directory.Build.props           # 统一编译属性（TreatWarningsAsErrors）
│   ├── Campus.Attendance.Core/         # 核心层：实体、枚举、接口、配置
│   ├── Campus.Attendance.Models/       # DTO 层：请求/响应数据模型
│   ├── Campus.Attendance.Services/     # 服务层：业务逻辑实现
│   ├── Campus.Attendance.Api/          # API 层：RESTful 端点（端口 5000）
│   ├── Campus.Attendance.Web/          # Web 层：Blazor Server UI（端口 8080）
│   └── Campus.Attendance.Tests/        # 测试层：xUnit 单元测试
├── legacy/                             # 原 WebForms 遗留代码（已归档，不参与编译）
├── docker/                             # Docker 部署说明
│   └── README.md
├── docs/                               # 项目文档
│   └── architecture.md                 # 架构说明
├── docker-compose.yml                  # Docker Compose 编排文件
├── .dockerignore                       # Docker 构建忽略列表
├── .gitignore
├── LICENSE
└── 需求分析.docx                       # 原始需求文档
```

## 本地开发

### 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- （可选）[MySQL 8.0](https://dev.mysql.com/downloads/mysql/) — 仅 Release 模式需要

### 命令

```bash
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
| Web（Blazor Server） | http://localhost:5249 | https://localhost:7250 |
| Api（Web API） | http://localhost:5144 | https://localhost:7088 |

> 开发环境下使用 SQLite（`DataSource=attendance.db`），数据库文件自动创建，无需手动配置。

## Docker 部署

```bash
# 一键启动（构建镜像 + 启动容器）
docker-compose up -d --build

# 访问应用
# http://localhost:8080
```

部署详情请参阅 [docker/README.md](docker/README.md)。

## 默认账号

| 角色 | 用户名 | 密码 | 说明 |
|------|--------|------|------|
| 管理员 | `admin` | `123456` | 系统管理员，拥有全部权限 |
| 任课教师 | `T001` | `123456` | 示例任课教师（张老师，高等数学） |
| 辅导员 | `T002` | `123456` | 示例辅导员（李老师，软工2201） |
| 学生 | `20220101` | `220101` | 示例学生（王同学），密码为学号后 6 位 |

> 种子数据由 `DbInitializer.SeedAsync()` 自动播种，重复执行不会产生重复数据。

## 数据库说明

系统通过 `DbConfig.ProviderType` 配置项切换数据库：

| 环境 | ProviderType | 连接字符串 | 说明 |
|------|-------------|-----------|------|
| Development | SQLite | `DataSource=attendance.db` | 零配置，文件自动创建 |
| Production | MySQL | `Server=...;Port=3306;Database=attendance;...` | 生产级数据库 |

切换方式：
- 开发环境：`appsettings.Development.json` 中 `Db:ProviderType=SQLite`
- 生产环境：环境变量 `Db__ProviderType=MySQL` + `Db__ConnectionString=...`

SqlSugar CodeFirst 自动建表，启动时由 `DbInitializer.InitializeAsync()` 执行，无需手动迁移。

## 架构说明

详细的架构设计、数据流、认证流程请参阅 [docs/architecture.md](docs/architecture.md)。

## 许可证

详见 [LICENSE](LICENSE)。
