# Campus.Attendance.Tests

测试项目，基于 xUnit 的单元测试，覆盖 API Features 中每个切片的 Handler 业务逻辑。

## 项目职责

- 对垂直切片中的 Handler 进行单元测试
- 使用内存 SQLite 数据库隔离测试（TestDbContext）
- 遵循 AAA 模式（Arrange-Act-Assert）
- 覆盖正常流程、边界条件与异常场景

## 关键目录与类型

| 目录 | 说明 |
|------|------|
| `Infrastructure/TestDbContext.cs` | 测试用内存 SQLite 数据库上下文 |
| `Auth/AuthServiceTests.cs` | 登录认证测试 |
| `Attendance/AttendanceServiceTests.cs` | 考勤会话、签到、点名测试 |
| `Leave/LeaveServiceTests.cs` | 请假申请与审批流程测试 |
| `Courses/CourseServiceTests.cs` | 课程管理测试 |
| `Courses/ScheduleServiceTests.cs` | 排课管理测试 |
| `Organization/OrganizationServiceTests.cs` | 院系/专业/班级管理测试 |
| `Users/UserServiceTests.cs` | 学生/教师管理测试 |
| `Statistics/StatisticsServiceTests.cs` | 出勤统计测试 |
| `Extensions/` | 扩展方法与枚举测试 |

## 依赖关系

- 引用 `Campus.Attendance.Shared`、`Campus.Attendance.Infrastructure`、`Campus.Attendance.Api`
- NuGet 包：`xunit`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk`、`Moq`、`coverlet.collector`

## 运行测试

```bash
# 运行全部 55 个测试
dotnet test src/Campus.Attendance.Tests

# 带覆盖率
dotnet test src/Campus.Attendance.Tests --collect:"XPlat Code Coverage"
```

## 测试覆盖

| 测试类 | 覆盖功能 | 测试数量 |
|--------|----------|---------|
| `AuthServiceTests` | 登录认证 | ~8 |
| `AttendanceServiceTests` | 考勤会话/签到/点名 | ~15 |
| `LeaveServiceTests` | 请假申请/审批 | ~8 |
| `CourseServiceTests` | 课程管理 | ~5 |
| `ScheduleServiceTests` | 排课管理 | ~6 |
| `OrganizationServiceTests` | 组织架构 | ~6 |
| `UserServiceTests` | 用户管理 | ~5 |
| `StatisticsServiceTests` | 考勤统计 | ~3 |
| `Extensions` | 扩展方法/枚举 | ~3 |
