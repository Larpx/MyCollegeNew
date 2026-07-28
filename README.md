# 校园考勤管理系统

[中文](README.md) | [English](README.en.md)

基于 **.NET 10 + ASP.NET Core 10** 的校园考勤管理系统，采用 **垂直切片架构 (VSA) + CQRS + Minimal APIs**，支持管理员、教师（任课教师 / 辅导员）、学生三端。

> 特别感谢乔富强老师、刘明老师、赵旭老师、王刚老师、古月老师给予的支持和帮助。

## 文档入口

| 文档 | 受众 |
|------|------|
| [用户使用说明](docs/用户使用说明.md) | 最终用户 |
| [开发说明](docs/开发说明.md) | 二次开发者 |

## 能做什么

- **管理员**：组织架构（院系 → 专业 → 班级）、师生管理、课程排课、全校考勤统计与 Excel 导出。
- **教师**：考勤会话、二维码签到（30 秒刷新）、一键点名、随机点名、手动补签、请假审批、考勤统计。
- **学生**：扫码签到、请假申请、个人考勤、课程表。

## 程序怎样工作

1. 管理员建好组织架构与账号、开设课程并排课。
2. 教师上课时创建考勤会话，系统生成动态二维码，学生扫码签到（也可一键点名 / 手动补签）。
3. 学生扫码签到或提交请假，课后查看个人考勤与课表。

## 快速开始

```bash
git clone <仓库地址>
cd my-college-project
dotnet restore MyCollegeNew.sln
dotnet build MyCollegeNew.sln

# 运行 API
dotnet run --project src/MyCollegeNew.Api
# 运行 Web（教师 / 学生端）
dotnet run --project src/MyCollegeNew.Web
# 运行 Admin（管理员后台）
dotnet run --project src/MyCollegeNew.Admin
# 运行测试
dotnet test src/MyCollegeNew.Tests
# Aspire 本地编排
dotnet run --project src/MyCollegeNew.AppHost
# Docker 部署
docker-compose up -d --build
```

> 本地开发敏感配置使用 `dotnet user-secrets` 管理；Docker 部署通过环境变量注入。详见 [开发说明](docs/开发说明.md)。

## 项目说明

| 项 | 说明 |
|------|------|
| 运行时 | .NET 10 |
| 架构 | 垂直切片架构 (VSA) + CQRS + Minimal APIs |
| 前端 | Blazor Web App（SSR + InteractiveServer） |
| ORM | SqlSugar Core（SQLite / MySQL） |
| 认证 | JWT Bearer + HttpOnly Cookie (BFF) |
| 编排 | .NET Aspire |
| 测试 | xUnit |
| 部署 | Docker Compose（Linux） |

## 目录示意

```text
my-college-project/
├── MyCollegeNew.sln
├── docs/        # 文档
└── src/         # 业务与测试工程
```

## 许可证

[MIT License](LICENSE) &copy; 2026 Larpx
