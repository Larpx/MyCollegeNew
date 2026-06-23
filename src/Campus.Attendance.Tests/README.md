# Campus.Attendance.Tests

测试层，基于 xUnit 的单元测试项目，覆盖服务层核心业务逻辑。

## 项目职责

- 对 Services 层各服务进行单元测试
- 使用内存 SQLite 数据库隔离测试（TestDbContext）
- 遵循 AAA 模式（Arrange-Act-Assert）
- 覆盖正常流程、边界条件与异常场景

## 关键目录与类型

| 目录 | 关键类型 | 说明 |
|------|---------|------|
| `Infrastructure/` | `TestDbContext` | 测试用内存 SQLite 数据库上下文，隔离测试数据 |
| `Auth/` | `AuthServiceTests` | 登录认证测试（管理员/教师/学生/密码错误） |
| `Attendance/` | `AttendanceServiceTests` | 考勤会话、签到、点名测试 |
| `Leave/` | `LeaveServiceTests` | 请假申请与审批流程测试 |
| `Courses/` | `CourseServiceTests`, `ScheduleServiceTests` | 课程与排课管理测试 |
| `Organization/` | `OrganizationServiceTests` | 院系/专业/班级管理测试 |
| `Users/` | `UserServiceTests` | 学生/教师管理、密码重置测试 |
| `Statistics/` | `StatisticsServiceTests` | 出勤统计测试 |

## 依赖关系

- 引用 `Campus.Attendance.Core`、`Campus.Attendance.Models`、`Campus.Attendance.Services`
- NuGet 包：`xunit`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk`

## 运行测试

```bash
dotnet test src/Campus.Attendance.Tests/Campus.Attendance.Tests.csproj
```
