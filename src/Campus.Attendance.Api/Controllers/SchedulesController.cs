using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Courses;
using Campus.Attendance.Services.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 课表管理控制器，提供课表 CRUD 与按教师/学生/班级的周课表查询
/// </summary>
[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scheduleService">课表管理服务</param>
    public SchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// 分页查询课表列表
    /// </summary>
    /// <param name="pageIndex">页码（从 1 开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="classId">班级 Id</param>
    /// <param name="teacherId">教师工号</param>
    /// <param name="courseId">课程 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ScheduleResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<PagedResult<ScheduleResponseDto>>> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? classId = null,
        [FromQuery] string? teacherId = null,
        [FromQuery] long? courseId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _scheduleService.GetSchedulesAsync(pageIndex, pageSize, classId, teacherId, courseId, cancellationToken);
        return ApiResponse<PagedResult<ScheduleResponseDto>>.Success(result);
    }

    /// <summary>
    /// 根据 Id 查询课表详情
    /// </summary>
    /// <param name="id">课表 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>课表详情</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ScheduleResponseDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var dto = await _scheduleService.GetScheduleByIdAsync(id, cancellationToken);
        if (dto is null)
        {
            return ApiResponse<ScheduleResponseDto>.Fail($"课表 {id} 不存在", 404);
        }

        return ApiResponse<ScheduleResponseDto>.Success(dto);
    }

    /// <summary>
    /// 创建排课
    /// </summary>
    /// <param name="dto">课表创建 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建后的课表信息</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<ScheduleResponseDto>> Create([FromBody] ScheduleCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.CreateScheduleAsync(dto, cancellationToken);
        return ApiResponse<ScheduleResponseDto>.Success(result, "创建成功");
    }

    /// <summary>
    /// 更新课表
    /// </summary>
    /// <param name="id">课表 Id</param>
    /// <param name="dto">课表更新 DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的课表信息</returns>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ScheduleResponseDto>> Update(long id, [FromBody] ScheduleUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.UpdateScheduleAsync(id, dto, cancellationToken);
        return ApiResponse<ScheduleResponseDto>.Success(result, "更新成功");
    }

    /// <summary>
    /// 软删除课表
    /// </summary>
    /// <param name="id">课表 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _scheduleService.DeleteScheduleAsync(id, cancellationToken);
        return ApiResponse<object>.Success(new { }, "删除成功");
    }

    /// <summary>
    /// 按教师查询某周课表（按星期分组）
    /// </summary>
    /// <param name="teacherId">教师工号</param>
    /// <param name="week">周次（1-20）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>周课表</returns>
    [HttpGet("by-teacher/{teacherId}")]
    [ProducesResponseType(typeof(ApiResponse<WeeklyScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<WeeklyScheduleDto>> GetByTeacher(string teacherId, [FromQuery] int week, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.GetScheduleByTeacherAsync(teacherId, week, cancellationToken);
        return ApiResponse<WeeklyScheduleDto>.Success(result);
    }

    /// <summary>
    /// 按学生查询某周课表（通过班级关联）
    /// </summary>
    /// <param name="studentId">学号</param>
    /// <param name="week">周次（1-20）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>周课表</returns>
    [HttpGet("by-student/{studentId}")]
    [ProducesResponseType(typeof(ApiResponse<WeeklyScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<WeeklyScheduleDto>> GetByStudent(string studentId, [FromQuery] int week, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.GetScheduleByStudentAsync(studentId, week, cancellationToken);
        return ApiResponse<WeeklyScheduleDto>.Success(result);
    }

    /// <summary>
    /// 按班级查询某周课表
    /// </summary>
    /// <param name="classId">班级 Id</param>
    /// <param name="week">周次（1-20）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>周课表</returns>
    [HttpGet("by-class/{classId:long}")]
    [ProducesResponseType(typeof(ApiResponse<WeeklyScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<WeeklyScheduleDto>> GetByClass(long classId, [FromQuery] int week, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.GetScheduleByClassAsync((int)classId, week, cancellationToken);
        return ApiResponse<WeeklyScheduleDto>.Success(result);
    }
}
