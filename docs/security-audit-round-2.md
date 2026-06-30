# 第二轮安全审计报告

**审计日期**：2026-06-30
**审计范围**：`d:\Work\repos\my-college-project` 全代码库
**审计方法**：自动化安全审计工具，按四个攻击面系统性检查（认证与访问控制 / 注入向量 / 外部交互 / 敏感数据处理）
**审计标准**：识别中等严重度及以上的已确认漏洞，且必须具备可论证的端到端利用路径
**排除项**：DEBUG TOTP 后门 `888888` 与默认密码 `123456` 为本地测试预留，不在本轮报告范围；第一轮已修复的硬编码 JWT 密钥与考勤/请假/课程 IDOR 问题已确认生效

---

## 一、审计概览

本轮审计共发现 **17 个问题**，按严重度分组如下：

| 严重度 | 数量 | 漏洞编号 |
|--------|------|----------|
| HIGH | 5 | H-1 ~ H-5 |
| MEDIUM | 7 | M-1 ~ M-7 |
| LOW | 5 | L-1 ~ L-5 |

### 漏洞汇总

| 编号 | 严重度 | 问题 | 攻击面 |
|------|--------|------|--------|
| H-1 | HIGH | 登录滑块验证码校验完全失效 | 认证与访问控制 |
| H-2 | HIGH | BFF `/auth/2fa-complete` 死端点未经验证创建认证会话 | 认证与访问控制 |
| H-3 | HIGH | 管理员自删除保护失效（ICurrentUser 类型混淆） | 认证与访问控制 |
| H-4 | HIGH | 管理员无法通过 `/profile/password` 修改自身密码 | 认证与访问控制 |
| H-5 | HIGH | appsettings.json 硬编码数据库连接字符串密码 | 敏感数据处理 |
| M-1 | MEDIUM | TOTP 验证码缺少重放保护 | 认证与访问控制 |
| M-2 | MEDIUM | 2FA 端点缺少独立速率限制与账户锁定 | 认证与访问控制 |
| M-3 | MEDIUM | JWT 无服务端撤销机制 | 认证与访问控制 |
| M-4 | MEDIUM | 2FA TOTP 密钥通过非 HttpOnly Cookie 传输 | 认证与访问控制 |
| M-5 | MEDIUM | 敏感操作无持久化审计日志 | 敏感数据处理 |
| M-6 | MEDIUM | CSV 批量导入将 ex.Message 返回客户端 | 敏感数据处理 |
| M-7 | MEDIUM | API 端 CORS 配置过于宽松 | 外部交互 |
| L-1 | LOW | 密码复杂度要求过低 | 认证与访问控制 |
| L-2 | LOW | 学生默认密码可预测（学号后 6 位） | 认证与访问控制 |
| L-3 | LOW | 登录流程存在用户存在性时序侧信道 | 认证与访问控制 |
| L-4 | LOW | SecurityHeadersMiddleware 缺少 CSP / HSTS / Referrer-Policy | 敏感数据处理 |
| L-5 | LOW | appsettings.Development.json 硬编码 JWT SecretKey | 敏感数据处理 |

---

## 二、HIGH 严重度漏洞详情

### H-1：登录滑块验证码校验完全失效（Captcha Bypass）

