using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Attendance;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Attendance
{
    /// <summary>创建考勤会话</summary>
    public record CreateSessionCommand(SessionCreateDto Dto, string TeacherId) : IRequest<ApiResponse<SessionResponseDto>>;

    /// <summary>查询会话详情</summary>
    public record GetSessionByIdQuery(long Id) : IRequest<ApiResponse<SessionResponseDto>>;

    /// <summary>查询教师进行中的会话</summary>
    public record GetActiveSessionsQuery(string TeacherId) : IRequest<ApiResponse<List<SessionResponseDto>>>;

    /// <summary>分页查询教师历史会话</summary>
    public record GetSessionsByTeacherQuery : PagedQuery, IRequest<ApiResponse<PagedResult<SessionResponseDto>>>
    {
        /// <summary>教师工号（由端点从当前用户填充，前端无需传递）</summary>
        public string? TeacherId { get; init; }

        /// <summary>开始日期</summary>
        public DateTime? StartDate { get; init; }

        /// <summary>结束日期</summary>
        public DateTime? EndDate { get; init; }
    }

    /// <summary>关闭会话</summary>
    public record CloseSessionCommand(long SessionId, string TeacherId) : IRequest<ApiResponse<object>>;

    /// <summary>查询会话签到记录</summary>
    public record GetSessionRecordsQuery(long SessionId) : IRequest<ApiResponse<List<AttendanceRecordResponseDto>>>;

    /// <summary>生成二维码</summary>
    public record GenerateQrCodeCommand(long SessionId, string TeacherId) : IRequest<ApiResponse<QrCodeResult>>;

    /// <summary>学生签到</summary>
    public record CheckInCommand(long SessionId, string Token, string StudentId) : IRequest<ApiResponse<CheckInResult>>;

    /// <summary>一键点名</summary>
    public record RollCallCommand(long SessionId, string TeacherId) : IRequest<ApiResponse<int>>;

    /// <summary>修改考勤记录状态</summary>
    public record UpdateRecordStatusCommand(long RecordId, AttendanceStatus Status, string TeacherId) : IRequest<ApiResponse<object>>;

    /// <summary>手动补签</summary>
    public record ManualCheckInCommand(long SessionId, string StudentId, AttendanceStatus Status, string TeacherId) : IRequest<ApiResponse<AttendanceRecordResponseDto>>;

    /// <summary>随机点名</summary>
    public record RandomPickQuery(long ClassId, long? SessionId, string? TeacherId) : IRequest<ApiResponse<RandomPickResult>>;

    /// <summary>标记随机点名结果</summary>
    public record MarkRandomPickCommand(long SessionId, string StudentId, bool Answered) : IRequest<ApiResponse<object>>;
}