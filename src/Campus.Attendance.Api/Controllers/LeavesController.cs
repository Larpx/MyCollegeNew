using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Leave;
using Campus.Attendance.Services.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 请假管理控制器，提供学生请假申请、辅导员审批、请假记录查询等端点
/// </summary>
[ApiController]
[Route("api/leaves")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="leaveService">请假服务</param>
    /// <param name="currentUser">当前用户上下文</param>
    public LeavesController(ILeaveService leaveService, ICurrentUser currentUser)
    {
        _leaveService = leaveService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 学生提交请假申请
    /// </summary>
    /// <param name="dto">请假创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的请假信息</returns>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<LeaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<LeaveResponseDto>> Create([FromBody] LeaveCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _leaveService.CreateLeaveAsync(dto, _currentUser.UserId, cancellationToken);
        return ApiResponse<LeaveResponseDto>.Success(result, "请假申请已提交");
    }

    /// <summary>
    /// 学生查询自己的请假记录
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LeaveResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<LeaveResponseDto>>> GetMyLeaves(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaveService.GetLeavesByStudentAsync(_currentUser.UserId, pageIndex, pageSize, cancellationToken);
        return ApiResponse<PagedResult<LeaveResponseDto>>.Success(result);
    }

    /// <summary>
    /// 辅导员查询请假记录（支持状态过滤）
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="status">审批状态过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet("counselor")]
    [Authorize(Roles = "Counselor")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LeaveResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<LeaveResponseDto>>> GetCounselorLeaves(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] LeaveStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaveService.GetLeavesByCounselorAsync(_currentUser.UserId, status, pageIndex, pageSize, cancellationToken);
        return ApiResponse<PagedResult<LeaveResponseDto>>.Success(result);
    }

    /// <summary>
    /// 辅导员待审批数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>待审批数量</returns>
    [HttpGet("pending-count")]
    [Authorize(Roles = "Counselor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> GetPendingCount(CancellationToken cancellationToken)
    {
        var count = await _leaveService.GetPendingLeavesCountAsync(_currentUser.UserId, cancellationToken);
        return ApiResponse<object>.Success(new { Count = count });
    }

    /// <summary>
    /// 查询请假详情
    /// </summary>
    /// <param name="id">请假 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>请假详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<LeaveResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _leaveService.GetLeaveByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<LeaveResponseDto>.Fail($"请假申请 {id} 不存在", 404);
        }

        return ApiResponse<LeaveResponseDto>.Success(dto);
    }

    /// <summary>
    /// 审批通过
    /// </summary>
    /// <param name="id">请假 Id</param>
    /// <param name="dto">审批备注 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审批后的请假信息</returns>
    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = "Counselor")]
    [ProducesResponseType(typeof(ApiResponse<LeaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<LeaveResponseDto>> Approve(long id, [FromBody] LeaveReviewDto dto, CancellationToken cancellationToken)
    {
        var result = await _leaveService.ApproveLeaveAsync(id, _currentUser.UserId, dto, cancellationToken);
        return ApiResponse<LeaveResponseDto>.Success(result, "审批通过");
    }

    /// <summary>
    /// 审批驳回
    /// </summary>
    /// <param name="id">请假 Id</param>
    /// <param name="dto">审批备注 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审批后的请假信息</returns>
    [HttpPost("{id:long}/reject")]
    [Authorize(Roles = "Counselor")]
    [ProducesResponseType(typeof(ApiResponse<LeaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<LeaveResponseDto>> Reject(long id, [FromBody] LeaveReviewDto dto, CancellationToken cancellationToken)
    {
        var result = await _leaveService.RejectLeaveAsync(id, _currentUser.UserId, dto, cancellationToken);
        return ApiResponse<LeaveResponseDto>.Success(result, "已驳回");
    }

    /// <summary>
    /// 按班级查询请假记录（教师/辅导员）
    /// </summary>
    /// <param name="classId">班级 Id</param>
    /// <param name="startDate">开始时间</param>
    /// <param name="endDate">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>请假记录列表</returns>
    [HttpGet("by-class/{classId:long}")]
    [Authorize(Roles = "Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<LeaveResponseDto>>> GetByClass(
        long classId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetLeavesByClassAsync(classId, startDate, endDate, cancellationToken);
        return ApiResponse<List<LeaveResponseDto>>.Success(result);
    }
}
