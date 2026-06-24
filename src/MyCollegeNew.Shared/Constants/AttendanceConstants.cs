namespace Larpx.PersonalTools.MyCollegeNew.Shared.Constants;

/// <summary>
/// 考勤业务常量定义
/// </summary>
public static class AttendanceConstants
{
    /// <summary>正常签到的时间窗口（分钟）</summary>
    public const int PresentThresholdMinutes = 5;

    /// <summary>迟到的时间窗口（分钟）</summary>
    public const int LateThresholdMinutes = 15;

    /// <summary>二维码短期令牌过期时间（秒）</summary>
    public const int QrTokenExpireSeconds = 30;

    /// <summary>二维码签到令牌的 JWT Claim 名称：会话 Id</summary>
    public const string ClaimSessionId = "session_id";

    /// <summary>二维码签到令牌的 JWT Claim 名称：用途标识</summary>
    public const string ClaimPurpose = "purpose";

    /// <summary>二维码签到令牌的用途标识值</summary>
    public const string PurposeCheckIn = "checkin";

    /// <summary>随机点名最近回答记录的内存缓存上限</summary>
    public const int RandomPickHistoryLimit = 50;
}
