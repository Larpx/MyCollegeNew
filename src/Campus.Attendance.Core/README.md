# Campus.Attendance.Core

核心层，定义系统的基础实体、枚举、接口与配置，不依赖任何其他业务层。

## 项目职责

- 定义所有数据库实体（EntityBase 抽象基类 + 12 个业务实体）
- 定义系统枚举（角色、状态、类型）
- 定义数据库与 JWT 配置模型
- 定义核心接口（IDbContext、ICurrentUser、ITokenService）
- 定义统一响应模型（ApiResponse<T>）与业务异常（BusinessException）

## 关键目录与类型

| 目录 | 关键类型 | 说明 |
|------|---------|------|
| `Entities/` | `EntityBase`, `SystemUser`, `Teacher`, `Student`, `Department`, `Major`, `Class`, `Course`, `CourseSchedule`, `AttendanceSession`, `AttendanceRecord`, `LeaveRequest`, `AuditLog` | 数据库实体，SqlSugar CodeFirst 映射 |
| `Enums/` | `UserRole`, `TeacherRole`, `AttendanceStatus`, `SessionStatus`, `LeaveStatus`, `LeaveType` | 业务枚举 |
| `Configuration/` | `DbConfig`, `ProviderType`, `IDbContext` | 数据库配置与上下文接口 |
| `Security/` | `JwtConfig`, `ITokenService`, `ICurrentUser` | JWT 配置与认证接口 |
| `Responses/` | `ApiResponse<T>` | 统一 API 响应包装 |
| `Exceptions/` | `BusinessException` | 业务异常，携带 HTTP 状态码 |
| `Constants/` | `AttendanceConstants` | 考勤业务常量 |

## 依赖关系

- **无项目依赖**（仅依赖 .NET 基础类库与 SqlSugar 接口）
- 被 Models、Services、Api、Web、Tests 项目引用
