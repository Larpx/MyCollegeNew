using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Attendance;
using Campus.Attendance.Services.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 考勤会话控制器，提供会话创建、二维码生成、学生签到、点名、关闭等端点
/// </summary>
[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="attendanceService">考勤服务</param>
    /// <param name="currentUser">当前用户上下文</param>
    public SessionsController(IAttendanceService attendanceService, ICurrentUser currentUser)
    {
        _attendanceService = attendanceService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 创建考勤会话（教师/辅导员）
    /// </summary>
    /// <param name="dto">会话创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的会话信息</returns>
    [HttpPost]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<SessionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<SessionResponseDto>> Create([FromBody] SessionCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.CreateSessionAsync(dto, _currentUser.UserId, cancellationToken);
        return ApiResponse<SessionResponseDto>.Success(result, Msg.Common.CreateSuccess);
    }

    /// <summary>
    /// 查询会话详情
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SessionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<SessionResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _attendanceService.GetSessionByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<SessionResponseDto>.Fail($"会话 {id} 不存在", 404);
        }

        return ApiResponse<SessionResponseDto>.Success(dto);
    }

    /// <summary>
    /// 教师进行中的会话
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>进行中的会话列表</returns>
    [HttpGet("active")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<List<SessionResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<SessionResponseDto>>> GetActive(CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetActiveSessionsByTeacherAsync(_currentUser.UserId, cancellationToken);
        return ApiResponse<List<SessionResponseDto>>.Success(result);
    }

    /// <summary>
    /// 分页查询教师历史会话
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="startDate">开始时间过滤</param>
    /// <param name="endDate">结束时间过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet("history")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SessionResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<SessionResponseDto>>> GetHistory(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _attendanceService.GetSessionsByTeacherAsync(pageIndex, pageSize, _currentUser.UserId, startDate, endDate, cancellationToken);
        return ApiResponse<PagedResult<SessionResponseDto>>.Success(result);
    }

    /// <summary>
    /// 查询会话签到记录
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>签到记录列表</returns>
    [HttpGet("{id:long}/records")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceRecordResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<AttendanceRecordResponseDto>>> GetRecords(long id, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetSessionRecordsAsync(id, cancellationToken);
        return ApiResponse<List<AttendanceRecordResponseDto>>.Success(result);
    }

    /// <summary>
    /// 生成二维码（返回 Base64 图片）
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>二维码生成结果</returns>
    [HttpPost("{id:long}/qrcode")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<QrCodeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<QrCodeResult>> GenerateQrCode(long id, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GenerateQrCodeAsync(id, _currentUser.UserId, cancellationToken);
        return ApiResponse<QrCodeResult>.Success(result, Msg.Attendance.QrCodeGenerated);
    }

    /// <summary>
    /// 学生签到
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="dto">签到请求 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>签到结果</returns>
    [HttpPost("{id:long}/checkin")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<CheckInResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<CheckInResult>> CheckIn(long id, [FromBody] CheckInRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.CheckInAsync(id, dto.Token, _currentUser.UserId, cancellationToken);
        return ApiResponse<CheckInResult>.Success(result, Msg.Attendance.CheckInSuccess);
    }

    /// <summary>
    /// 一键点名（教师）
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标记的学生数</returns>
    [HttpPost("{id:long}/roll-call-all")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<object>> RollCallAll(long id, CancellationToken cancellationToken)
    {
        var count = await _attendanceService.RollCallAllPresentAsync(id, _currentUser.UserId, cancellationToken);
        return ApiResponse<object>.Success(new { Count = count }, Msg.Attendance.RollCallComplete(count));
    }

    /// <summary>
    /// 修改单条记录状态（教师）
    /// </summary>
    /// <param name="recordId">记录 Id</param>
    /// <param name="dto">修改状态 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPut("records/{recordId:long}")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<object>> UpdateRecord(long recordId, [FromBody] UpdateRecordStatusDto dto, CancellationToken cancellationToken)
    {
        await _attendanceService.UpdateRecordStatusAsync(recordId, dto.Status, _currentUser.UserId, cancellationToken);
        return ApiResponse<object>.Success(new { }, Msg.Common.ChangeSuccess);
    }

    /// <summary>
    /// 手动补签（教师）
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="dto">手动补签 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>补签后的记录</returns>
    [HttpPost("{id:long}/manual-checkin")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<AttendanceRecordResponseDto>> ManualCheckIn(long id, [FromBody] ManualCheckInDto dto, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.ManualCheckInAsync(id, dto.StudentId, dto.Status, _currentUser.UserId, cancellationToken);
        return ApiResponse<AttendanceRecordResponseDto>.Success(result, Msg.Attendance.ManualCheckInSuccess);
    }

    /// <summary>
    /// 关闭会话（教师）
    /// </summary>
    /// <param name="id">会话 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id:long}/close")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<object>> Close(long id, CancellationToken cancellationToken)
    {
        await _attendanceService.CloseSessionAsync(id, _currentUser.UserId, cancellationToken);
        return ApiResponse<object>.Success(new { }, Msg.Attendance.SessionAlreadyClosed);
    }

    /// <summary>
    /// 随机点名（教师）
    /// </summary>
    /// <param name="classId">班级 Id</param>
    /// <param name="sessionId">会话 Id（可选，用于避免连续点名）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>随机点名学生信息</returns>
    [HttpPost("random-pick/{classId:long}")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<RandomPickResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<RandomPickResult>> RandomPick(long classId, [FromQuery] long? sessionId = null, CancellationToken cancellationToken = default)
    {
        var result = await _attendanceService.RandomPickAsync(classId, sessionId, cancellationToken);
        return ApiResponse<RandomPickResult>.Success(result);
    }

    /// <summary>
    /// 标记随机点名结果
    /// </summary>
    /// <param name="sessionId">会话 Id</param>
    /// <param name="dto">标记 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("random-pick/{sessionId:long}/mark")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> MarkRandomPick(long sessionId, [FromBody] MarkRandomPickDto dto, CancellationToken cancellationToken)
    {
        await _attendanceService.MarkRandomPickResultAsync(sessionId, dto.StudentId, dto.Answered, cancellationToken);
        return ApiResponse<object>.Success(new { }, Msg.Attendance.MarkSuccess);
    }
}
