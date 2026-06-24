using System.ComponentModel.DataAnnotations;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Leave;

/// <summary>请假创建 DTO</summary>
public class LeaveCreateDto
{
    [Required(ErrorMessage = "请假开始时间不能为空")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "请假结束时间不能为空")]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "请假类型不能为空")]
    public LeaveType LeaveType { get; set; }

    [Required(ErrorMessage = "请假事由不能为空")]
    [StringLength(512, ErrorMessage = "请假事由长度不能超过 512 个字符")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>审批备注 DTO</summary>
public class LeaveReviewDto
{
    [StringLength(256, ErrorMessage = "审批备注长度不能超过 256 个字符")]
    public string? ReviewRemark { get; set; }
}

/// <summary>请假响应 DTO</summary>
public class LeaveResponseDto
{
    public long Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string CounselorId { get; set; } = string.Empty;
    public string CounselorName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public LeaveType LeaveType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public string? ReviewRemark { get; set; }
    public DateTime? ReviewTime { get; set; }
    public DateTime CreateTime { get; set; }
}
