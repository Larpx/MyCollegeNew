# 校园考勤管理系统

基于 **.NET 10 + ASP.NET Core 10** 的校园考勤管理系统，采用**垂直切片架构 (VSA) + CQRS + Minimal APIs**，支持管理员、教师（任课教师/辅导员）、学生三端功能。

> 特别感谢乔富强老师、刘明老师、赵旭老师、王刚老师、古月老师给予的支持和帮助。

---

## 功能特性

### 管理员
- 系统仪表盘：全局数据概览（学生数、教师数、课程数、出勤率）
- 组织架构管理：院系 → 专业 → 班级 → 学生四级组织树
- 教师管理：账号创建、角色分配（任课教师/辅导员）
- 学生管理：信息维护、班级分配、CSV 批量导入
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

> 详细说明请参阅 [docs/](docs/) 目录下的文档。

---

## 默认账号

| 角色 | 用户名 | 密码 |
|------|--------|------|
| 管理员 | `admin` | `123456` |
| 任课教师 | `T001` | `123456` |
| 辅导员 | `T002` | `123456` |
| 学生 | `20220101` | `220101` |

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
