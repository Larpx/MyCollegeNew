using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Attendance;

namespace Campus.Attendance.Services.Attendance;

/// <summary>
/// 考勤会话与签到服务接口，封装会话生命周期管理、二维码生成、学生签到、点名等业务
/// </summary>
public interface IAttendanceService
{
    /// <summary>创建考勤会话（状态 Active，生成初始 QrToken）</summary>
    Task<SessionResponseDto> CreateSessionAsync(SessionCreateDto dto, string teacherId, CancellationToken cancellationToken = default);

    /// <summary>查询会话详情（含课程名、班级名、教师名）</summary>
    Task<SessionResponseDto?> GetSessionByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>查询教师进行中的会话</summary>
    Task<List<SessionResponseDto>> GetActiveSessionsByTeacherAsync(string teacherId, CancellationToken cancellationToken = default);

    /// <summary>分页查询教师历史会话，支持时间区间过滤</summary>
    Task<PagedResult<SessionResponseDto>> GetSessionsByTeacherAsync(
        int pageIndex, int pageSize, string teacherId,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>关闭会话（Status=Closed），并为未签到学生创建缺勤记录</summary>
    Task CloseSessionAsync(long sessionId, string teacherId, CancellationToken cancellationToken = default);

    /// <summary>查询会话的所有签到记录</summary>
    Task<List<AttendanceRecordResponseDto>> GetSessionRecordsAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>生成短期二维码（30 秒过期），返回 Base64 图片与 token</summary>
    Task<QrCodeResult> GenerateQrCodeAsync(long sessionId, string teacherId, CancellationToken cancellationToken = default);

    /// <summary>学生扫码签到，根据签到时间判定 Present/Late/Absent</summary>
    Task<CheckInResult> CheckInAsync(long sessionId, string token, string studentId, CancellationToken cancellationToken = default);

    /// <summary>一键点名：将所有未签到学生标记为 Present，批量插入记录</summary>
    Task<int> RollCallAllPresentAsync(long sessionId, string teacherId, CancellationToken cancellationToken = default);

    /// <summary>修改单条考勤记录状态（校验记录所属会话属于该教师）</summary>
    Task UpdateRecordStatusAsync(long recordId, AttendanceStatus status, string teacherId, CancellationToken cancellationToken = default);

    /// <summary>教师手动补签</summary>
    Task<AttendanceRecordResponseDto> ManualCheckInAsync(long sessionId, string studentId, AttendanceStatus status, string teacherId, CancellationToken cancellationToken = default);

    /// <summary>随机点名：从班级学生中随机抽取一名，可避免连续回答</summary>
    Task<RandomPickResult> RandomPickAsync(long classId, long? sessionId = null, CancellationToken cancellationToken = default);

    /// <summary>标记随机点名结果（已回答/未回答）</summary>
    Task MarkRandomPickResultAsync(long sessionId, string studentId, bool answered, CancellationToken cancellationToken = default);
}
