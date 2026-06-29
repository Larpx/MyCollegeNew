# 考勤与课程管理系统增强设计

- 日期：2026-06-29
- 范围：教师端 / 管理员端 / 学生端
- 状态：待审核

## 1. 背景与目标

当前系统已具备基础考勤（二维码签到、随机点名、一键点名）、课程管理、课表查询、请假审批能力，但存在以下缺口：

1. **DEBUG 模式 2FA 体验差**：每次测试需查动态密码，需 `888888` 后门
2. **教师仪表盘 Bug**：调用错误 URL 导致"响应格式错误"
3. **新建会话页缺失班级选择**：只能从仪表盘今日课程入口进入，无法主动选择
4. **角色单一**：仅 `Teacher / Counselor`，缺少系主任（系管理员）角色
5. **排课能力薄弱**：无课程模板、无教师主动接课、无合班课、无冲突校验
6. **调换课缺失**：教师无法主动发起调换课
7. **学生闭环未打通**：课表已有但请假与考勤端联动不足
8. **随机提问不严谨**：从全班抽取而非已签到学生

本设计目标：补全排课、调换课、合班课、请假联动、随机提问改造，使系统达到市面成熟考勤系统水准。

## 2. 立即修复的三个 Bug

### 2.1 DEBUG 模式 2FA `888888` 后门

在 `TotpService.VerifyCode` 中加 `#if DEBUG` 编译开关，Release 构建自动剔除：

```csharp
public bool VerifyCode(string secret, string code)
{
#if DEBUG
    if (code == "888888") return true;
#endif
    // 原有 TOTP 校验逻辑
}
```

所有走 `TotpService.VerifyCode` 的入口（绑定 / 验证）自动生效。

### 2.2 教师仪表盘 URL Bug

`src/MyCollegeNew.Web/Components/Pages/Teacher/Dashboard.razor` 第 196 行调用错误 URL：

```
原：schedules/by-teacher/{_userId}?week=1
改：schedules/weekly/teacher/{_userId}?week=1
```

对齐 `CourseEndpoints.cs` 第 109 行实际路由。

### 2.3 新建会话页加班级选择下拉框

`Session.razor` 当前的"新建会话"表单只有只读 `courseId` / `classId` 输入框。改为：

- 当从仪表盘"今日课程"跳转时（带 `scheduleId`），保留原逻辑直接发起考勤
- 当用户从"考勤记录"页主动点"发起考勤"时（`mode=manual`），加载该教师可教的课程列表 + 每个课程对应的班级列表，让教师选择

## 3. 角色与权限模型

### 3.1 教师角色扩展

不修改 `TeacherRole` 枚举，改为在 Teacher 实体加标记位：

```csharp
// Teacher.cs 新增字段
/// <summary>是否为系主任（可同时为任课教师/辅导员）</summary>
[SugarColumn(ColumnDescription = "是否为系主任")]
public bool IsDepartmentHead { get; set; }

/// <summary>系主任所管辖院系 Id（非系主任为 null）</summary>
[SugarColumn(IsNullable = true, ColumnDescription = "系主任所管辖院系 Id")]
public long? HeadDepartmentId { get; set; }
```

### 3.2 权限分层

| 角色 | 课程相关 | 排课 | 调换课 | 考勤 | 请假审批 | 课表查看 |
|---|---|---|---|---|---|---|
| 系主任(任课) | 接课、查看自己课程 | 为本系所有班级排课 | 代任何教师发起调换课 | 自己课程 | 若同时为辅导员则可 | 自己 + 本系全部教师本周 |
| 任课教师 | 主动接课 | 不可 | 主动发起调换课 | 自己课程 | 不可 | 自己 |
| 辅导员 | 不可 | 不可 | 不可 | 不可 | 自己班级 | 自己（若有课） |
| 学生 | 不可 | 不可 | 不可 | 不可 | 不可 | 自己班级 |

### 3.3 JWT Claim 与授权策略

JWT 不携带系主任身份，运行时通过查库判断（可缓存）。新增授权策略：

