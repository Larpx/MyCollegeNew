using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
/// <summary>
/// 请假申请实体，由学生提交，辅导员审批
/// </summary>
[SugarTable("leave_request")]
public class LeaveRequest : EntityBase
{
    /// <summary>请假申请主键，自增</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "请假申请主键")]
    public long Id { get; set; }

    /// <summary>申请学生学号（关联 Student.Id）</summary>
    [SugarColumn(Length = 32, ColumnDescription = "学生学号")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>审批辅导员工号（关联 Teacher.Id）</summary>
    [SugarColumn(Length = 32, ColumnDescription = "审批辅导员工号")]
    public string CounselorId { get; set; } = string.Empty;

    /// <summary>请假开始时间（UTC）</summary>
    [SugarColumn(ColumnDescription = "请假开始时间")]
    public DateTime StartTime { get; set; }

    /// <summary>请假结束时间（UTC）</summary>
    [SugarColumn(ColumnDescription = "请假结束时间")]
    public DateTime EndTime { get; set; }

    /// <summary>请假类型（Sick/Personal/Official/Other）</summary>
    [SugarColumn(ColumnDescription = "请假类型")]
    public LeaveType LeaveType { get; set; }

    /// <summary>请假事由</summary>
    [SugarColumn(Length = 512, ColumnDescription = "请假事由")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>审批状态（Pending/Approved/Rejected）</summary>
    [SugarColumn(ColumnDescription = "审批状态")]
    public LeaveStatus Status { get; set; }

    /// <summary>审批备注（辅导员填写）</summary>
    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "审批备注")]
    public string? ReviewRemark { get; set; }

    /// <summary>审批时间（UTC），未审批时为空</summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "审批时间")]
    public DateTime? ReviewTime { get; set; }
}
}