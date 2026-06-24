using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Organization;
using Campus.Attendance.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 院系管理控制器，提供院系 CRUD、按院系查询专业与组织架构树
/// </summary>
[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="organizationService">组织架构管理服务</param>
    public DepartmentsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// 查询所有院系列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>院系列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<DepartmentResponseDto>>> GetList(CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetDepartmentsAsync(cancellationToken);
        return ApiResponse<List<DepartmentResponseDto>>.Success(result);
    }

    /// <summary>
    /// 查询组织架构树（院系→专业→班级）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>组织架构树</returns>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse<List<OrganizationTreeNodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<OrganizationTreeNodeDto>>> GetTree(CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetOrganizationTreeAsync(cancellationToken);
        return ApiResponse<List<OrganizationTreeNodeDto>>.Success(result);
    }

    /// <summary>
    /// 根据 Id 查询院系详情
    /// </summary>
    /// <param name="id">院系 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>院系详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<DepartmentResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _organizationService.GetDepartmentByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<DepartmentResponseDto>.Fail(Msg.Common.EntityNotFound($"院系 {id}"), 404);
        }

        return ApiResponse<DepartmentResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建院系
    /// </summary>
    /// <param name="dto">院系创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的院系信息</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<DepartmentResponseDto>> Create([FromBody] DepartmentCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _organizationService.CreateDepartmentAsync(dto, cancellationToken);
        return ApiResponse<DepartmentResponseDto>.Success(result, Msg.Common.CreateSuccess);
    }

    /// <summary>
    /// 更新院系
    /// </summary>
    /// <param name="id">院系 Id</param>
    /// <param name="dto">院系更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的院系信息</returns>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<DepartmentResponseDto>> Update(long id, [FromBody] DepartmentUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _organizationService.UpdateDepartmentAsync(id, dto, cancellationToken);
        return ApiResponse<DepartmentResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除院系
    /// </summary>
    /// <param name="id">院系 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteDepartmentAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, Msg.Common.DeleteSuccess);
    }

    /// <summary>
    /// 查询该院系下的专业列表
    /// </summary>
    /// <param name="id">院系 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>专业列表</returns>
    [HttpGet("{id:long}/majors")]
    [ProducesResponseType(typeof(ApiResponse<List<MajorResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<MajorResponseDto>>> GetMajors(long id, CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetMajorsByDepartmentAsync(id, cancellationToken);
        return ApiResponse<List<MajorResponseDto>>.Success(result);
    }
}
