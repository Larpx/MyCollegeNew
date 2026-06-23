using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Leave;

namespace Campus.Attendance.Services.Leave;

/// <summary>
/// 请假审批流服务接口，封装学生请假申请、辅导员审批、审批后考勤记录联动更新等业务
/// </summary>
public interface ILeaveService
{
    /// <summary>学生提交请假申请（Status=Pending，CounselorId 从学生班级关联获取）</summary>
    Task<LeaveResponseDto> CreateLeaveAsync(LeaveCreateDto dto, string studentId, CancellationToken cancellationToken = default);

    /// <summary>学生分页查询自己的请假记录</summary>
    Task<PagedResult<LeaveResponseDto>> GetLeavesByStudentAsync(string studentId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>辅导员分页查询请假记录，支持状态过滤</summary>
    Task<PagedResult<LeaveResponseDto>> GetLeavesByCounselorAsync(string counselorId, LeaveStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>辅导员待审批数量（用于首页提醒）</summary>
    Task<long> GetPendingLeavesCountAsync(string counselorId, CancellationToken cancellationToken = default);

    /// <summary>查询请假详情</summary>
    Task<LeaveResponseDto?> GetLeaveByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>审批通过（Status=Approved，记录 ReviewRemark 和 ReviewTime，并联动更新考勤记录为 Leave）</summary>
    Task<LeaveResponseDto> ApproveLeaveAsync(long id, string counselorId, LeaveReviewDto dto, CancellationToken cancellationToken = default);

    /// <summary>审批驳回（Status=Rejected）</summary>
    Task<LeaveResponseDto> RejectLeaveAsync(long id, string counselorId, LeaveReviewDto dto, CancellationToken cancellationToken = default);

    /// <summary>按班级查询请假记录（教师/辅导员查看班级请假情况）</summary>
    Task<List<LeaveResponseDto>> GetLeavesByClassAsync(long classId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
