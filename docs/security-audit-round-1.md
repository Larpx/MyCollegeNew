# 第一轮安全审计报告

**审计日期**：2026-06-29
**审计范围**：`d:\Work\repos\my-college-project` 全代码库
**审计方法**：自动化安全审计工具，按四个攻击面系统性检查（认证与访问控制 / 注入向量 / 外部交互 / 敏感数据处理）
**审计标准**：识别中等严重度及以上的已确认漏洞，且必须具备可论证的端到端利用路径

---

## 一、审计概览

本轮审计共发现 **7 个问题**，其中：

- **5 个已修复**（问题 2、4、5、6、7）
- **2 个为本地测试预留**（问题 1、3），生产环境自动屏蔽或手动处理，无需修复

### 问题汇总

| 编号 | 严重度 | 问题 | 状态 |
|------|--------|------|------|
| 1 | HIGH | DEBUG TOTP 后门 `888888` | 本地测试预留，生产环境自动屏蔽 |
| 2 | HIGH | 硬编码 JWT 密钥 | 已修复 |
| 3 | MEDIUM | 弱默认密码 `123456` | 本地测试预留，生产环境手动清空数据库重新播种 |
| 4 | HIGH | 考勤会话详情 IDOR | 已修复 |
| 5 | HIGH | 考勤签到记录 IDOR | 已修复 |
| 6 | HIGH | 请假详情 IDOR | 已修复 |
| 7 | HIGH | 课程/课表查询 IDOR | 已修复 |

---

## 二、问题详情与修复记录

### 问题 1：DEBUG TOTP 后门（本地测试预留，无需修复）

- **严重度**：HIGH
- **位置**：`src/MyCollegeNew.Infrastructure/Auth/TotpService.cs:37-40`
- **描述**：`#if DEBUG` 编译指令下，TOTP 验证码输入 `888888` 直接通过
- **处理方式**：用户声明为本地测试预留，生产环境以 Release 配置编译自动屏蔽

### 问题 2：硬编码 JWT 密钥（已修复）

- **严重度**：HIGH
- **位置**：`src/MyCollegeNew.Api/appsettings.json`
- **描述**：JWT `SecretKey` 硬编码为 `MyCollegeNew.SecretKey.2026.DoNotHardCode.InProduction`，攻击者可伪造任意用户 token
- **影响**：攻击者可伪造管理员 JWT，完全接管系统
- **修复方案**：将 `appsettings.json` 中 `SecretKey` 置空，强制生产环境通过环境变量 `Jwt__SecretKey` 注入；`Program.cs:105-108` 已有启动时非空校验
- **修复文件**：[appsettings.json](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/appsettings.json)
- **修复状态**：已完成

### 问题 3：弱默认密码（本地测试预留，无需修复）

- **严重度**：MEDIUM
- **位置**：`src/MyCollegeNew.Infrastructure/Data/DbInitializer.cs`
- **描述**：种子数据中默认管理员账号密码为 `123456`
- **处理方式**：用户声明生产环境会手动清空数据库并重新播种生产专用数据

### 问题 4 & 5：考勤会话/记录 IDOR（已修复）

- **严重度**：HIGH
- **位置**：`src/MyCollegeNew.Api/Features/Attendance/AttendanceHandlers.cs`
- **描述**：
  - 问题 4：`GET /api/v1/sessions/{id}` 任何已登录用户可查看任意考勤会话详情
  - 问题 5：`GET /api/v1/sessions/{id}/records` 任何已登录用户可查看任意考勤签到记录
- **影响**：学生可查看其他班级的考勤数据，教师可查看非自己创建的考勤会话
- **修复方案**：
  - 注入 `ICurrentUser` 依赖
  - 新增 `CanAccessSessionAsync` 辅助方法实现基于角色的访问控制：
    - 管理员可访问所有会话
    - 教师/辅导员必须是该会话的授课教师
    - 学生必须属于该会话的班级
  - 在 `GetSessionByIdQuery` 和 `GetSessionRecordsQuery` handler 中调用权限校验
