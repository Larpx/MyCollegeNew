namespace Larpx.PersonalTools.MyCollegeNew.Shared.Enums
{
/// <summary>
/// 考勤状态枚举，标识学生单次考勤的出勤结果
/// </summary>
public enum AttendanceStatus
{
    /// <summary>正常出勤</summary>
    Present,
    /// <summary>迟到</summary>
    Late,
    /// <summary>缺勤</summary>
    Absent,
    /// <summary>请假（经审批通过）</summary>
    Leave
}
}