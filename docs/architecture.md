# 架构说明

## 1. 分层架构

系统采用经典的四层分层架构，依赖方向自上而下，Core 层不依赖任何业务层。

```
┌─────────────────────────────────────────────────────────┐
│                    用户浏览器                             │
└──────────────┬──────────────────────────┬───────────────┘
               │                          │
               ▼                          ▼
┌──────────────────────┐     ┌──────────────────────────┐
│  Campus.Attendance   │     │  Campus.Attendance.Api   │
│       .Web           │     │   (RESTful API)          │
│  (Blazor Server)     │     │                          │
│                      │     │  Controllers             │
│  Pages / Components  │     │  Middleware              │
│  ApiClient (HttpClient)──→ │                          │
└──────────┬───────────┘     └────────────┬─────────────┘
           │                              │
           │ 引用                         │ 引用
           ▼                              ▼
┌─────────────────────────────────────────────────────────┐
│                Campus.Attendance.Services               │
│                  (业务逻辑层)                            │
│                                                         │
│  AuthService / AttendanceService / LeaveService / ...   │
│  DbInitializer / SqlSugarDbContext                      │
└──────────────────────┬──────────────────────────────────┘
                       │ 引用
                       ▼
┌─────────────────────────────────────────────────────────┐
│                Campus.Attendance.Models                 │
│                  (DTO 层)                               │
│  LoginDto / AttendanceDtos / LeaveDtos / ...            │
└──────────────────────┬──────────────────────────────────┘
                       │ 引用
                       ▼
┌─────────────────────────────────────────────────────────┐
│                Campus.Attendance.Core                   │
│                  (核心层)                                │
│  Entities / Enums / Configuration / Security /          │
│  Interfaces / Responses / Exceptions                    │
└─────────────────────────────────────────────────────────┘
```

### 依赖规则

| 层 | 可引用 | 被引用 |
|----|--------|--------|
| Core | 无 | Models, Services, Api, Web, Tests |
| Models | Core | Services, Api, Web, Tests |
| Services | Core, Models | Api, Web, Tests |
| Api | Core, Models, Services | Tests |
| Web | Core, Models, Services | 无 |
| Tests | 全部 | 无 |

> Web 引用 Services 是为了类型共享，实际业务调用通过 HttpClient 走 Api，不直接调用服务。

## 2. 数据流

### 常规请求流（以学生查看课表为例）

```
用户浏览器
    │
    │ ① 点击「课表」菜单
    ▼
Blazor Server (Web 容器)
    │
    │ ② Razor 组件调用 ApiClient.GetAsync<ScheduleDto>("/api/schedules/my")
    ▼
ApiClient (HttpClient)
    │
    │ ③ 从 localStorage 读取 JWT Token，附加到 Authorization: Bearer {token}
    │    发送 HTTP GET http://api:8080/api/schedules/my
    ▼
Api Controller (SchedulesController)
    │
    │ ④ [Authorize] 策略校验 → JWT 解析 → 提取 UserId/Role
    │    调用 IScheduleService.GetMyScheduleAsync(userId)
    ▼
Service 层 (ScheduleService)
    │
    │ ⑤ 通过 IDbContext.Client (SqlSugar) 查询数据库
    ▼
SqlSugar ORM
    │
    │ ⑥ 生成 SQL，执行查询
    ▼
MySQL 数据库 (db 容器)
    │
    │ ⑦ 返回数据集
    ▼
Service 层
    │
    │ ⑧ 映射为 DTO (ScheduleDto)
    ▼
Api Controller
    │
    │ ⑨ 包装为 ApiResponse<ScheduleDto>，JSON 序列化返回
    ▼
ApiClient
    │
    │ ⑩ 反序列化 ApiResponse<T>，校验 Code，返回 Data
    ▼
Blazor Server
    │
    │ ⑪ 渲染 Razor 组件，推送 HTML 到浏览器（SignalR）
    ▼
用户浏览器
```

### 关键设计点

- **ApiClient 统一封装**：所有 HTTP 调用经过 `ApiClient.SendAsync<T>`，自动附加 Token、处理 401/403、反序列化 `ApiResponse<T>`
- **401 自动登出**：Token 过期时清除 localStorage 并跳转 `/login`
- **403 权限提示**：无权限操作时抛出 `ApiException`，前端显示提示
- **统一响应包装**：所有 API 返回 `ApiResponse<T>`（Code + Message + Data）

## 3. 认证流程

### 登录流程

