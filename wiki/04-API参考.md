# API 参考

> 📖 本文档是 Wiki 的一部分 · [🏠 返回首页](./Home.md)

API 入口项目基于 **Minimal APIs + 垂直切片架构 (VSA)**，提供 RESTful HTTP 接口，API 版本控制为 `/api/v1/...`。

---

## 认证与安全流程

### 登录流程

1. 客户端调用 `/api/v1/auth/login`，请求体必须包含 `CaptchaToken`（滑块验证码，H-1）
2. `LoginValidator` 通过 `NotEmpty()` 强制校验 `CaptchaToken`，`LoginHandler` 再次校验 token 有效性
3. 用户不存在时执行 dummy BCrypt Verify，消除时序侧信道（L-3）
4. 密码校验通过后签发 JWT（含 `jti`、`system_user_id` 等 claim）
5. 若启用 2FA → 返回 `requiresTwoFactor`，走 2FA 验证流程
6. 若学生 `MustChangePassword = true` → 返回 `mustChangePassword`，跳转 `/force-change-password`

### JWT 黑名单（M-3）

- 登出端点 `/auth/logout` 调用 `TokenRevocationService.RevokeAsync(jti, expireAt)` 将 `jti` 写入 `IDistributedCache`
- `OnTokenValidated` 事件实时检查 `jti` 是否在黑名单，命中则 401
- **依赖 Redis / `IDistributedCache`**，生产环境必须配置

### 审计日志（M-5）

`IAuditService` 在以下场景记录审计事件（用户、IP、操作、结果、时间）：

- 登录成功 / 失败
- 密码修改 / 重置
- 2FA 绑定 / 验证
- 用户增删
- CSV 批量导入

---

## API 端点表

### 认证（Auth）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/auth/login` | POST | 匿名 | 登录（强制校验滑块验证码 `CaptchaToken`） |
| `/api/v1/auth/logout` | POST | 已登录 | 登出，将当前 JWT 的 `jti` 加入黑名单（M-3） |
| `/api/v1/auth/force-change-password` | POST | 已登录 | 强制改密（学生随机密码首登 / `MustChangePassword` 场景，L-2） |
| `/api/v1/auth/2fa-setup` | POST | 已登录 | 启用 2FA，TOTP 密钥经服务端会话下发（M-4） |
| `/api/v1/auth/2fa-verify` | POST | 已登录 | 验证 2FA（含重放保护 M-1 + 速率限制 M-2） |
| `/api/v1/auth/2fa-challenge` | POST | 匿名 | 2FA 登录验证（独立速率限制 M-2） |
| `/api/v1/auth/change-password` | POST | 已登录 | 修改密码 |
| `/api/v1/auth/me` | GET | 已登录 | 获取当前用户信息 |

> 已删除端点：~~`/api/v1/auth/2fa-complete`~~（H-2 修复，避免未验证即创建会话）

### 用户管理（Users / Students / Teachers）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/users/students` | GET/POST | 管理员 | 学生列表 / 创建学生 |
| `/api/v1/users/students/{id}` | GET/PUT/DELETE | 管理员 | 学生详情 / 更新 / 删除 |
| `/api/v1/users/students/batch-import` | POST | 管理员 | CSV 批量导入学生（详见下方） |
| `/api/v1/users/teachers` | GET/POST | 管理员 | 教师列表 / 创建教师 |
| `/api/v1/users/teachers/{id}` | GET/PUT/DELETE | 管理员 | 教师详情 / 更新 / 删除 |

### 教师（Teachers）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/teachers/me` | GET | 教师 | 获取当前教师信息 |
| `/api/v1/teachers/courses` | GET | 教师 | 我的课程列表 |

### 考勤（Attendance）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/attendance/sessions` | POST/GET | 教师 | 创建考勤会话 / 会话列表 |
| `/api/v1/attendance/sessions/{id}` | GET | 教师 | 会话详情 |
| `/api/v1/attendance/qrcode` | GET | 教师 | 生成动态二维码（30 秒刷新） |
| `/api/v1/attendance/checkin` | POST | 学生 | 扫码签到 |
| `/api/v1/attendance/roll-call` | POST | 教师 | 一键点名 / 随机点名 |
| `/api/v1/attendance/{id}/manual` | PUT | 教师 | 手动补签 |

### 请假（Leave）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/leave` | POST/GET | 学生/教师 | 请假申请 / 列表查询 |
| `/api/v1/leave/{id}/approve` | POST | 辅导员 | 审批请假 |
| `/api/v1/leave/{id}/reject` | POST | 辅导员 | 驳回请假 |

