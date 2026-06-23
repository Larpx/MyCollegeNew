namespace Campus.Attendance.Core.Constants;

/// <summary>
/// 考勤业务常量定义，集中管理签到状态判定的时间阈值等参数，避免魔法数字散落各处
/// </summary>
public static class AttendanceConstants
{
    /// <summary>正常签到的时间窗口（分钟）：会话开始后此时间内签到记为 Present</summary>
    public const int PresentThresholdMinutes = 5;

    /// <summary>迟到的时间窗口（分钟）：会话开始后 5-15 分钟内签到记为 Late</summary>
    public const int LateThresholdMinutes = 15;

    /// <summary>二维码短期令牌过期时间（秒），默认 30 秒</summary>
    public const int QrTokenExpireSeconds = 30;

    /// <summary>二维码签到令牌的 JWT Claim 名称：会话 Id</summary>
    public const string ClaimSessionId = "session_id";

    /// <summary>二维码签到令牌的 JWT Claim 名称：用途标识（值为 "checkin"）</summary>
    public const string ClaimPurpose = "purpose";

    /// <summary>二维码签到令牌的用途标识值</summary>
    public const string PurposeCheckIn = "checkin";

    /// <summary>随机点名最近回答记录的内存缓存上限，避免内存无限增长</summary>
    public const int RandomPickHistoryLimit = 50;
}