```
用户输入用户名/密码
    │
    ▼
Login.razor → ApiClient.PostAsync<LoginResult>("/api/auth/login", {username, password})
    │
    ▼
AuthController.Login → IAuthService.LoginAsync(request)
    │
    ├─ ① 查 SystemUser 表（管理员）
    │     └─ BCrypt.Verify(password, admin.Password) → 通过 → 角色 Admin
    │
    ├─ ② 查 Teacher 表（教师）
    │     └─ BCrypt.Verify(password, teacher.Password) → 通过 → 角色 Teacher/Counselor
    │
    └─ ③ 查 Student 表（学生）
          └─ BCrypt.Verify(password, student.Password) → 通过 → 角色 Student
    │
    ▼
ITokenService.GenerateToken(userId, userName, role)
    │
    │  使用 JwtConfig.SecretKey 签发 JWT
    │  Claims: UserId, UserName, Role
    │  ExpireMinutes: 120 分钟
    ▼
返回 LoginResult { Token, UserId, UserName, Role }
    │
    ▼
前端 TokenService 将 Token 存入 localStorage（key: campus_token）
    │
    ▼
CustomAuthStateProvider 从 localStorage 读取 Token
    │
    │  解析 JWT Claims → 构建 ClaimsPrincipal → AuthenticationState
    ▼
Blazor 授权系统根据角色渲染对应页面
```

### 请求认证

```
后续每次 API 调用：
    ApiClient.SendAsync<T>
        │
        ├─ 从 localStorage 读取 Token
        ├─ 附加 Header: Authorization: Bearer {token}
        └─ 发送请求
            │
            ▼
        Api 中间件管道
            │
            ├─ UseAuthentication → JWT Bearer 校验
            │   └─ 验证签名、过期时间、Issuer、Audience
            │
            ├─ UseAuthorization → 策略校验
            │   └─ [Authorize(Policy="RequireAdmin")] 等
            │
            └─ ICurrentUser (Scoped) → 从 HttpContext.User 提取 UserId/Role
                └─ 传递给 Service 层使用
```

## 4. 数据库切换

系统通过 `DbConfig.ProviderType` 配置项在 SQLite 与 MySQL 之间切换，由 `SqlSugarDbContext` 在构造时根据配置创建对应的 `SqlSugarClient`。

### 配置方式

**开发环境（SQLite）** — `appsettings.Development.json`：

```json
{
  "Db": {
    "ProviderType": "SQLite",
    "ConnectionString": "DataSource=attendance.db"
  }
}
```

**生产环境（MySQL）** — 环境变量覆盖：

```
Db__ProviderType=MySQL
Db__ConnectionString=Server=db;Port=3306;Database=attendance;Uid=root;Pwd=root;
```

### 切换原理

```
SqlSugarDbContext 构造函数
    │
    ├─ 读取 IOptions<DbConfig>
    │
    ├─ ResolveDbType(ProviderType)
    │   ├─ SQLite → DbType.Sqlite
    │   └─ MySQL  → DbType.MySql
    │
    └─ new SqlSugarClient(new ConnectionConfig {
           DbType = dbType,
           ConnectionString = config.ConnectionString,
           IsAutoCloseConnection = true
       })
```

### 自动建表

`DbInitializer.InitializeAsync()` 在 Api 启动时调用 `db.CodeFirst.InitTables(...)` 自动创建所有实体表，无需手动执行迁移脚本。

## 5. 角色权限

### 角色定义

| 角色 | 枚举值 | 说明 | 典型操作 |
|------|--------|------|---------|
| 管理员 | `UserRole.Admin` | 系统管理员 | 全部操作：用户管理、组织架构、课程管理、统计 |
| 任课教师 | `UserRole.Teacher` | 任课教师 | 发起考勤、点名、查看自己课程的出勤 |
| 辅导员 | `UserRole.Counselor` | 辅导员 | 审批请假、查看所属班级学生考勤 |
| 学生 | `UserRole.Student` | 学生 | 签到、请假申请、查看自己的考勤记录 |

### 授权策略

在 `Api/Program.cs` 中注册基于角色的策略：

```csharp
options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
options.AddPolicy("RequireTeacher", policy => policy.RequireRole("Teacher", "Counselor"));
options.AddPolicy("RequireStudent", policy => policy.RequireRole("Student"));
options.AddPolicy("RequireCounselor", policy => policy.RequireRole("Counselor"));
```

### 前端路由守卫

Blazor Server 通过 `CustomAuthStateProvider` 提供认证状态，`AccessGuard` 组件根据角色控制页面访问：

- 未登录 → 重定向 `/login`
- 角色不匹配 → 显示无权限提示
- 各角色使用独立 Layout（AdminLayout / TeacherLayout / StudentLayout）

## 6. 考勤签到流程

### 完整流程

