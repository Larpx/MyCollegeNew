# Campus.Attendance.Models

数据传输对象（DTO）层，定义 API 请求与响应的数据结构，隔离实体层与接口层。

## 项目职责

- 定义各业务模块的请求 DTO（CreateRequestDto / UpdateRequestDto / QueryDto）
- 定义各业务模块的响应 DTO（ResponseDto / ListItemDto）
- 携带数据验证特性（`[Required]`、`[StringLength]`、`[Range]`）

## 关键目录与类型

| 目录 | 关键类型 | 说明 |
|------|---------|------|
| `Auth/` | `LoginRequest`, `LoginResult` | 登录请求与返回结果 |
| `Users/` | `StudentDtos`, `TeacherDtos`, `PasswordAndBatchDtos` | 学生/教师增删改查 DTO、密码重置与批量导入 |
| `Organization/` | `OrganizationDtos` | 院系/专业/班级 DTO |
| `Courses/` | `CourseDtos`, `ScheduleDtos` | 课程与排课 DTO |
| `Attendance/` | `AttendanceDtos` | 考勤会话、签到、点名 DTO |
| `Leave/` | `LeaveDtos` | 请假申请与审批 DTO |
| `Statistics/` | `StatisticsDtos` | 统计报表 DTO |

## 依赖关系

- 引用 `Campus.Attendance.Core`（复用枚举与实体）
- 被 Services、Api、Web、Tests 项目引用