- `RequireDepartmentHead`：校验当前教师 `IsDepartmentHead == true` 且操作的班级/课程属于其院系
- `RequireTeacherOrDepartmentHead`：原任课教师或其院系系主任均可

## 4. 数据模型扩展

### 4.1 Course 实体改造为"课程模板"语义

不重命名，仅语义化。新增字段：

```csharp
/// <summary>创建者工号（系主任发布课程模板时为系主任工号；教师主动开课时为申请人工号）</summary>
public string CreatorId { get; set; } = string.Empty;

/// <summary>课程状态（Draft 草稿 / OpenForPick 开放接课 / Closed 已关闭接课）</summary>
public CourseStatus Status { get; set; }
```

原 `TeacherId` 字段保留但语义变为"默认任课教师"，系主任发布时可为空，由接课分配填充。

### 4.2 CourseSchedule 改造支持合班课 + 接课分配

```csharp
// CourseSchedule.cs 字段调整
/// <summary>班级 Id 列表（逗号分隔，支持合班课，如 "1,2,3"）</summary>
[SugarColumn(Length = 128, ColumnDescription = "班级 Id 列表（合班课逗号分隔）")]
public string ClassIds { get; set; } = string.Empty;

/// <summary>原 ClassId 字段废弃（迁移时回填为 ClassIds 首个值），保留供旧代码兼容</summary>
[Obsolete("使用 ClassIds 替代，合班课支持")]
public long ClassId { get; set; }
```

为减少破坏性改动，`ClassId` 保留为 `ClassIds` 解析后的首个值（单班场景），新代码读写 `ClassIds`。

### 4.3 新增实体：CourseAssignment（任课分配 / 接课记录）

```csharp
[SugarTable("course_assignment")]
public class CourseAssignment : EntityBase
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>课程模板 Id（关联 Course.Id）</summary>
    public long CourseId { get; set; }

    /// <summary>任课教师工号</summary>
    [SugarColumn(Length = 32)]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>合班班级 Id 列表（逗号分隔）</summary>
    [SugarColumn(Length = 128)]
    public string ClassIds { get; set; } = string.Empty;

    /// <summary>学期标识（如 "2026-Spring"）</summary>
    [SugarColumn(Length = 16)]
    public string Semester { get; set; } = string.Empty;

    /// <summary>接课状态（Pending 待系主任确认 / Active 已生效 / Withdrawn 已撤回）</summary>
    public AssignmentStatus Status { get; set; }

    /// <summary>接课申请理由（教师主动接课时填写）</summary>
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? ApplyReason { get; set; }

    /// <summary>系主任审批备注</summary>
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? ReviewRemark { get; set; }
}
```

### 4.4 新增实体：CourseSwapRequest（调换课申请）

```csharp
[SugarTable("course_swap_request")]
public class CourseSwapRequest : EntityBase
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>原排课 Id（关联 CourseSchedule.Id）</summary>
    public long ScheduleId { get; set; }

    /// <summary>原任课教师工号（发起人）</summary>
    [SugarColumn(Length = 32)]
    public string OriginalTeacherId { get; set; } = string.Empty;

    /// <summary>代课教师工号（被委托人）</summary>
    [SugarColumn(Length = 32)]
    public string SubstituteTeacherId { get; set; } = string.Empty;

    /// <summary>代课起始周次</summary>
    public int StartWeek { get; set; }

    /// <summary>代课结束周次</summary>
    public int EndWeek { get; set; }

    /// <summary>调换原因</summary>
    [SugarColumn(Length = 256)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>状态（Pending 代课人待确认 / Accepted 已生效 / Rejected 已拒绝 / Cancelled 已撤销）</summary>
    public SwapStatus Status { get; set; }

    /// <summary>代课人确认备注</summary>
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? SubstituteRemark { get; set; }

    /// <summary>代课人确认时间</summary>
    public DateTime? ConfirmedTime { get; set; }
}
```

### 4.5 新增实体：CourseScheduleOverride（代课覆盖层）

