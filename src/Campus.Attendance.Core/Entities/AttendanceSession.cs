using Campus.Attendance.Core.Enums;
using SqlSugar;

namespace Campus.Attendance.Core.Entities;

/// <summary>
/// 考勤会话实体，由教师发起的一次签到活动，包含二维码 token 与时间窗口
/// </summary>
[SugarTable("attendance_session")]
public class AttendanceSession : EntityBase
{
    /// <summary>考勤会话主键，自增</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "考勤会话主键")]
    public long Id { get; set; }

    /// <summary>课程 Id</summary>
    [SugarColumn(ColumnDescription = "课程 Id")]
    public long CourseId { get; set; }

    /// <summary>班级 Id</summary>
    [SugarColumn(ColumnDescription = "班级 Id")]
    public long ClassId { get; set; }

    /// <summary>发起教师工号（关联 Teacher.Id）</summary>
    [SugarColumn(Length = 32, ColumnDescription = "发起教师工号")]
    public string TeacherId { get; set; } = string.Empty;

    /// <summary>关联课表 Id（可为空，表示临时发起）</summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "关联课表 Id")]
    public long? ScheduleId { get; set; }

    /// <summary>签到开始时间（UTC）</summary>
    [SugarColumn(ColumnDescription = "签到开始时间")]
    public DateTime StartTime { get; set; }

    /// <summary>签到结束时间（UTC）</summary>
    [SugarColumn(ColumnDescription = "签到结束时间")]
    public DateTime EndTime { get; set; }

    /// <summary>会话状态（Active=进行中, Closed=已关闭）</summary>
    [SugarColumn(ColumnDescription = "会话状态")]
    public SessionStatus Status { get; set; }

    /// <summary>二维码签名 token（短期有效，用于扫码签到校验）</summary>
    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "二维码 token")]
    public string? QrToken { get; set; }
}
