namespace Larpx.PersonalTools.MyCollegeNew.Shared.Enums
{
/// <summary>
/// AttendanceStatus 枚举扩展方法，提供考勤状态的中文显示名称映射
/// </summary>
public static class AttendanceStatusExtensions
{
    /// <summary>
    /// 获取考勤状态的中文显示名称，用于界面展示与报表导出
    /// </summary>
    public static string GetDisplayName(this AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "正常",
        AttendanceStatus.Late => "迟到",
        AttendanceStatus.Absent => "缺勤",
        AttendanceStatus.Leave => "请假",
        _ => status.ToString()
    };
}
}