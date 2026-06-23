using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Courses;
using Campus.Attendance.Services.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 课程管理控制器，提供课程 CRUD 与按教师查询课程
/// </summary>
[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="courseService">课程管理服务</param>
    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    /// <summary>
    /// 分页查询课程列表
    /// </summary>
    /// <param name="pageIndex">页码（从 1 开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="keyword">关键字（课程名称）</param>
    /// <param name="teacherId">教师工号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CourseResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<PagedResult<CourseResponseDto>>> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _courseService.GetCoursesAsync(pageIndex, pageSize, keyword, teacherId, cancellationToken);
        return ApiResponse<PagedResult<CourseResponseDto>>.Success(result);
    }

    /// <summary>
    /// 根据 Id 查询课程详情
    /// </summary>
    /// <param name="id">课程 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>课程详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<CourseResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<CourseResponseDto>.Fail($"课程 {id} 不存在", 404);
        }

        return ApiResponse<CourseResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建课程
    /// </summary>
    /// <param name="dto">课程创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的课程信息</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<CourseResponseDto>> Create([FromBody] CourseCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateCourseAsync(dto, cancellationToken);
        return ApiResponse<CourseResponseDto>.Success(result, "创建成功");
    }

    /// <summary>
    /// 更新课程
    /// </summary>
    /// <param name="id">课程 Id</param>
    /// <param name="dto">课程更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的课程信息</returns>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<CourseResponseDto>> Update(long id, [FromBody] CourseUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateCourseAsync(id, dto, cancellationToken);
        return ApiResponse<CourseResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除课程
    /// </summary>
    /// <param name="id">课程 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _courseService.DeleteCourseAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, "删除成功");
    }

    /// <summary>
    /// 按教师查询课程列表
    /// </summary>
    /// <param name="teacherId">教师工号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>课程列表</returns>
    [HttpGet("by-teacher/{teacherId}")]
    [ProducesResponseType(typeof(ApiResponse<List<CourseResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<List<CourseResponseDto>>> GetByTeacher(string teacherId, CancellationToken cancellationToken)
    {
        var result = await _courseService.GetCoursesByTeacherAsync(teacherId, cancellationToken);
        return ApiResponse<List<CourseResponseDto>>.Success(result);
    }
}
