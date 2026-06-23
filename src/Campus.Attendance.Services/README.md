# Campus.Attendance.Services

服务层，实现全部业务逻辑，包括认证、考勤、请假、课程、组织架构、统计等模块。

## 项目职责

- 实现核心接口（IAuthService、IAttendanceService 等）
- 封装数据库操作（通过 SqlSugar IDbContext）
- 实现数据库初始化与种子数据播种（DbInitializer）
- 生成 JWT 令牌（TokenService）与二维码（AttendanceService 内集成 QRCoder）
- 实现 Excel 导出（StatisticsService 内集成 ClosedXML）

## 关键目录与类型

| 目录 | 关键类型 | 说明 |
|------|---------|------|
| `Data/` | `SqlSugarDbContext`, `DbInitializer` | 数据库上下文实现、CodeFirst 建表与种子数据 |
| `Auth/` | `AuthService`, `TokenService`, `CurrentUserService` | 登录认证、JWT 签发、当前用户上下文 |
| `Attendance/` | `AttendanceService` | 考勤会话管理、二维码生成、学生签到、随机点名 |
| `Leave/` | `LeaveService` | 请假申请、辅导员审批、考勤记录联动 |
| `Courses/` | `CourseService`, `ScheduleService` | 课程管理、排课管理 |
| `Organization/` | `OrganizationService` | 院系/专业/班级增删改查 |
| `Users/` | `UserService` | 学生/教师管理、密码重置、批量导入 |
| `Statistics/` | `StatisticsService` | 出勤统计、Excel 报表导出 |

## 依赖关系

- 引用 `Campus.Attendance.Core`、`Campus.Attendance.Models`
- NuGet 包：`SqlSugarCore`、`BCrypt.Net-Next`、`QRCoder`、`ClosedXML`、`System.IdentityModel.Tokens.Jwt`
- 被 Api、Web、Tests 项目引用
