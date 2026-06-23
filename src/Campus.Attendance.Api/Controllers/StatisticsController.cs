using Campus.Attendance.Core.Responses;
using Campus.Attendance.Core.Security;
using Campus.Attendance.Models.Statistics;
using Campus.Attendance.Services.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus.Attendance.Api.Controllers;

/// <summary>
/// 考勤统计与报表控制器，提供全局统计、院系排名、出勤趋势、多维度统计与 Excel 导出端点
/// </summary>
[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="statisticsService">统计服务</param>
    /// <param name="currentUser">当前用户上下文</param>
    public StatisticsController(IStatisticsService statisticsService, ICurrentUser currentUser)
    {
        _statisticsService = statisticsService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 全局统计（管理员）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>全局统计概览</returns>
    [HttpGet("overview")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<OverviewStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<OverviewStatisticsDto>> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetOverviewAsync(cancellationToken);
        return ApiResponse<OverviewStatisticsDto>.Success(result);
    }

    /// <summary>
    /// 院系出勤率排名（管理员）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>院系排名列表</returns>
    [HttpGet("department-ranking")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentRankingDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<DepartmentRankingDto>>> GetDepartmentRanking(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetDepartmentRankingAsync(cancellationToken);
        return ApiResponse<List<DepartmentRankingDto>>.Success(result);
    }

    /// <summary>
    /// 出勤趋势（管理员）
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>出勤趋势列表</returns>
    [HttpGet("attendance-trend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceTrendDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<AttendanceTrendDto>>> GetAttendanceTrend(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetAttendanceTrendAsync(startDate, endDate, cancellationToken);
        return ApiResponse<List<AttendanceTrendDto>>.Success(result);
    }

    /// <summary>
    /// 班级考勤统计
    /// </summary>
    /// <param name="classId">班级 Id</param>
    /// <param name="startDate">开始时间</param>
    /// <param name="endDate">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>班级统计</returns>
    [HttpGet("class/{classId:long}")]
    [Authorize(Roles = "Admin,Teacher,Counselor")]
    [ProducesResponseType(typeof(ApiResponse<ClassStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<ClassStatisticsDto>> GetClassStatistics(
        long classId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetClassStatisticsAsync(classId, startDate, endDate, cancellationToken);
        return ApiResponse<ClassStatisticsDto>.Success(result);
    }

    /// <summary>
    /// 课程考勤统计
    /// </summary>
    /// <param name="courseId">课程 Id</param>
    /// <param name="startDate">开始时间</param>
    /// <param name="endDate">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>课程统计</returns>
    [HttpGet("course/{courseId:long}")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(ApiResponse<ClassStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<ClassStatisticsDto>> GetCourseStatistics(
        long courseId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetCourseStatisticsAsync(courseId, startDate, endDate, cancellationToken);
        return ApiResponse<ClassStatisticsDto>.Success(result);
    }

    /// <summary>
    /// 学生个人统计（学生只能查自己）
    /// </summary>
    /// <param name="studentId">学生学号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>学生统计</returns>
    [HttpGet("student/{studentId}")]
    [ProducesResponseType(typeof(ApiResponse<StudentStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<StudentStatisticsDto>> GetStudentStatistics(string studentId, CancellationToken cancellationToken)
    {
        // 学生只能查询自己的统计
        if (_currentUser.Role == Core.Enums.UserRole.Student && _currentUser.UserId != studentId)
        {
            return ApiResponse<StudentStatisticsDto>.Fail("仅可查询自己的考勤统计", 403);
        }

        var result = await _statisticsService.GetStudentStatisticsAsync(studentId, cancellationToken);
        return ApiResponse<StudentStatisticsDto>.Success(result);
    }

    /// <summary>
    /// 教师统计（教师只能查自己）
    /// </summary>
    /// <param name="teacherId">教师工号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>教师统计</returns>
    [HttpGet("teacher/{teacherId}")]
    [ProducesResponseType(typeof(ApiResponse<TeacherStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<TeacherStatisticsDto>> GetTeacherStatistics(string teacherId, CancellationToken cancellationToken)
    {
        // 教师只能查询自己的统计
        if ((_currentUser.Role == Core.Enums.UserRole.Teacher || _currentUser.Role == Core.Enums.UserRole.Counselor)
            && _currentUser.UserId != teacherId)
        {
            return ApiResponse<TeacherStatisticsDto>.Fail("仅可查询自己的考勤统计", 403);
        }

        var result = await _statisticsService.GetTeacherStatisticsAsync(teacherId, cancellationToken);
        return ApiResponse<TeacherStatisticsDto>.Success(result);
    }

    /// <summary>
    /// 导出会话考勤记录为 Excel
    /// </summary>
    /// <param name="sessionId">会话 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Excel 文件</returns>
    [HttpGet("export/session/{sessionId:long}")]
    [Authorize(Roles = "Admin,Teacher,Counselor")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportSessionRecords(long sessionId, CancellationToken cancellationToken)
    {
        var bytes = await _statisticsService.ExportAttendanceRecordsAsync(sessionId, cancellationToken);
        var fileName = $"考勤记录_{sessionId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 导出班级考勤汇总为 Excel
    /// </summary>
    /// <param name="classId">班级 Id</param>
    /// <param name="startDate">开始时间</param>
    /// <param name="endDate">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Excel 文件</returns>
    [HttpGet("export/class/{classId:long}")]
    [Authorize(Roles = "Admin,Teacher,Counselor")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportClassAttendance(
        long classId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var bytes = await _statisticsService.ExportClassAttendanceAsync(classId, startDate, endDate, cancellationToken);
        var fileName = $"班级考勤汇总_{classId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 导出班级学生名单为 Excel
    /// </summary>
    /// <param name="classId">班级 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Excel 文件</returns>
    [HttpGet("export/students/{classId:long}")]
    [Authorize(Roles = "Admin,Teacher,Counselor")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportStudentList(long classId, CancellationToken cancellationToken)
    {
        var bytes = await _statisticsService.ExportStudentListAsync(classId, cancellationToken);
        var fileName = $"学生名单_{classId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
