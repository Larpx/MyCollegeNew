# Web 前端项目说明

Web 前端项目，基于 **Blazor Web App**，采用**静态 SSR 默认渲染 + 高交互组件 InteractiveServer** 的模式，通过 HttpClient 调用后端 API。

## 项目职责

- 提供 Blazor Web App UI（静态 SSR + 交互式 Server）
- 实现三端页面（管理员 / 教师 / 学生）
- BFF 简化模式：JWT 存储在 HttpOnly Cookie，服务端拦截器附加 Bearer Token
- 封装 ApiClient 统一调用后端 API
- 自定义 AuthenticationStateProvider，基于 Cookie 中的 JWT 构建认证状态
- 响应式布局，适配桌面与移动端浏览器

## 关键目录与类型

| 目录 | 说明 |
|------|------|
| `Services/ApiClient.cs` | API 客户端：从 Cookie 读取 Token 附加到请求头 |
| `Services/TokenService.cs` | Token 服务：管理 HttpOnly Cookie 中的 JWT |
| `Services/CustomAuthStateProvider.cs` | 自定义认证状态提供器 |
| `Components/Layout/` | 布局组件（Admin/Teacher/Student/Login） |
| `Components/Pages/Admin/` | 管理员页面（仪表盘、用户管理、组织架构、课程、统计） |
| `Components/Pages/Teacher/` | 教师页面（考勤发起、点名、请假审批、课程） |
| `Components/Pages/Student/` | 学生页面（签到、请假、课表、个人信息） |
| `Components/Ui/` | 通用 UI 组件库（Button、Card、Modal、Table 等） |
| `wwwroot/` | 静态资源（CSS 设计系统） |
| `Program.cs` | 服务注册、HttpClient 配置、Blazor 渲染配置 |

## 依赖关系

- 引用 `Campus.Attendance.Shared`、`Campus.Attendance.Infrastructure`、`Campus.Attendance.ServiceDefaults`
- 通过 `HttpClient` 调用 `Campus.Attendance.Api`（基址由 `Api:BaseUrl` 配置）
- 运行端口：开发 5249（HTTP）/ 7250（HTTPS），Docker 8080

## 渲染模式

| 页面类型 | 渲染模式 | 说明 |
|----------|----------|------|
| 静态展示页 | 静态 SSR（默认） | 无交互需求，服务端渲染 |
| 登录页、仪表盘、表单 | `@rendermode InteractiveServer` | 需要交互操作 |
| 错误/未找到页面 | 静态 SSR | 错误处理页面 |

## 配置

| 配置项 | 环境变量 | 说明 |
|--------|---------|------|
| `Api:BaseUrl` | `Api__BaseUrl` | 后端 API 基址（默认 http://localhost:5000） |