```csharp
[SugarTable("course_schedule_override")]
public class CourseScheduleOverride : EntityBase
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>原排课 Id</summary>
    public long ScheduleId { get; set; }

    /// <summary>代课教师工号（生效期间内为此教师）</summary>
    [SugarColumn(Length = 32)]
    public string SubstituteTeacherId { get; set; } = string.Empty;

    /// <summary>覆盖生效起始周</summary>
    public int StartWeek { get; set; }

    /// <summary>覆盖生效结束周</summary>
    public int EndWeek { get; set; }

    /// <summary>关联调换课申请 Id</summary>
    public long SwapRequestId { get; set; }
}
```

### 4.6 AttendanceSession 改造支持合班课

当前 `AttendanceSession` 只有单个 `ClassId`。为支持合班课考勤，新增 `ClassIds` 字段：

```csharp
// AttendanceSession.cs 字段调整
/// <summary>班级 Id 列表（逗号分隔，支持合班课，如 "1,2,3"）</summary>
[SugarColumn(Length = 128, ColumnDescription = "班级 Id 列表（合班课逗号分隔）")]
public string ClassIds { get; set; } = string.Empty;

/// <summary>原 ClassId 字段废弃（迁移时回填为 ClassIds 首个值），保留供旧代码兼容</summary>
[Obsolete("使用 ClassIds 替代，合班课支持")]
public long ClassId { get; set; }
```

签到校验改为：学生属于 `ClassIds` 中任一班级即可通过。一键点名与请假同步覆盖所有合班班级。

### 4.7 新增枚举

```csharp
public enum CourseStatus { Draft, OpenForPick, Closed }
public enum AssignmentStatus { Pending, Active, Withdrawn }
public enum SwapStatus { Pending, Accepted, Rejected, Cancelled }
```

### 4.8 排课冲突校验规则

创建/更新 CourseSchedule 时校验，避免重复排课：

1. **教师时段冲突**：同一教师在同一周次、星期、节次范围内，不能有两份排课（含代课覆盖层）。查询时 union `CourseSchedule` + `CourseScheduleOverride`（按当前周判断）
2. **班级时段冲突**：合班 `ClassIds` 中任一班级在同一时段不能有其他排课
3. **节次合法性**：`1 ≤ StartSection ≤ EndSection ≤ 12`，`1 ≤ StartWeek ≤ EndWeek ≤ 30`，`1 ≤ DayOfWeek ≤ 7`
4. **代课教师空闲校验**：发起调换课时，校验 `SubstituteTeacherId` 在 `[StartWeek, EndWeek]` 范围内、对应星期与节次无任何排课/覆盖

## 5. 排课三件套 API

### 5.1 课程排课（系主任）

路由前缀 `/api/courses` + `/api/schedules`，复用现有并扩展：

- `POST /courses/templates` — 系主任发布课程模板（Status=OpenForPick）
- `POST /course-assignments/apply` — 任课教师主动接课（Status=Pending）
- `POST /course-assignments/{id}/approve` — 系主任审批接课
- `POST /course-assignments/{id}/withdraw` — 教师撤回接课
- `GET /course-assignments/by-teacher/{teacherId}` — 教师查看自己的接课记录
- `GET /courses/templates/open` — 教师查看待接课程列表

### 5.2 班级排课（系主任）

扩展现有 `POST /schedules`：

- 接收 `ClassIds`（必填，合班场景传多 Id）
- 内部执行 4.7 节冲突校验
- `TeacherId` 必填（已生效接课分配的任课教师）

新增：

- `GET /schedules/conflict-check` — 排课前预检（前端实时提示冲突）
- `POST /schedules/batch` — 批量排课（同一课程在多班级多时段一次性排）

### 5.3 教师调换课

- `POST /course-swaps` — 原任课教师发起调换课
- `POST /course-swaps/{id}/accept` — 代课教师确认接受
- `POST /course-swaps/{id}/reject` — 代课教师拒绝
- `POST /course-swaps/{id}/cancel` — 原任课教师撤销
- `GET /course-swaps/my-requests` — 我发起的调换课
- `GET /course-swaps/pending` — 待我确认的调换课
- `GET /course-swaps/department` — 系主任查看本系全部调换课

