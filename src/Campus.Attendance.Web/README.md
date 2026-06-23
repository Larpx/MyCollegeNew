# Campus.Attendance.Web

Web 层，基于 Blazor Server 的前端 UI，通过 HttpClient 调用后端 API。

## 项目职责

- 提供 Blazor Server 交互式 UI（服务端渲染 + SignalR 实时通信）
- 实现三端页面（管理员 / 教师 / 学生）
- 封装 ApiClient 统一调用后端 API，自动附加 JWT Token
- 自定义 AuthenticationStateProvider，基于 JWT Claims 构建认证状态
- 响应式布局，适配桌面与移动端浏览器

## 关键目录与类型

| 目录 | 关键类型 | 说明 |
|------|---------|------|
| `Components/Pages/Admin/` | `Dashboard`, `Students`, `Teachers`, `Departments`, `Courses`, `Statistics` | 管理员页面 |
| `Components/Pages/Teacher/` | `Dashboard`, `Session`, `RollCall`, `Attendance`, `Leaves`, `Courses` | 教师页面（考勤发起/点名/请假审批） |
| `Components/Pages/Student/` | `Home`, `CheckIn`, `Attendance`, `Leave`, `Schedule`, `Profile` | 学生页面（签到/请假/课表） |
| `Components/Pages/` | `Login` | 统一登录页 |
| `Components/Layout/` | `MainLayout`, `AdminLayout`, `TeacherLayout`, `StudentLayout`, `LoginLayout`, `NavMenu` | 布局组件与导航菜单 |
| `Components/Ui/` | `Button`, `Card`, `Input`, `Modal`, `Table`, `Pagination`, `Badge`, `Icon`, `EmptyState`, `PageHeader` | 通用 UI 组件库 |
| `Services/` | `ApiClient`, `CustomAuthStateProvider`, `TokenService` | API 客户端、认证状态提供器、Token 管理 |
| `Program.cs` | — | 服务注册、HttpClient 配置、Blazor 渲染配置 |

## 依赖关系

- 引用 `Campus.Attendance.Core`、`Campus.Attendance.Models`、`Campus.Attendance.Services`
- 通过 `HttpClient` 调用 `Campus.Attendance.Api`（基址由 `Api:BaseUrl` 配置）
- 运行端口：开发 5249（HTTP）/ 7250（HTTPS），Docker 8080

## 配置

| 配置项 | 环境变量 | 说明 |
|--------|---------|------|
| `Api:BaseUrl` | `Api__BaseUrl` | 后端 API 基址（默认 http://localhost:5000） |
