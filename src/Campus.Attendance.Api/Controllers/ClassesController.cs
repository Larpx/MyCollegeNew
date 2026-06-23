using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Organization;
using Campus.Attendance.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 班级管理控制器，提供班级 CRUD
/// </summary>
[ApiController]
[Route("api/classes")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="organizationService">组织架构管理服务</param>
    public ClassesController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// 查询班级列表，支持按专业过滤
    /// </summary>
    /// <param name="majorId">可选专业 Id 过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>班级列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ClassResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<ClassResponseDto>>> GetList(
        [FromQuery] long? majorId,
        CancellationToken cancellationToken)
    {
        // 未指定专业时返回空列表，避免无约束的全量查询；指定专业则返回该专业下的班级
        var result = majorId.HasValue
            ? await _organizationService.GetClassesByMajorAsync(majorId.Value, cancellationToken)
            : new List<ClassResponseDto>();
        return ApiResponse<List<ClassResponseDto>>.Success(result);
    }

    /// <summary>
    /// 根据 Id 查询班级详情
    /// </summary>
    /// <param name="id">班级 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>班级详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ClassResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ClassResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _organizationService.GetClassByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<ClassResponseDto>.Fail($"班级 {id} 不存在", 404);
        }

        return ApiResponse<ClassResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建班级
    /// </summary>
    /// <param name="dto">班级创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的班级信息</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ClassResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<ClassResponseDto>> Create([FromBody] ClassCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _organizationService.CreateClassAsync(dto, cancellationToken);
        return ApiResponse<ClassResponseDto>.Success(result, "创建成功");
    }

    /// <summary>
    /// 更新班级
    /// </summary>
    /// <param name="id">班级 Id</param>
    /// <param name="dto">班级更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的班级信息</returns>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ClassResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ClassResponseDto>> Update(long id, [FromBody] ClassUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _organizationService.UpdateClassAsync(id, dto, cancellationToken);
        return ApiResponse<ClassResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除班级
    /// </summary>
    /// <param name="id">班级 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteClassAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, "删除成功");
    }
}
