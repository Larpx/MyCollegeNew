using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Users;
using Campus.Attendance.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 学生管理控制器，提供学生 CRUD 与 CSV 批量导入
/// </summary>
[ApiController]
[Route("api/students")]
[Authorize(Roles = "Admin")]
public class StudentsController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userService">用户管理服务</param>
    public StudentsController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 分页查询学生列表
    /// </summary>
    /// <param name="pageIndex">页码（从 1 开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="keyword">关键字（学号或姓名）</param>
    /// <param name="classId">班级 Id</param>
    /// <param name="majorId">专业 Id</param>
    /// <param name="departmentId">院系 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<PagedResult<StudentResponseDto>>> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] long? classId = null,
        [FromQuery] long? majorId = null,
        [FromQuery] long? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetStudentsAsync(pageIndex, pageSize, keyword, classId, majorId, departmentId, cancellationToken);
        return ApiResponse<PagedResult<StudentResponseDto>>.Success(result);
    }

    /// <summary>
    /// 根据学号查询学生详情
    /// </summary>
    /// <param name="id">学号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>学生详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<StudentResponseDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var dto = await _userService.GetStudentByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<StudentResponseDto>.Fail($"学生 {id} 不存在", 404);
        }

        return ApiResponse<StudentResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建学生
    /// </summary>
    /// <param name="dto">学生创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的学生信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<StudentResponseDto>> Create([FromBody] StudentCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateStudentAsync(dto, cancellationToken);
        return ApiResponse<StudentResponseDto>.Success(result, "创建成功");
    }

    /// <summary>
    /// 更新学生信息
    /// </summary>
    /// <param name="id">学号</param>
    /// <param name="dto">学生更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的学生信息</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<StudentResponseDto>> Update(string id, [FromBody] StudentUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateStudentAsync(id, dto, cancellationToken);
        return ApiResponse<StudentResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除学生
    /// </summary>
    /// <param name="id">学号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(string id, CancellationToken cancellationToken)
    {
        await _userService.DeleteStudentAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, "删除成功");
    }

    /// <summary>
    /// CSV 批量导入学生
    /// </summary>
    /// <param name="file">CSV 文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导入结果</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<BatchImportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<BatchImportResultDto>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ApiResponse<BatchImportResultDto>.Fail("请上传有效的 CSV 文件", 400);
        }

        await using var stream = file.OpenReadStream();
        var result = await _userService.BatchImportStudentsAsync(stream, cancellationToken);
        return ApiResponse<BatchImportResultDto>.Success(result, "导入完成");
    }
}
