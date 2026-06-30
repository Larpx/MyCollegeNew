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
| `Components/Pages/Login.razor` | 登录页（含 CaptchaToken 隐藏字段、`info=changed` 成功提示） |
| `Components/Pages/ForceChangePassword.razor` | 强制改密页（L-2），CSV 导入学生首次登录后跳转至此 |
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

## 认证流程

### 标准登录流程

```
用户访问 /login
    │
    ▼
Login.razor 渲染（@rendermode InteractiveServer）
    ├─ 滑块验证码组件 → 生成 CaptchaToken
    └─ 表单含 CaptchaToken 隐藏字段（H-1）
    │
    ▼
POST /bff/login（BFF 端点）
    └─ 转发至 API /api/v1/auth/login，携带 CaptchaToken
    │
    ▼
登录成功 → 写入 HttpOnly Cookie → 跳转对应角色首页
登录失败 → 返回错误信息
```

### 强制改密流程（L-2）

CSV 批量导入的学生账号使用随机密码并标记 `MustChangePassword = true`，首次登录后强制改密：

```
学生登录成功（MustChangePassword = true）
    │
    ▼
CustomAuthStateProvider 检测到 MustChangePassword 标记
    │
    ▼
重定向至 /force-change-password
    │
    ▼
ForceChangePassword.razor 渲染改密表单
    ├─ 新密码需满足密码策略（至少 8 位、含大小写字母和数字，L-1）
    └─ 提交至 BFF /bff/force-change-password
    │
    ▼
改密成功 → 跳转 /login?info=changed（登录页显示成功提示）
```

> 改密完成前禁止访问其他受保护页面，`ForceChangePassword.razor` 不在布局菜单中暴露。

## BFF 端点

Web 项目作为 BFF，封装对后端 API 的调用，避免浏览器直接暴露 Token 与 API 细节。第二轮安全审计后新增端点：

| BFF 端点 | 转发至 API | 说明 |
|----------|-----------|------|
| `POST /bff/login` | `/api/v1/auth/login` | 登录（携带 CaptchaToken，H-1） |
| `POST /bff/logout` | `/api/v1/auth/logout` | 登出（触发 JWT 黑名单撤销，M-3） |
| `POST /bff/force-change-password` | `/api/v1/auth/force-change-password` | 强制改密（L-2） |
| `POST /bff/2fa/setup` | `/api/v1/auth/2fa-setup` | 2FA 绑定初始化（密钥存服务端会话，M-4） |
| `POST /bff/2fa/verify` | `/api/v1/auth/2fa-verify` | 2FA 绑定验证 |
| `POST /bff/2fa/challenge` | `/api/v1/auth/2fa-challenge` | 2FA 登录验证（独立速率限制，M-2） |

> BFF 端点统一由 `Program.cs` 注册，通过 `ApiClient` 转发请求并附加 Bearer Token。
> `/bff/force-change-password` 为 L-2 修复新增，配合 `ForceChangePassword.razor` 使用。

## 2FA 流程（TOTP）

第二轮安全审计（M-1、M-2、M-4）后，2FA 流程调整：

```
绑定阶段（用户已登录）:
    POST /bff/2fa/setup
        └─ API 生成 TOTP 密钥 → 写入 IDistributedCache 服务端会话（M-4）
        └─ 返回二维码图像（不再通过非 HttpOnly Cookie 传输密钥）
    用户扫码后输入验证码
    POST /bff/2fa/verify
        └─ 校验验证码 + 重放保护（M-1，已用验证码 30s 内拒绝）
        └─ 绑定成功 → IAuditService 记录（M-5）

登录验证阶段:
    POST /bff/login → 返回需要 2FA
    POST /bff/2fa/challenge
        └─ 独立速率限制：每 IP 每分钟 10 次（M-2）
        └─ 5 次失败锁定
        └─ 重放保护（M-1）
```

## 登录页面（Login.razor）

第二轮安全审计（H-1、L-2）后，登录页调整：

- **CaptchaToken 隐藏字段**：滑块验证码组件生成 token 后写入隐藏字段，随表单提交至 BFF（H-1 强制校验）
- **`info=changed` 成功提示**：强制改密完成后跳转 `/login?info=changed`，登录页检测该参数并显示"密码修改成功，请重新登录"提示
- **错误信息脱敏**：登录失败仅显示通用错误（用户名或密码错误），不区分用户不存在与密码错误（配合 L-3 时序侧信道防护）

## 强制改密页面（ForceChangePassword.razor）

L-2 修复新增页面，路径 `/force-change-password`：

- **触发条件**：登录用户 `MustChangePassword = true`（CSV 批量导入的随机密码学生账号）
- **渲染模式**：`@rendermode InteractiveServer`
- **表单字段**：当前密码、新密码、确认新密码
- **密码策略**：新密码需满足 `ApplyPasswordPolicy()`（L-1，至少 8 位、含大小写字母和数字）
- **提交**：调用 BFF `/bff/force-change-password`，成功后清除登录态并跳转 `/login?info=changed`
- **访问控制**：未登录或非强制改密用户访问时重定向至首页
