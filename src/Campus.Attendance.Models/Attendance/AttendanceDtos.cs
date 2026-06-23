using System.ComponentModel.DataAnnotations;
using Campus.Attendance.Core.Enums;

namespace Campus.Attendance.Models.Attendance;

/// <summary>
/// 考勤会话创建 DTO
/// </summary>
public class SessionCreateDto
{
    /// <summary>课程 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
    public long CourseId { get; set; }

    /// <summary>班级 Id</summary>
    [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
    public long ClassId { get; set; }

    /// <summary>关联课表 Id（可为空，表示临时发起）</summary>
    public long? ScheduleId { get; set; }

    /// <summary>签到开始时间（UTC）</summary>
    [Required(ErrorMessage = "签到开始时间不能为空")]
    public DateTime StartTime { get; set; }

    /// <summary>签到结束时间（UTC），不传则默认开始时间后 30 分钟</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 考勤会话响应 DTO，包含课程名、班级名、教师名等冗余信息
/// </summary>
public class SessionResponseDto
{
    /// <summary>会话 Id</summary>
    public long Id { get; set; }

    /// <summary>课程 Id</summary>
    public long CourseId { get; set; }

    /// <summary>课程名称</summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>班级 Id</summary>
    public long ClassId { get; set; }

    /// <summary>班级名称</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>发起教师工号</summary>
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>发起教师姓名</summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>关联课表 Id</summary>
    public long? ScheduleId { get; set; }

    /// <summary>签到开始时间（UTC）</summary>
    public DateTime StartTime { get; set; }

    /// <summary>签到结束时间（UTC）</summary>
    public DateTime EndTime { get; set; }

    /// <summary>会话状态</summary>
    public SessionStatus Status { get; set; }

    /// <summary>当前二维码 token（仅教师可见）</summary>
    public string? QrToken { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 二维码生成结果
/// </summary>
public class QrCodeResult
{
    /// <summary>签到令牌</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Base64 编码的二维码图片</summary>
    public string Base64Image { get; set; } = string.Empty;

    /// <summary>过期时间（秒）</summary>
    public int ExpireSeconds { get; set; }

    /// <summary>生成时间（UTC）</summary>
    public DateTime GenerateTime { get; set; }
}

/// <summary>
/// 签到请求 DTO
/// </summary>
public class CheckInRequestDto
{
    /// <summary>二维码 token</summary>
    [Required(ErrorMessage = "签到令牌不能为空")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// 签到结果
/// </summary>
public class CheckInResult
{
    /// <summary>签到后的考勤状态</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>签到时间（UTC）</summary>
    public DateTime CheckInTime { get; set; }

    /// <summary>提示信息</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 考勤记录响应 DTO
/// </summary>
public class AttendanceRecordResponseDto
{
    /// <summary>记录 Id</summary>
    public long Id { get; set; }

    /// <summary>会话 Id</summary>
    public long SessionId { get; set; }

    /// <summary>学生学号</summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>学生姓名</summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>考勤状态</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>签到时间（UTC）</summary>
    public DateTime? CheckInTime { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 修改单条考勤记录状态的 DTO
/// </summary>
public class UpdateRecordStatusDto
{
    /// <summary>新的考勤状态</summary>
    [Required(ErrorMessage = "考勤状态不能为空")]
    public AttendanceStatus Status { get; set; }
}

/// <summary>
/// 教师手动补签 DTO
/// </summary>
public class ManualCheckInDto
{
    /// <summary>学生学号</summary>
    [Required(ErrorMessage = "学生学号不能为空")]
    [StringLength(32, ErrorMessage = "学号长度不能超过 32 个字符")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>考勤状态（教师指定）</summary>
    [Required(ErrorMessage = "考勤状态不能为空")]
    public AttendanceStatus Status { get; set; }
}

/// <summary>
/// 随机点名结果
/// </summary>
public class RandomPickResult
{
    /// <summary>学生学号</summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>学生姓名</summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>班级 Id</summary>
    public long ClassId { get; set; }

    /// <summary>班级名称</summary>
    public string ClassName { get; set; } = string.Empty;
}

/// <summary>
/// 随机点名结果标记 DTO
/// </summary>
public class MarkRandomPickDto
{
    /// <summary>学生学号</summary>
    [Required(ErrorMessage = "学生学号不能为空")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>是否已回答</summary>
    public bool Answered { get; set; }
}