- **攻击者画像**：外部未认证攻击者
- **可控输入向量**：直接 POST 到 `POST /api/v1/login` 的 JSON body
- **确切代码路径**：
  - [AuthDtos.cs:21](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Shared/Features/Auth/AuthDtos.cs#L21) — `LoginRequest.CaptchaToken` 声明为可空 `string?`，无 `[Required]`
  - [LoginValidator.cs:15-19](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Auth/Login/LoginValidator.cs#L15-19) — 仅校验 Username/Password 非空，未校验 CaptchaToken
  - [LoginHandler.cs:41-91](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Auth/Login/LoginHandler.cs#L41-91) — `Handle` 方法从不读取 `request.CaptchaToken`，也从不调用 `ValidateCaptchaTokenAsync`
  - [CaptchaEndpoints.cs:291](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Auth/Captcha/CaptchaEndpoints.cs#L291) — `ValidateCaptchaTokenAsync` 方法定义存在但全局无任何调用方（死代码）
  - [Web/Program.cs:122](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Web/Program.cs#L122) 与 [Admin/Program.cs:121](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Admin/Program.cs#L121) — BFF 构造 `LoginRequest` 时均不设置 CaptchaToken
- **影响**：滑块验证码纯为前端装饰。攻击者直接调用 API 登录端点可完全跳过验证码，唯一防线是每 IP 5 次/分钟的速率限制（可通过分布式 IP 绕过），使暴力破解/撞库成为可能
- **修复方案**：在 `LoginHandler` 注入 `IDistributedCache`，密码校验前调用 `ValidateCaptchaTokenAsync(request.CaptchaToken, _cache)`，失败返回 401；在 `LoginValidator` 中对 CaptchaToken 添加 `NotEmpty()` 校验

### H-2：BFF `/auth/2fa-complete` 死端点未经验证创建认证会话

- **攻击者画像**：外部未认证攻击者
- **可控输入向量**：POST 到 `/auth/2fa-complete` 的表单字段 `Token`、`UserId`、`UserName`、`Role`
- **确切代码路径**：
  - [Web/Program.cs:252-296](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Web/Program.cs#L252-296) — 从 `context.Request.Form` 读取 4 个字段，**不调用任何 API 验证**，直接构造 Claims 并 `SignInAsync` 创建认证 Cookie
  - [Admin/Program.cs:244-280](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Admin/Program.cs#L244-280) — Admin 端同样实现，仅额外检查 `role != "Admin"` 后放行
  - 该端点是**死端点**：[TwoFactor.razor:46,76,106](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Web/Components/Pages/TwoFactor.razor#L46) 表单分别提交到 `/auth/2fa-setup`、`/auth/2fa-bind`、`/auth/2fa-verify`，从不提交到 `/auth/2fa-complete`
- **影响**：攻击者可向 Admin 端 `/auth/2fa-complete` POST `Role=Admin&UserId=1&UserName=admin&Token=任意字符串`，获得 Admin 角色的认证 Cookie 进入管理后台 UI。虽然 `token` claim 为伪造值导致后续 API 调用 401，但攻击者可窥见后台 UI 结构与导航路径
- **修复方案**：删除 `/auth/2fa-complete` 端点（已被 `/auth/2fa-verify` 和 `/auth/2fa-bind` 取代）；若需保留必须先调用 API 的 `/2fa/verify` 验证 Token，从 API 响应获取角色信息后再创建 Cookie

### H-3：管理员自删除保护失效（ICurrentUser.UserId 类型混淆）

- **攻击者画像**：已认证管理员（或被劫持的管理员会话）
- **可控输入向量**：`DELETE /api/v1/system-users/{id}` 中的路径参数 `id`
- **确切代码路径**：
  - [SystemUserHandlers.cs:156-160](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/SystemUsers/SystemUserHandlers.cs#L156-160) — 比较条件 `user.Username == _currentUser.UserId`，注释声称"ICurrentUser.UserId 存的是用户名"
  - 但实际 [LoginHandler.cs:57](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Auth/Login/LoginHandler.cs#L57) 调用 `BuildTwoFactorResultAsync(admin.Id.ToString(), ...)`，缓存的是 `admin.Id`（数字主键字符串如 `"1"`）
  - [TokenService.cs:60](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Infrastructure/Auth/TokenService.cs#L60) — JWT `user_id` claim 设为该数字 Id
  - [CurrentUserService.cs:29-31](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Infrastructure/Auth/CurrentUserService.cs#L29-31) — `UserId` 读取 `user_id` claim，返回数字 Id
  - 因此 `"admin" == "1"` 恒为 `false`，自删除保护永不生效
- **影响**：管理员可删除自己的账号。若系统中仅剩一个管理员，删除后系统将无法管理
- **修复方案**：在 `ICurrentUser` 接口增加 `long? SystemUserId` 属性（仅 Admin 角色有值），将比较改为 `user.Id == _currentUser.SystemUserId`

### H-4：管理员无法通过 `/profile/password` 修改自身密码（H-3 同源根因）

- **攻击者画像**：已认证管理员
- **可控输入向量**：POST `/api/v1/profile/password` 的 OldPassword/NewPassword
- **确切代码路径**：
  - [ChangePasswordEndpoint.cs:21](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Profile/ChangePassword/ChangePasswordEndpoint.cs#L21) — 传入 `currentUser.UserId`（= 数字 Id 字符串如 `"1"`）
  - [ChangePasswordHandler.cs:43](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Profile/ChangePassword/ChangePasswordHandler.cs#L43) — Admin 分支用 `u.Username == command.UserId` 匹配，`"admin" != "1"` 永不命中，返回 404
  - 对比 Teacher/Student 分支（第 62、80 行）使用 `t.Id == command.UserId` / `s.Id == command.UserId` 因 Id 即工号/学号而正常工作
- **影响**：管理员自助改密（需验证旧密码）功能完全失效。管理员只能通过 `POST /api/v1/system-users/{id}/reset-password` 由其他管理员重置（不要求旧密码、会清空 2FA 绑定），削弱了密码安全
- **修复方案**：Admin 分支改为按 Id 查询 `u.Id.ToString() == command.UserId`，或与 H-3 修复方案统一

### H-5：appsettings.json 硬编码数据库连接字符串密码（root 超级用户）

- **攻击者画像**：任何能访问代码仓库的人员
- **可控输入向量**：无（被动凭证泄露）
- **确切代码路径**：[appsettings.json:9-12](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/appsettings.json#L9-12)
  ```json
  "Db": {
    "ProviderType": "MySQL",
    "ConnectionString": "Server=localhost;Port=3306;Database=attendance;Uid=root;Pwd=root;"
  }
  ```
- **影响**：上一轮仅置空了 `Jwt:SecretKey`，但 `Db:ConnectionString` 仍明文嵌入 `root` 密码且使用 MySQL 超级用户账号。文件提交到版本库导致凭证泄露。若生产部署遗漏设置 `Db__ConnectionString` 环境变量，将以此凭证连接数据库。root 账号泄露等于数据库完全沦陷
- **修复方案**：将 `ConnectionString` 置空，在 `Program.cs` 启动时增加非空校验（参照 JWT SecretKey 处理方式 [Program.cs:105-108](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L105-108)）；改用最小权限的专用数据库账号

---

## 三、MEDIUM 严重度漏洞详情

### M-1：TOTP 验证码缺少重放保护

- **攻击者画像**：MITM / XSS 攻击者，截获一次有效的 2FA 请求
- **可控输入向量**：截获的 TOTP 验证码
- **确切代码路径**：[TotpService.cs:35-64](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Infrastructure/Auth/TotpService.cs#L35-64) — `VerifyCode` 匹配后直接返回 `true`，无任何机制记录"该验证码已使用"
- **影响**：违反 RFC 6238 §5.2 强制要求。攻击者在 30 秒窗口内截获有效 TOTP 后可重放完成 2FA
- **修复方案**：维护短期缓存 `totp:used:{userId}:{code}`（TTL 30-90 秒），验证成功后写入，下次验证检查是否已存在

### M-2：2FA 端点缺少独立速率限制与账户锁定

- **攻击者画像**：外部攻击者，持有有效的 2FA 临时令牌
- **可控输入向量**：`/2fa/verify` 与 `/2fa/bind` 的 Code 字段
- **确切代码路径**：
  - [TwoFactorEndpoints.cs:26-48](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Auth/TwoFactor/TwoFactorEndpoints.cs#L26-48) — 三个端点均 `AllowAnonymous()`，无 `.RequireRateLimiting()`
  - [Program.cs:249-250](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L249-250) — 仅应用全局 `fixed` 限流（100 次/分钟/IP）
- **影响**：攻击者获得 2FA 临时令牌后（5 分钟有效），可在每 IP 100 次/分钟速率下尝试 TOTP 验证码。6 位验证码空间 100 万，分布式 IP 攻击在 5 分钟窗口内可覆盖相当比例
- **修复方案**：为 `/2fa/verify` 和 `/2fa/bind` 添加独立 `RequireRateLimiting("twofa")`（如 10 次/分钟/IP）；缓存中记录每个临时令牌的失败次数，超过 5 次使令牌失效

### M-3：JWT 无服务端撤销机制

- **攻击者画像**：任何窃取到 JWT 的攻击者（XSS、日志泄露、网络拦截）
- **可控输入向量**：窃取的 JWT
- **确切代码路径**：
  - [Program.cs:109-123](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L109-123) — 纯无状态验证（签名+过期），无 Token 黑名单
  - 全局 grep 确认无 `/auth/logout`、`/auth/refresh`、`/auth/revoke` API 端点
  - [JwtConfig.cs:18](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Shared/Security/JwtConfig.cs#L18) — `ExpireMinutes = 120`（2 小时）
- **影响**：JWT 签发后 2 小时内无法撤销。用户"登出"后被窃取的 JWT 仍可正常调用所有 API
- **修复方案**：引入 Token 黑名单（基于 Redis，TTL = JWT 剩余有效期），在 JWT Bearer `OnTokenValidated` 事件中检查黑名单；或缩短 JWT 有效期并引入 Refresh Token 机制

### M-4：2FA TOTP 密钥通过非 HttpOnly Cookie 传输

- **攻击者画像**：XSS 攻击者
- **可控输入向量**：TOTP 绑定流程中的 Cookie 读取
- **确切代码路径**：
  - [Web/Program.cs:330-331](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Web/Program.cs#L330-331) 与 [Admin/Program.cs:313-314](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Admin/Program.cs#L313-314) — `Cookies.Append("2fa_secret", result.Data.Secret, cookieOptions)`，`HttpOnly=false`、`Secure=false`
  - [TwoFactor.razor:179-184](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Web/Components/Pages/TwoFactor.razor#L179-184) — 前端从该 Cookie 读取密钥并显示
- **影响**：TOTP 密钥以明文存入可被 JS 读取的 Cookie，且允许 HTTP 明文传输。存在 XSS 时攻击者可窃取密钥，永久生成有效 TOTP
- **修复方案**：将 TOTP 密钥存储在服务端会话（`IDistributedCache`，键为 2FA 临时令牌）而非 Cookie；前端需要显示时由 API 返回一次性值

### M-5：敏感操作无持久化审计日志（AuditLog 表已建表但从未写入）

- **攻击者画像**：内部已认证用户、管理员、运维人员
- **可控输入向量**：所有敏感操作请求体
- **确切代码路径**：
  - [AuditLog.cs:10](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Shared/Entities/AuditLog.cs#L10) — 实体定义完整（UserId、UserRole、Action、Target、IpAddress、CreateTime）
  - [DbInitializer.cs:50](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Infrastructure/Data/DbInitializer.cs#L50) — `typeof(AuditLog)` 已注册建表
  - 全代码库搜索 `AuditLog` 仅 2 处命中——**无任何 `Insertable<AuditLog>()` 写入调用**
  - 未审计的敏感操作：登录成败、密码修改/重置、2FA 绑定/重置、用户增删、批量导入（[LoginHandler.cs:53,67,82](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Auth/Login/LoginHandler.cs#L53)、[ChangePasswordHandler.cs:106](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Profile/ChangePassword/ChangePasswordHandler.cs#L106)、[SystemUserHandlers.cs:121,165,185](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/SystemUsers/SystemUserHandlers.cs#L121)）
- **影响**：管理员重置任意用户密码、删除账号、2FA 重置等高危操作无任何不可篡改的审计痕迹，发生安全事件后无法取证
- **修复方案**：创建 `IAuditService` 抽象，通过 MediatR 管道行为在上述 Handler 中写入 `AuditLog`；`IpAddress` 从 `IHttpContextAccessor` 解析

### M-6：CSV 批量导入将 ex.Message 返回客户端（信息泄露）

- **攻击者画像**：已认证管理员
- **可控输入向量**：上传的 CSV 文件内容
- **确切代码路径**：[StudentHandlers.cs:280-285](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Students/StudentHandlers.cs#L280-285)
  ```csharp
  catch (Exception ex)
  {
      result.FailedCount++;
      result.Failures.Add(new BatchImportFailureItem { Row = lineNumber, Reason = ex.Message });
      _logger.LogWarning(ex, "CSV 导入第 {Row} 行失败", lineNumber);
  }
  ```
- **影响**：`ex.Message` 可能包含数据库表名/列名、唯一约束名、字段类型不匹配细节、文件路径等内部实现信息，违反"禁止暴露异常详情"规则。管理员可通过构造恶意 CSV 探测后端结构
- **修复方案**：第 283 行改为 `Reason = $"第 {lineNumber} 行数据格式错误或已存在"`，具体异常仅保留在第 284 行日志

### M-7：API 端 CORS 配置过于宽松（AllowAnyOrigin）

- **攻击者画像**：外部恶意网站运营者
- **可控输入向量**：任意 HTTP Origin 头
- **确切代码路径**：[Program.cs:197-205](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L197-205)
  ```csharp
  policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
  ```
- **影响**：任何外部域名可向 API 发起跨域请求并读取响应。虽然认证基于 Bearer Token（非 Cookie）缓解了 CSRF 风险，但 AllowAnyOrigin 端点的响应可被任意站点 JS 读取，放大信息泄露面
- **修复方案**：使用白名单配置允许的前端域名（通过 `appsettings.json` 的 `Cors:AllowedOrigins` 注入），用 `WithOrigins(allowedOrigins)` 替换 `AllowAnyOrigin()`

---

## 四、LOW 严重度漏洞清单

| 编号 | 问题 | 位置 | 修复建议 |
|------|------|------|----------|
| L-1 | 密码复杂度要求过低（仅 MinimumLength(6)，无大小写/数字/特殊字符要求） | ChangePasswordValidator.cs:18-21 | 添加复杂度规则（至少包含大小写字母+数字），引入常见弱密码黑名单 |
| L-2 | 学生默认密码可预测（学号后 6 位），CSV 导入与种子数据均如此 | StudentHandlers.cs:24-25,260-262 | 生成随机初始密码并通过安全渠道通知学生；强制首次登录修改密码 |
| L-3 | 登录流程存在用户存在性时序侧信道（依次查询 SystemUser→Teacher→Student） | LoginHandler.cs:46-90 | 对不存在的用户名执行 dummy BCrypt Verify 操作以对齐时序；或并行查询 |
| L-4 | SecurityHeadersMiddleware 缺少 CSP / HSTS / Referrer-Policy / Permissions-Policy | SecurityHeadersMiddleware.cs:25-27 | 追加 Content-Security-Policy、Strict-Transport-Security、Referrer-Policy |
| L-5 | appsettings.Development.json 硬编码 JWT SecretKey | appsettings.Development.json:15 | 开发环境改用 `dotnet user-secrets` 管理密钥，使密钥不进入版本库 |

---

## 五、已清查确认无问题的攻击面

以下攻击面经逐一核验确认符合规范，无新增漏洞：

### 注入向量（全部安全）

1. **SQL 注入**：所有 SqlSugar 查询均使用 lambda 表达式（参数化），无字符串拼接 SQL；`Ado.ExecuteCommand` 仅用于静态 DDL 建表语句（[TestDbContext.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Tests/Infrastructure/TestDbContext.cs) 中 15 处均为 `CREATE TABLE IF NOT EXISTS`）
2. **SqlSugar 动态查询**：全代码库搜索 `.Where($"` 与 `.Where("` 均无命中，所有 `.Where()` 均使用 lambda 表达式
3. **Shell 命令注入**：全代码库无 `Process.Start` 调用
4. **模板渲染注入**：全代码库无 `RazorLight`、`RazorEngine` 动态模板编译调用
5. **文件路径穿越**：CSV 导入使用 `IFormFile` Stream 不接触磁盘路径；日志读取限定 `Logs` 目录固定扩展名且 `TopDirectoryOnly`
6. **反序列化**：无 `BinaryFormatter`、无 `TypeNameHandling=All/Auto/Objects` 危险配置

### 外部交互（全部安全）

7. **HttpClient 与 SSRF**：所有出站 HttpClient 调用指向配置中的固定 BaseAddress，无用户可控 URL
8. **第三方 API 集成**：TOTP（OtpNet）与 QR 码（QRCoder）均为本地计算，无外部 HTTP 调用；OTPAuth URI 使用 `Uri.EscapeDataString` 防注入
9. **Webhook 处理器**：代码库无 Webhook 端点或出站回调实现
10. **文件下载**：仅返回内存生成的 Excel（`MemoryStream`），文件名服务端拼接，用户仅控制 long 类型 Id

### 敏感数据处理（大部分安全）

11. **日志敏感字段**：审查全部 24 个含 `_logger.Log` 的文件，未发现记录 password/token/TOTP secret 明文
12. **异常信息暴露**：[GlobalExceptionHandler.cs:55-59](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Exceptions/GlobalExceptionHandler.cs#L55-59) 系统异常分支已正确不暴露 `ex.Message`（除 M-6 的 CSV 导入）
13. **DTO 隔离**：`StudentResponseDto`、`TeacherResponseDto`、`SystemUserResponseDto` 均不含 Password 与 TwoFactorSecret 字段
14. **密码哈希**：统一使用 `BCrypt.Net.BCrypt.HashPassword/Verify`，自带随机盐值
15. **PII 字段**：Student/Teacher/SystemUser 实体无身份证号、手机号、家庭住址字段
16. **JWT 签名校验**：`ValidateIssuerSigningKey`、`ValidateLifetime`、`ValidateIssuer`、`ValidateAudience` 全开，ClockSkew=1 分钟
17. **管理端点保护**：SystemAdmin/SystemUsers/Students/Teachers/Organization 端点均正确标注 `RequireAuthorization("RequireAdmin")`
18. **DepartmentHead 策略**：通过数据库查询 `IsDepartmentHead` 字段校验，未信任前端输入
19. **限流**：登录端点 `RequireRateLimiting("login")`（5 次/分钟），全局 `fixed`（100 次/分钟）

---

## 六、架构梳理

### 1. 入口点（Program.cs 中间件链）

中间件顺序（[Program.cs:223-266](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L223-266)）：

```
UseExceptionHandler()          // 全局异常（GlobalExceptionHandler）
MapDefaultEndpoints()         // Aspire 健康检查
UseMiddleware<SecurityHeadersMiddleware>()  // 安全响应头
[Dev] MapOpenApi/Scalar       // 仅开发环境
UseHttpsRedirection()
UseResponseCompression()
UseCors()                     // ⚠ AllowAnyOrigin/Method/Header（见 M-7）
UseRateLimiter()
UseOutputCache()
UseAuthentication()           // JWT Bearer
UseAuthorization()
MapGroup("/api/v1")            // VSA Feature 端点
```

### 2. 信任边界

| 边界 | 实现 | 风险点 |
|------|------|--------|
| API → DB | `IDbContext`（SqlSugar）注入，Scoped/请求级 | 配置层硬编码密码（H-5） |
| API → 文件系统 | CSV 导入 `IFormFile` | ex.Message 泄露（M-6） |
| 前端 → API | JWT Bearer（Header），CORS `AllowAnyOrigin` | CORS 过宽（M-7），但 Bearer 不在 Cookie 中，无 CSRF 风险 |

### 3. 认证流程

- 配置：[Program.cs:109-123](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L109-123) `AddJwtBearer`，HS256 对称签名
- 启动校验：[Program.cs:105-108](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L105-108) 强制 `JwtConfig.SecretKey` 非空
- 令牌生成：[TokenService.cs:50-75](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Infrastructure/Auth/TokenService.cs#L50-75) 写入 `user_id/user_name/role` 声明
- 当前用户解析：[CurrentUserService.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Infrastructure/Auth/CurrentUserService.cs) 从 `HttpContext.User.Claims` 解析

### 4. 授权策略

定义位置：[Program.cs:126-135](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Program.cs#L126-135)

| 策略 | 要求 | 自定义处理器 |
|------|------|--------------|
| RequireAdmin | Role=Admin | — |
| RequireTeacher | Role=Teacher/Counselor | — |
| RequireStudent | Role=Student | — |
| RequireCounselor | Role=Counselor | — |
| RequireDepartmentHead | AuthenticatedUser + DepartmentHeadRequirement | `DepartmentHeadHandler`（校验 `Teacher.IsDepartmentHead`） |

### 5. 数据流转

```
HTTP 请求
  → Minimal API Endpoint
  → IMediator.Send(Command/Query)         // MediatR
  → ValidationBehavior<,>（管道验证）       // FluentValidation
  → XxxHandler.Handle（业务逻辑）
  → IDbContext.Client（SqlSugar）          // 参数化查询
  → DB（MySQL/SQLite）
  ← ApiResponse<T> 统一包装
  ← GlobalExceptionHandler 兜底
```

---

## 七、修复优先级建议

### 优先级 1（HIGH，外部可直接利用）

- **H-1** 验证码失效：修复成本中等，但外部可直接暴力破解
- **H-2** 死端点：修复成本极低（删除即可），但外部可直接获取 Admin Cookie
- **H-3 + H-4** ICurrentUser 类型混淆：同源根因，应一并修复
- **H-5** DB 凭证硬编码：修复成本极低（置空+校验）

### 优先级 2（MEDIUM，合规与纵深防御）

- **M-5** 审计日志缺失：影响合规取证能力，建议尽快补齐
- **M-1** TOTP 重放：违反 RFC 标准，修复成本中等
- **M-2** 2FA 限流：修复成本低
- **M-6** CSV 异常泄露：修复成本极低
- **M-7** CORS 收紧：修复成本低
- **M-3** JWT 撤销：修复成本较高（需引入 Redis）
- **M-4** TOTP Cookie：修复成本中等

### 优先级 3（LOW，可纳入后续迭代）

- L-1 ~ L-5 可纳入后续安全加固迭代

---

## 八、本轮审计结论

本轮审计在第一轮已修复 5 个高危漏洞的基础上，新发现 5 个 HIGH、7 个 MEDIUM、5 个 LOW 严重度的漏洞。其中 H-1（验证码失效）与 H-2（死端点）是外部可直接利用的高危漏洞应优先修复；H-3 与 H-4 同源（ICurrentUser 类型混淆）应一并修复；H-5 修复成本极低。MEDIUM 中 M-5（审计日志缺失）影响合规取证能力，建议尽快补齐。

注入向量与外部交互两个攻击面经全面清查确认无新增漏洞，SqlSugar ORM 参数化查询、文件路径操作、HttpClient 出站调用等均符合安全规范。

---

## 九、修复状态（2026-06-30 全量修复完成）

本轮审计发现的 17 个漏洞已全部修复，构建验证通过（0 错误 0 警告，70/70 单元测试通过）。

### 修复清单

| 编号 | 严重度 | 问题 | 修复方案 | 状态 |
|------|--------|------|----------|------|
| H-1 | HIGH | 登录滑块验证码校验完全失效 | LoginRequest.CaptchaToken 改为必填，LoginValidator 添加 NotEmpty 校验，LoginHandler 强制校验 captcha token | 已修复 |
| H-2 | HIGH | BFF /auth/2fa-complete 死端点 | 删除该端点，2FA 流程由 /auth/2fa-verify 与 /auth/2fa-bind 完成 | 已修复 |
| H-3 | HIGH | ICurrentUser 类型混淆 | 新增 SystemUserId 属性，JWT 新增 system_user_id claim，SystemUserHandlers 自删除校验改用 SystemUserId | 已修复 |
| H-4 | HIGH | 管理员无法修改自身密码 | ChangePasswordHandler Admin 分支兼容 Id/Username 查询 | 已修复 |
| H-5 | HIGH | appsettings.json 硬编码数据库连接字符串 | 连接字符串置空，启动时校验，生产环境通过环境变量 Db__ConnectionString 注入 | 已修复 |
| M-1 | MEDIUM | TOTP 验证码缺少重放保护 | TotpService.VerifyCodeAsync 使用 IDistributedCache 记录已用验证码，窗口期内拒绝重放 | 已修复 |
| M-2 | MEDIUM | 2FA 端点缺少速率限制与锁定 | 新增 twofa 速率限制策略（每 IP 每分钟 10 次）+ 5 次失败锁定临时令牌 | 已修复 |
| M-3 | MEDIUM | JWT 无服务端撤销机制 | 新增 TokenRevocationService（黑名单 TTL=JWT 剩余有效期）+ /auth/logout 端点 + jti claim + OnTokenValidated 黑名单检查 | 已修复 |
| M-4 | MEDIUM | TOTP 密钥通过非 HttpOnly Cookie 传输 | 改用 IDistributedCache 服务端会话存储 TOTP 密钥/二维码，TwoFactor.razor 从缓存读取 | 已修复 |
| M-5 | MEDIUM | 敏感操作无持久化审计日志 | 新增 IAuditService/AuditService（容错写入，IP 解析，字段截断）+ AuditLog 实体，应用于登录/改密/2FA/用户增删/批量导入 | 已修复 |
| M-6 | MEDIUM | CSV 批量导入 ex.Message 返回客户端 | catch 块替换为通用错误提示，详细信息保留在服务端日志 | 已修复 |
| M-7 | MEDIUM | CORS 配置过于宽松 | 改用白名单，appsettings.json Cors:AllowedOrigins 置空，生产环境通过环境变量注入 | 已修复 |
| L-1 | LOW | 密码复杂度要求过低 | 新增 ApplyPasswordPolicy() FluentValidation 扩展（8 位+大小写+数字），应用于所有密码校验器 | 已修复 |
| L-2 | LOW | 学生默认密码可预测 | CSV 导入使用 RandomNumberGenerator 生成 12 位随机密码，Student 实体新增 MustChangePassword 字段，首次登录强制改密（新增 /auth/force-change-password 端点与 /force-change-password 页面） | 已修复 |
| L-3 | LOW | 登录用户存在性时序侧信道 | 用户不存在时执行 dummy BCrypt Verify 对齐耗时 | 已修复 |
| L-4 | LOW | SecurityHeadersMiddleware 缺少安全头 | 新增 CSP、HSTS（HTTPS）、Referrer-Policy、Permissions-Policy | 已修复 |
| L-5 | LOW | appsettings.Development.json 硬编码 JWT SecretKey | SecretKey 置空，csproj 添加 UserSecretsId，DEBUG 模式自动生成随机密钥，建议使用 dotnet user-secrets 管理 | 已修复 |

### 验证结果

- 构建：0 错误，0 警告
- 单元测试：70/70 通过
- 涉及修改的文件清单见各修复项的代码引用