调换课生效（accept）时事务内：

1. 更新 `CourseSwapRequest.Status = Accepted`
2. 创建 `CourseScheduleOverride` 记录覆盖层
3. 推送通知给原任课教师

## 6. 教师端 UI 与导航

### 6.1 NavMenu 调整

教师菜单根据 `IsDepartmentHead` 动态生成：

```
所有教师可见：
  - 仪表盘 /teacher/dashboard
  - 我的课程 /teacher/courses
  - 我的课表 /teacher/schedule              (新增)
  - 考勤记录 /teacher/attendance
  - 调换课 /teacher/swaps                    (新增)

系主任额外可见：
  - 课程模板 /teacher/templates              (新增)
  - 接课审批 /teacher/assignment-review       (新增)
  - 班级排课 /teacher/scheduling              (新增)
  - 本系课表 /teacher/department-schedule     (新增)
  - 调换课管理 /teacher/swaps/department      (新增)

辅导员额外可见：
  - 请假审批 /teacher/leaves
```

### 6.2 新增页面

| 路由 | 页面 | 说明 |
|---|---|---|
| `/teacher/schedule` | Schedule.razor | 教师本人周课表（周次切换、考虑代课覆盖层） |
| `/teacher/swaps` | Swaps.razor | 我发起的调换课 + 待我确认的 |
| `/teacher/swaps/new` | SwapCreate.razor | 发起调换课表单（选排课、代课教师、周次范围） |
| `/teacher/templates` | CourseTemplates.razor | 系主任发布课程模板 |
| `/teacher/assignment-review` | AssignmentReview.razor | 系主任审批接课申请 |
| `/teacher/scheduling` | Scheduling.razor | 系主任排课工作台（课程+班级+教师+时段+冲突预检） |
| `/teacher/department-schedule` | DepartmentSchedule.razor | 本系全部教师本周课表 |
| `/teacher/swaps/department` | DepartmentSwaps.razor | 系主任查看/管理本系调换课 |

### 6.3 教师仪表盘增强

修复 URL Bug 后新增：

- 系主任卡片：本系待审批接课数、本周调换课数、本系出勤率
- 调换课提醒：待我确认的调换课徽章
- 待接课程池入口（系主任）

## 7. 学生端课表与请假闭环

### 7.1 学生课表页

调用 `schedules/weekly/student/{studentId}` 返回，后端已实现按学生 ClassId 转查班级课表。增强：

- 周次切换组件
- 课程卡片显示：课程名、教师名、教室、节次、起止周
- 当天高亮当前正在上的课

### 7.2 学生请假已通过 → 考勤端展示

审批通过时已写入 `Status=Approved`。考勤端展示链路：

**后端**：创建考勤会话（或一键点名时）后台批量查询该班级当天有效请假（`LeaveRequest.Status=Approved` 且 `StartTime ≤ now ≤ EndTime`）的学生列表，自动写入 `AttendanceRecord`（Status=Leave, Remark="请假：{LeaveType}"）。`GET /sessions/{id}/records` 已返回全部记录，前端无需改 API。

**前端**：`RollCall.razor` 和 `Session.razor` 表格增加"请假人"分组筛选，已通过请假学生默认置顶显示，不可手动改为缺勤。

### 7.3 随机提问改造

当前 `RandomPickQuery` 从班级所有学生中随机抽取。改造为：

**API 调整**：`GET /classes/{classId}/random-pick?sessionId={id}` 改为从该 session 已签到（`Status=Present` 或 `Late`）的学生中抽取，排除请假与缺勤学生。

**Handler 改造**：

```csharp
// 原：var students = await db.Queryable<Student>().Where(s => s.ClassId == query.ClassId).ToListAsync();
// 新：从 AttendanceRecord 中查询该 session 已签到学生
var checkedInStudents = await db.Queryable<AttendanceRecord>()
    .Where(r => r.SessionId == query.SessionId 
                && (r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late)
                && !r.IsDeleted)
    .Select(r => new { r.StudentId, r.StudentName })
    .ToListAsync();
```

