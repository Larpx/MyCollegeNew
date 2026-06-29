using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Attendance
{
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

        /// <summary>关联课表 Id</summary>
        public long? ScheduleId { get; set; }

        /// <summary>签到开始时间</summary>
        [Required(ErrorMessage = "签到开始时间不能为空")]
        public DateTime StartTime { get; set; }

        /// <summary>签到结束时间</summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 考勤会话响应 DTO
    /// </summary>
    public class SessionResponseDto
    {
        public long Id { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public long ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public long? ScheduleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public SessionStatus Status { get; set; }
        public string? QrToken { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 二维码生成结果
    /// </summary>
    public class QrCodeResult
    {
        public string Token { get; set; } = string.Empty;
        public string Base64Image { get; set; } = string.Empty;
        public int ExpireSeconds { get; set; }
        public DateTime GenerateTime { get; set; }
    }

    /// <summary>
    /// 签到请求 DTO
    /// </summary>
    public class CheckInRequestDto
    {
        [Required(ErrorMessage = "签到令牌不能为空")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// 签到结果
    /// </summary>
    public class CheckInResult
    {
        public AttendanceStatus Status { get; set; }
        public DateTime CheckInTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 考勤记录响应 DTO
    /// </summary>
    public class AttendanceRecordResponseDto
    {
        public long Id { get; set; }
        public long SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string? Remark { get; set; }

        /// <summary>请假事由（当存在已批准请假时叠加返回）</summary>
        public string? LeaveReason { get; set; }

        /// <summary>请假审批备注（辅导员填写）</summary>
        public string? LeaveRemark { get; set; }
    }

    /// <summary>
    /// 修改考勤记录状态 DTO
    /// </summary>
    public class UpdateRecordStatusDto
    {
        [Required(ErrorMessage = "考勤状态不能为空")]
        public AttendanceStatus Status { get; set; }
    }

    /// <summary>
    /// 教师手动补签 DTO
    /// </summary>
    public class ManualCheckInDto
    {
        [Required(ErrorMessage = "学生学号不能为空")]
        [StringLength(32, ErrorMessage = "学号长度不能超过 32 个字符")]
        public string StudentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "考勤状态不能为空")]
        public AttendanceStatus Status { get; set; }
    }

    /// <summary>
    /// 随机点名结果
    /// </summary>
    public class RandomPickResult
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public long ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 随机点名结果标记 DTO
    /// </summary>
    public class MarkRandomPickDto
    {
        [Required(ErrorMessage = "学生学号不能为空")]
        public string StudentId { get; set; } = string.Empty;
        public bool Answered { get; set; }
    }
}