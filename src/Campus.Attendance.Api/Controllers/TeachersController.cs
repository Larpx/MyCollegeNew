using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Users;
using Campus.Attendance.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 教师管理控制器，提供教师 CRUD
/// </summary>
[ApiController]
[Route("api/teachers")]
[Authorize(Roles = "Admin")]
public class TeachersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userService">用户管理服务</param>
    public TeachersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 分页查询教师列表
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="keyword">关键字（工号或姓名）</param>
    /// <param name="role">教师角色</param>
    /// <param name="departmentId">院系 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TeacherResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<PagedResult<TeacherResponseDto>>> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] TeacherRole? role = null,
        [FromQuery] long? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetTeachersAsync(pageIndex, pageSize, keyword, role, departmentId, cancellationToken);
        return ApiResponse<PagedResult<TeacherResponseDto>>.Success(result);
    }

    /// <summary>
    /// 根据工号查询教师详情
    /// </summary>
    /// <param name="id">工号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>教师详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TeacherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<TeacherResponseDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var dto = await _userService.GetTeacherByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<TeacherResponseDto>.Fail($"教师 {id} 不存在", 404);
        }

        return ApiResponse<TeacherResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建教师
    /// </summary>
    /// <param name="dto">教师创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的教师信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TeacherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<TeacherResponseDto>> Create([FromBody] TeacherCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateTeacherAsync(dto, cancellationToken);
        return ApiResponse<TeacherResponseDto>.Success(result, "创建成功");
    }

    /// <summary>
    /// 更新教师信息
    /// </summary>
    /// <param name="id">工号</param>
    /// <param name="dto">教师更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的教师信息</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TeacherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<TeacherResponseDto>> Update(string id, [FromBody] TeacherUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateTeacherAsync(id, dto, cancellationToken);
        return ApiResponse<TeacherResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除教师
    /// </summary>
    /// <param name="id">工号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(string id, CancellationToken cancellationToken)
    {
        await _userService.DeleteTeacherAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, "删除成功");
    }
}
