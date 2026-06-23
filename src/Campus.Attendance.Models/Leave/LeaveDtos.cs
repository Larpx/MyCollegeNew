using System.ComponentModel.DataAnnotations;
using Campus.Attendance.Core.Enums;

namespace Campus.Attendance.Models.Leave;

/// <summary>
/// 学生请假申请创建 DTO
/// </summary>
public class LeaveCreateDto
{
    /// <summary>请假开始时间（UTC）</summary>
    [Required(ErrorMessage = "请假开始时间不能为空")]
    public DateTime StartTime { get; set; }

    /// <summary>请假结束时间（UTC）</summary>
    [Required(ErrorMessage = "请假结束时间不能为空")]
    public DateTime EndTime { get; set; }

    /// <summary>请假类型</summary>
    [Required(ErrorMessage = "请假类型不能为空")]
    public LeaveType LeaveType { get; set; }

    /// <summary>请假事由</summary>
    [Required(ErrorMessage = "请假事由不能为空")]
    [StringLength(512, ErrorMessage = "请假事由长度不能超过 512 个字符")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 辅导员审批备注 DTO
/// </summary>
public class LeaveReviewDto
{
    /// <summary>审批备注</summary>
    [StringLength(256, ErrorMessage = "审批备注长度不能超过 256 个字符")]
    public string? ReviewRemark { get; set; }
}

/// <summary>
/// 请假申请响应 DTO
/// </summary>
public class LeaveResponseDto
{
    /// <summary>请假申请 Id</summary>
    public long Id { get; set; }

    /// <summary>申请学生学号</summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>申请学生姓名</summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>审批辅导员工号</summary>
    public string CounselorId { get; set; } = string.Empty;

    /// <summary>审批辅导员姓名</summary>
    public string CounselorName { get; set; } = string.Empty;

    /// <summary>请假开始时间（UTC）</summary>
    public DateTime StartTime { get; set; }

    /// <summary>请假结束时间（UTC）</summary>
    public DateTime EndTime { get; set; }

    /// <summary>请假类型</summary>
    public LeaveType LeaveType { get; set; }

    /// <summary>请假事由</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>审批状态</summary>
    public LeaveStatus Status { get; set; }

    /// <summary>审批备注</summary>
    public string? ReviewRemark { get; set; }

    /// <summary>审批时间（UTC）</summary>
    public DateTime? ReviewTime { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
}