- **修复文件**：[AttendanceHandlers.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Attendance/AttendanceHandlers.cs)
- **修复状态**：已完成

### 问题 6：请假详情 IDOR（已修复）

- **严重度**：HIGH
- **位置**：`src/MyCollegeNew.Api/Features/Leave/LeaveHandlers.cs`
- **描述**：`GET /api/v1/leaves/{id}` 任何已登录用户可查看任意请假申请详情
- **影响**：学生可查看其他学生的请假申请，包含请假原因等隐私信息
- **修复方案**：
  - 注入 `ICurrentUser` 依赖
  - 在 `GetLeaveByIdQuery` handler 中添加权限校验：
    - 管理员可查看所有
    - 学生仅可查看自己提交的
    - 辅导员仅可查看分配给自己的
- **修复文件**：[LeaveHandlers.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Leave/LeaveHandlers.cs)
- **修复状态**：已完成

### 问题 7：课程/课表查询 IDOR（已修复）

- **严重度**：HIGH
- **位置**：`src/MyCollegeNew.Api/Features/Courses/CourseHandlers.cs`
- **描述**：以下 4 个端点仅使用 `RequireAuthorization()` 无角色限制，任何已登录用户可查询任意数据：
  - `GET /api/v1/courses/by-teacher/{teacherId}` — 任何用户可查询任意教师课程
  - `GET /api/v1/schedules/weekly/teacher/{teacherId}` — 任何用户可查询任意教师周课表
  - `GET /api/v1/schedules/weekly/student/{studentId}` — 任何用户可查询任意学生周课表
  - `GET /api/v1/schedules/weekly/class/{classId}` — 任何用户可查询任意班级周课表
- **影响**：学生可探测任意教师/学生/班级的排课信息
- **修复方案**：
  - 注入 `ICurrentUser` 依赖
  - 新增 `CanAccessClassScheduleAsync` 辅助方法实现基于角色的访问控制：
    - 管理员可访问所有
    - 教师/辅导员需在该班级有排课
    - 学生需属于该班级
  - 各端点权限校验规则：
    - `GetCoursesByTeacherQuery`：学生拒绝；教师/辅导员仅可查自己
    - `GetScheduleByTeacherQuery`：学生拒绝；教师/辅导员仅可查自己
    - `GetScheduleByStudentQuery`：学生仅可查自己
    - `GetScheduleByClassQuery`：通过 `CanAccessClassScheduleAsync` 校验
- **修复文件**：[CourseHandlers.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Api/Features/Courses/CourseHandlers.cs)
- **修复状态**：已完成

---

## 三、测试同步更新

为适配 `ICurrentUser` 注入，同步更新了测试基础设施：

- [TestDbContext.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Tests/Infrastructure/TestDbContext.cs)：新增 `TestCurrentUser` 测试用户实现，提供 `Admin`/`Teacher`/`Student` 三种角色工厂
- 4 个测试文件构造函数调用已同步更新：
  - [AttendanceServiceTests.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Tests/Attendance/AttendanceServiceTests.cs)
  - [ScheduleServiceTests.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Tests/Courses/ScheduleServiceTests.cs)
  - [CourseServiceTests.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Tests/Courses/CourseServiceTests.cs)
  - [LeaveServiceTests.cs](file:///d:/Work/repos/my-college-project/src/MyCollegeNew.Tests/Leave/LeaveServiceTests.cs)

---

## 四、构建验证

- `dotnet build`：0 错误 0 警告
- `dotnet test`：70/70 全部通过

---

## 五、本轮审计结论

本轮审计发现并修复了 5 个高危漏洞（硬编码 JWT 密钥 + 4 个 IDOR 越权访问漏洞），2 个本地测试预留问题按用户声明无需修复。所有修复均通过构建验证与单元测试。