### 课程（Course）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/courses` | GET/POST | 管理员/教师 | 课程列表 / 创建课程 |
| `/api/v1/courses/{id}` | GET/PUT/DELETE | 管理员 | 课程详情 / 更新 / 删除 |
| `/api/v1/schedules` | GET/POST | 管理员/教师 | 排课查询 / 创建排课 |

### 系统用户（SystemUsers）

| 端点 | 方法 | 鉴权 | 说明 |
|------|------|------|------|
| `/api/v1/system-users` | GET/POST | 管理员 | 系统用户列表 / 创建 |
| `/api/v1/system-users/{id}` | DELETE | 管理员 | 删除（自删除保护，H-3） |
| `/api/v1/system-users/{id}/reset-password` | POST | 管理员 | 重置密码 |

> 端点鉴权策略：`RequireAdmin` / `RequireTeacher` / `RequireStudent` / `RequireCounselor` / `RequireDepartmentHead`。

---

## 学生 CSV 批量导入

`POST /api/v1/users/students/batch-import`（`multipart/form-data`，字段 `file`）

第二轮安全审计后的行为变更：

- **密码策略**：不再使用统一默认密码，改用 `RandomNumberGenerator` 为每位学生生成 **12 位随机密码**
- **强制改密标记**：`Student.MustChangePassword = true`，学生首次登录引导至 `/force-change-password`
- **响应体新增字段**：
  - `GeneratedPasswords`：`Dictionary<string, string>`，学号 → 随机密码清单（仅管理员可见，需通过安全渠道下发）
  - `MustChangePasswordCount`：标记为强制改密的学生数
- **异常脱敏（M-6）**：导入异常时响应仅返回通用错误信息（`ProblemDetails`），**不暴露 `ex.Message`** 给客户端，详细信息仅写入日志
- **审计（M-5）**：调用 `IAuditService` 记录批量导入操作

---

## 密码策略

通过 FluentValidation 扩展 `ApplyPasswordPolicy()` 统一约束密码复杂度（L-1 修复）：

- 最小长度 **8 位**
- 必须包含 **大写字母**（A-Z）
- 必须包含 **小写字母**（a-z）
- 必须包含 **数字**（0-9）

适用端点：`/auth/change-password`、`/auth/force-change-password`、用户创建/重置密码等。

```csharp
public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy();
    }
}
```

---

## 安全配置

### 安全响应头

`SecurityHeadersMiddleware` 统一下发以下响应头（L-4 修复）：

| 响应头 | 取值 |
|--------|------|
| `Content-Security-Policy` | 默认 `default-src 'self'`，按页面需要放宽 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains`（生产环境） |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | 限制摄像头/麦克风/地理位置等敏感能力 |

### CORS 配置

第二轮审计后 CORS 改用 **白名单** 模式（M-7 修复）：

- `appsettings.json` 中 `Cors:AllowedOrigins` **置空**，不再使用通配符
- 允许的来源通过环境变量 `Cors__AllowedOrigins:0`、`Cors__AllowedOrigins:1`... 注入
- 启动时校验白名单非空（生产环境），为空且非 Development 环境将抛出启动异常

---

## 配置项

> 第二轮安全审计后，`Db:ConnectionString`、`Jwt:SecretKey`、`Cors:AllowedOrigins` 均已置空，启动时强制校验，**生产环境必须通过环境变量或 user-secrets 注入**。

| 配置项 | 环境变量 | 说明 |
|--------|---------|------|
| `Db:ProviderType` | `Db__ProviderType` | 数据库类型（SQLite / MySQL） |
| `Db:ConnectionString` | `Db__ConnectionString` | 连接字符串（**H-5：appsettings 置空，必须注入**） |
| `Jwt:SecretKey` | `Jwt__SecretKey` | JWT 签名密钥（≥32 字符，**L-5：Development 置空，DEBUG 自动生成临时密钥，建议用 user-secrets**） |
| `Jwt:Issuer` | `Jwt__Issuer` | 签发者 |
| `Jwt:Audience` | `Jwt__Audience` | 受众 |
| `Jwt:ExpireMinutes` | `Jwt__ExpireMinutes` | 过期时间（分钟，默认 120） |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins:0` ... | CORS 白名单（**M-7：appsettings 置空，必须注入**） |
| `ConnectionStrings:redis` | `ConnectionStrings__redis` | Redis 连接字符串（**JWT 黑名单 / TOTP 重放保护 / 2FA 速率限制依赖此缓存，生产必须配置**） |

> 详细 API 说明请参阅 [docs/API项目说明.md](../docs/API项目说明.md)。

---

[⬅️ 上一页](./03-快速开始.md) · [🏠 首页](./Home.md) · [➡️ 下一页](./05-安全设计.md)