```
任课教师
    │
    │ ① 选择课程 + 班级，创建考勤会话
    ▼
AttendanceService.CreateSessionAsync
    │
    ├─ 校验课程属于该教师
    ├─ 校验班级存在
    ├─ 创建 AttendanceSession（状态 Active）
    ├─ 生成 QrToken（短期 JWT，含 SessionId，有效期 30 秒）
    └─ 使用 QRCoder 生成二维码 PNG（Base64）
    │
    ▼
教师页面显示二维码（每 30 秒自动刷新）
    │
    │ ② 学生使用手机扫描二维码
    ▼
学生浏览器打开签到页面（携带 QrToken）
    │
    │ ③ 学生点击「签到」
    ▼
AttendanceService.CheckInAsync(sessionId, studentId, qrToken)
    │
    ├─ 校验 QrToken 有效性（JWT 解析 + 过期检查）
    ├─ 校验会话状态为 Active
    ├─ 校验学生属于该班级
    ├─ 判定签到状态：
    │   ├─ 在开始时间 ±10 分钟内 → AttendanceStatus.Present（正常）
    │   ├─ 超过开始时间 10 分钟 → AttendanceStatus.Late（迟到）
    │   └─ 超过开始时间 30 分钟 → AttendanceStatus.Absent（缺勤）
    └─ 写入 AttendanceRecord
    │
    ▼
返回签到结果（状态 + 时间）
```

### 一键点名

教师可在会话进行中发起随机点名：

```
AttendanceService.RandomPickAsync(sessionId)
    │
    ├─ 查询该班级所有学生
    ├─ 排除最近已被点名的学生（_randomPickHistory 缓存）
    ├─ 随机选取一名学生
    ├─ 记录到 _randomPickHistory
    └─ 返回被点名学生信息
    │
    ▼
教师手动标记：到场 / 缺席 → 更新 AttendanceRecord
```

## 7. 请假审批流程

### 完整流程

```
学生
    │
    │ ① 提交请假申请（选择请假类型、时间区间、填写原因）
    ▼
LeaveService.CreateLeaveAsync(dto, studentId)
    │
    ├─ 校验学生存在
    ├─ 校验时间区间有效（EndTime > StartTime）
    ├─ 从学生所属班级获取辅导员 Id（Class.CounselorId）
    ├─ 创建 LeaveRequest（状态 Pending）
    └─ 记录日志
    │
    ▼
辅导员
    │
    │ ② 在「请假审批」页面查看待审批列表
    ▼
辅导员审批
    │
    │ ③ 通过 / 驳回
    ▼
LeaveService.ApproveAsync(leaveId, counselorId) / RejectAsync(...)
    │
    ├─ 校验该请假属于当前辅导员
    ├─ 更新 LeaveRequest.Status = Approved / Rejected
    ├─ 记录审批意见与时间
    │
    └─ 【审批通过时】联动更新考勤记录
        │
        ├─ 查询请假时间区间内的考勤记录
        └─ 将状态更新为 AttendanceStatus.Leave（请假）
    │
    ▼
学生可在「我的请假」页面查看审批结果
```

### 考勤联动

请假审批通过后，系统自动将请假时间区间内该学生的考勤记录状态更新为 `Leave`（请假），避免被记为缺勤。这一联动逻辑在 `LeaveService.ApproveAsync` 中实现，确保考勤数据与请假记录一致。

## 8. Docker 部署架构

```
┌─────────────────────────────────────────────────────┐
│                   Docker 网络                        │
│                                                     │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐      │
│  │   db     │    │   api    │    │   web    │      │
│  │ MySQL 8.0│    │ ASP.NET  │    │ Blazor   │      │
│  │          │◄───│ API      │◄───│ Server   │      │
│  │ :3306    │    │ :8080    │    │ :8080    │      │
│  └────┬─────┘    └──────────┘    └──────────┘      │
│       │                                              │
│       │ mysql_data 卷                               │
│       ▼                                              │
│  ┌──────────┐                                       │
│  │ 数据卷   │                                       │
│  │ /var/lib │                                       │
│  │ /mysql   │                                       │
│  └──────────┘                                       │
└─────────────────────────────────────────────────────┘

外部访问：
  - Web UI:  http://localhost:8080  →  web:8080
  - API:     http://localhost:5000  →  api:8080
  - MySQL:   http://localhost:3306  →  db:3306（开发调试用）
```

### 启动顺序

1. `db` 启动 → 健康检查 `mysqladmin ping`（start_period 30s）
2. `db` healthy → `api` 启动 → 自动建表 + 种子数据 → 健康检查 HTTP（start_period 40s）
3. `api` healthy → `web` 启动 → 健康检查 HTTP（start_period 40s）

### 容器间通信

- Web → Api：通过 Docker 内部 DNS `http://api:8080`（环境变量 `Api__BaseUrl`）
- Api → DB：通过 Docker 内部 DNS `Server=db;Port=3306`（环境变量 `Db__ConnectionString`）
