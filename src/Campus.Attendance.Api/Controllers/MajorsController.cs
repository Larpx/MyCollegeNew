using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Organization;
using Campus.Attendance.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 专业管理控制器，提供专业 CRUD 与按专业查询班级
/// </summary>
[ApiController]
[Route("api/majors")]
[Authorize]
public class MajorsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="organizationService">组织架构管理服务</param>
    public MajorsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// 查询所有专业列表
    /// </summary>
    /// <param name="departmentId">可选院系 Id 过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>专业列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MajorResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<MajorResponseDto>>> GetList(
        [FromQuery] long? departmentId,
        CancellationToken cancellationToken)
    {
        // 未指定院系时返回空列表，避免无约束的全量查询；指定院系则返回该院系下的专业
        var result = departmentId.HasValue
            ? await _organizationService.GetMajorsByDepartmentAsync(departmentId.Value, cancellationToken)
            : new List<MajorResponseDto>();
        return ApiResponse<List<MajorResponseDto>>.Success(result);
    }

    /// <summary>
    /// 根据 Id 查询专业详情
    /// </summary>
    /// <param name="id">专业 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>专业详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<MajorResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<MajorResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _organizationService.GetMajorByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<MajorResponseDto>.Fail($"专业 {id} 不存在", 404);
        }

        return ApiResponse<MajorResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建专业
    /// </summary>
    /// <param name="dto">专业创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的专业信息</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MajorResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<MajorResponseDto>> Create([FromBody] MajorCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _organizationService.CreateMajorAsync(dto, cancellationToken);
        return ApiResponse<MajorResponseDto>.Success(result, "创建成功");
    }

    /// <summary>
    /// 更新专业
    /// </summary>
    /// <param name="id">专业 Id</param>
    /// <param name="dto">专业更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的专业信息</returns>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MajorResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<MajorResponseDto>> Update(long id, [FromBody] MajorUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _organizationService.UpdateMajorAsync(id, dto, cancellationToken);
        return ApiResponse<MajorResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除专业
    /// </summary>
    /// <param name="id">专业 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteMajorAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, "删除成功");
    }

    /// <summary>
    /// 查询该专业下的班级列表
    /// </summary>
    /// <param name="id">专业 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>班级列表</returns>
    [HttpGet("{id:long}/classes")]
    [ProducesResponseType(typeof(ApiResponse<List<ClassResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<ClassResponseDto>>> GetClasses(long id, CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetClassesByMajorAsync(id, cancellationToken);
        return ApiResponse<List<ClassResponseDto>>.Success(result);
    }
}