历史去重逻辑（`_randomPickHistory`）保留，避免同节课反复抽到同一学生。

## 8. 实施分阶段

为避免单次提交过大、便于回滚，分 4 个阶段：

### 阶段 1：Bug 修复（独立提交）

- DEBUG 2FA 后门
- 教师仪表盘 URL 修复
- 新建会话页班级选择下拉框
- 编译验证 + 提交

### 阶段 2：数据模型与角色扩展（独立提交）

- Teacher 加 `IsDepartmentHead` / `HeadDepartmentId`
- 新增 `CourseAssignment` / `CourseSwapRequest` / `CourseScheduleOverride` 实体
- 新增枚举 `CourseStatus` / `AssignmentStatus` / `SwapStatus`
- DbInitializer 种子数据（系主任账号）
- SqlSugar CodeFirst 同步表
- 编译验证 + 提交

### 阶段 3：排课与调换课后端（独立提交）

- 课程模板 CRUD + 接课申请审批 Handler
- 排课冲突校验服务 `ScheduleConflictService`
- 调换课申请/确认/拒绝 Handler + 覆盖层创建
- 单元测试覆盖冲突校验与代课生效
- 编译验证 + 提交

### 阶段 4：前端页面与体验完善（独立提交）

- 教师端 NavMenu 动态化
- Schedule / Swaps / SwapCreate / CourseTemplates / AssignmentReview / Scheduling / DepartmentSchedule 页面
- 学生课表增强
- 考勤端请假人展示 + 随机提问改造
- 教师仪表盘系主任卡片
- 编译验证 + 提交

## 9. 测试策略

沿用项目现有 `MyCollegeNew.Tests` xUnit 项目，AAA 模式：

- `ScheduleConflictServiceTests`：教师冲突、班级冲突、合班冲突、代课空闲校验、节次合法性边界
- `CourseAssignmentHandlersTests`：接课申请、系主任审批、撤回
- `CourseSwapHandlersTests`：发起、确认生效、覆盖层写入、拒绝、撤销、原任课人权限校验
- `AttendanceHandlersTests` 扩展：随机提问仅从已签到学生抽取、请假学生自动写入 Leave 记录
- 命名遵循 `Method_Scenario_ExpectedResult`

## 10. 约束与边界

- **不破坏现有 API**：现有 `GET /schedules/weekly/teacher/{teacherId}` 行为保留，新增覆盖层 union 查询
- **不修改 UserRole 枚举**：系主任通过 Teacher 标记位识别，JWT 不变
- **DEBUG 后门严格编译隔离**：`#if DEBUG` 包裹，Release 不含后门代码
- **数据迁移**：现有 `CourseSchedule.ClassId` → `ClassIds` 单值迁移脚本
- **现有考勤会话兼容**：CourseAssignment 未引入前已存在的 session 保持原样，新会话走新流程
- **合班课考勤**：合班 `AttendanceSession.ClassIds` 存多班，签到时校验学生属于任一合班班级即可

## 11. 验收清单

- [ ] DEBUG 模式下 2FA 输入 `888888` 可通过验证，Release 构建不含此分支
- [ ] 教师仪表盘加载无"响应格式错误"提示，今日课程列表正确显示
- [ ] 教师从"考勤记录"页可主动选择课程 + 班级发起考勤
- [ ] 系主任可发布课程模板，教师可主动接课，系主任审批后生效
- [ ] 系主任排课时校验教师时段冲突与班级时段冲突，合班课正常排课
- [ ] 教师可发起调换课，代课教师确认后生效，期间内课表显示代课教师
- [ ] 学生个人中心可查看本人周课表，支持周次切换
- [ ] 学生请假经辅导员审批通过后，考勤当天会话中自动显示为请假状态
- [ ] 考勤端随机提问仅从已签到学生中抽取
- [ ] 系主任个人中心可查看本系全部教师本周课表
- [ ] 所有新增功能配有对应单元测试，编译 0 错误 0 警告
