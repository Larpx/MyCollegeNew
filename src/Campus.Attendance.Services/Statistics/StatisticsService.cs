using Campus.Attendance.Core.Configuration;
using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Models.Statistics;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using SqlSugar;

using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Services.Statistics;

/// <summary>
/// 考勤统计与报表服务实现，提供多维度考勤统计与 Excel 报表导出
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<StatisticsService> _logger;

    /// <summary>
    /// 构造函数，注入数据库上下文与日志器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志器</param>
    public StatisticsService(IDbContext dbContext, ILogger<StatisticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 管理员全局统计：全校出勤率、总学生数、总教师数、今日会话数、今日出勤率
    /// </summary>
    public async Task<OverviewStatisticsDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var totalStudents = await db.Queryable<Student>().Where(s => !s.IsDeleted).CountAsync();
        var totalTeachers = await db.Queryable<Teacher>().Where(t => !t.IsDeleted).CountAsync();
        var todaySessions = await db.Queryable<AttendanceSession>()
            .Where(s => !s.IsDeleted && s.StartTime >= today && s.StartTime < tomorrow)
            .CountAsync();

        // 全校历史出勤率
        var overallRate = await CalculateAttendanceRateAsync(db, null, null, null);

        // 今日出勤率
        var todayRate = await CalculateAttendanceRateAsync(db, null, today, tomorrow);

        return new OverviewStatisticsDto
        {
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            TodaySessions = todaySessions,
            OverallAttendanceRate = overallRate,
            TodayAttendanceRate = todayRate
        };
    }

    /// <summary>
    /// 院系出勤率排名
    /// </summary>
    public async Task<List<DepartmentRankingDto>> GetDepartmentRankingAsync(CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;

        // 查询所有院系
        var departments = await db.Queryable<Department>()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Id)
            .ToListAsync();

        if (departments.Count == 0)
        {
            return new List<DepartmentRankingDto>();
        }

        var departmentIds = departments.Select(d => d.Id).ToList();

        // 各院系学生数
        var studentCounts = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
            .GroupBy(s => s.DepartmentId)
            .Select(s => new { s.DepartmentId, Count = SqlFunc.AggregateCount(s.Id) })
            .ToListAsync();
        var studentCountMap = studentCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

        // 各院系考勤记录统计（通过学生关联院系）
        var recordStats = await db.Queryable<AttendanceRecord, Student>((r, s) =>
                new JoinQueryInfos(JoinType.Inner, r.StudentId == s.Id))
            .Where((r, s) => !r.IsDeleted && !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
            .GroupBy((r, s) => s.DepartmentId)
            .Select((r, s) => new
            {
                DepartmentId = s.DepartmentId,
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
            })
            .ToListAsync();

        var result = departments.Select(d =>
        {
            var stat = recordStats.FirstOrDefault(x => x.DepartmentId == d.Id);
            var total = stat?.Total ?? 0;
            var present = stat?.PresentCount ?? 0;
            var late = stat?.LateCount ?? 0;
            return new DepartmentRankingDto
            {
                DepartmentId = d.Id,
                DepartmentName = d.Name,
                AttendanceRate = CalculateRate(present + late, total),
                StudentCount = studentCountMap.GetValueOrDefault(d.Id, 0)
            };
        }).ToList();

        // 按出勤率降序排名
        var ranked = result.OrderByDescending(x => x.AttendanceRate).ThenBy(x => x.DepartmentId).ToList();
        for (var i = 0; i < ranked.Count; i++)
        {
            ranked[i].Rank = i + 1;
        }

        return ranked;
    }

    /// <summary>
    /// 异常考勤趋势（按日期分组的出勤率）
    /// </summary>
    public async Task<List<AttendanceTrendDto>> GetAttendanceTrendAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        if (endDate < startDate)
        {
            throw new BusinessException(Msg.Statistics.EndDateBeforeStart, 400);
        }

        // 查询区间内所有考勤记录，按会话开始日期分组
        var stats = await db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
            .Where((r, s) => !r.IsDeleted && !s.IsDeleted
                && s.StartTime >= startDate && s.StartTime <= endDate)
            .GroupBy((r, s) => s.StartTime.Date)
            .Select((r, s) => new
            {
                Date = s.StartTime.Date,
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
            })
            .ToListAsync();

        return stats.Select(x => new AttendanceTrendDto
        {
            Date = x.Date,
            AttendanceRate = CalculateRate(x.PresentCount + x.LateCount, x.Total),
            LateCount = x.LateCount,
            AbsentCount = x.AbsentCount,
            LeaveCount = x.LeaveCount
        }).OrderBy(x => x.Date).ToList();
    }

    /// <summary>
    /// 班级考勤统计
    /// </summary>
    public async Task<ClassStatisticsDto> GetClassStatisticsAsync(long classId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == classId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"班级 {classId}"), 404);
        }

        // 查询该班级的会话数与考勤记录统计
        var sessionQuery = db.Queryable<AttendanceSession>()
            .Where(s => !s.IsDeleted && s.ClassId == classId);
        if (startDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.StartTime >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.StartTime <= endDate.Value);
        }
        var totalSessions = await sessionQuery.CountAsync();

        var stats = await db.Queryable<AttendanceRecord>()
            .Where(r => !r.IsDeleted && SqlFunc.Subqueryable<AttendanceSession>()
                .Where(s => s.Id == r.SessionId && !s.IsDeleted && s.ClassId == classId
                    && (!startDate.HasValue || s.StartTime >= startDate.Value)
                    && (!endDate.HasValue || s.StartTime <= endDate.Value))
                .Any())
            .Select(r => new
            {
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
            })
            .FirstAsync();

        var total = stats?.Total ?? 0;
        var present = stats?.PresentCount ?? 0;
        var late = stats?.LateCount ?? 0;
        var absent = stats?.AbsentCount ?? 0;
        var leave = stats?.LeaveCount ?? 0;

        return new ClassStatisticsDto
        {
            ClassId = classId,
            ClassName = cls.Name,
            TotalSessions = totalSessions,
            AttendanceRate = CalculateRate(present + late, total),
            LateCount = late,
            AbsentCount = absent,
            LeaveCount = leave
        };
    }

    /// <summary>
    /// 课程考勤统计
    /// </summary>
    public async Task<ClassStatisticsDto> GetCourseStatisticsAsync(long courseId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var course = await db.Queryable<Course>().FirstAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);
        if (course is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"课程 {courseId}"), 404);
        }

        var sessionQuery = db.Queryable<AttendanceSession>()
            .Where(s => !s.IsDeleted && s.CourseId == courseId);
        if (startDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.StartTime >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.StartTime <= endDate.Value);
        }
        var totalSessions = await sessionQuery.CountAsync();

        var stats = await db.Queryable<AttendanceRecord>()
            .Where(r => !r.IsDeleted && SqlFunc.Subqueryable<AttendanceSession>()
                .Where(s => s.Id == r.SessionId && !s.IsDeleted && s.CourseId == courseId
                    && (!startDate.HasValue || s.StartTime >= startDate.Value)
                    && (!endDate.HasValue || s.StartTime <= endDate.Value))
                .Any())
            .Select(r => new
            {
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
            })
            .FirstAsync();

        var total = stats?.Total ?? 0;
        var present = stats?.PresentCount ?? 0;
        var late = stats?.LateCount ?? 0;
        var absent = stats?.AbsentCount ?? 0;
        var leave = stats?.LeaveCount ?? 0;

        // 复用 ClassStatisticsDto 结构，ClassName 字段存放课程名称
        return new ClassStatisticsDto
        {
            ClassId = courseId,
            ClassName = course.Name,
            TotalSessions = totalSessions,
            AttendanceRate = CalculateRate(present + late, total),
            LateCount = late,
            AbsentCount = absent,
            LeaveCount = leave
        };
    }

    /// <summary>
    /// 学生个人统计：本学期出勤率、迟到/缺勤/请假次数、课程维度统计列表
    /// </summary>
    public async Task<StudentStatisticsDto> GetStudentStatisticsAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var student = await db.Queryable<Student>().FirstAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);
        if (student is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"学生 {studentId}"), 404);
        }

        // 总体统计
        var stats = await db.Queryable<AttendanceRecord>()
            .Where(r => !r.IsDeleted && r.StudentId == studentId)
            .Select(r => new
            {
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
            })
            .FirstAsync();

        var total = stats?.Total ?? 0;
        var present = stats?.PresentCount ?? 0;
        var late = stats?.LateCount ?? 0;
        var absent = stats?.AbsentCount ?? 0;
        var leave = stats?.LeaveCount ?? 0;

        // 课程维度统计
        var courseStats = await db.Queryable<AttendanceRecord, AttendanceSession, Course>((r, s, c) =>
                new JoinQueryInfos(
                    JoinType.Inner, r.SessionId == s.Id,
                    JoinType.Inner, s.CourseId == c.Id))
            .Where((r, s, c) => !r.IsDeleted && !s.IsDeleted && !c.IsDeleted && r.StudentId == studentId)
            .GroupBy((r, s, c) => new { c.Id, c.Name })
            .Select((r, s, c) => new
            {
                CourseId = c.Id,
                CourseName = c.Name,
                TotalSessions = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
            })
            .ToListAsync();

        var courseStatistics = courseStats.Select(cs => new CourseStatisticsItemDto
        {
            CourseId = cs.CourseId,
            CourseName = cs.CourseName,
            TotalSessions = cs.TotalSessions,
            AttendanceRate = CalculateRate(cs.PresentCount + cs.LateCount, cs.TotalSessions)
        }).ToList();

        return new StudentStatisticsDto
        {
            StudentId = studentId,
            StudentName = student.Name,
            TotalSessions = total,
            PresentCount = present,
            LateCount = late,
            AbsentCount = absent,
            LeaveCount = leave,
            AttendanceRate = CalculateRate(present + late, total),
            CourseStatistics = courseStatistics
        };
    }

    /// <summary>
    /// 教师统计：总课程数、总会话数、平均出勤率
    /// </summary>
    public async Task<TeacherStatisticsDto> GetTeacherStatisticsAsync(string teacherId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == teacherId && !t.IsDeleted, cancellationToken);
        if (teacher is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"教师 {teacherId}"), 404);
        }

        var totalCourses = await db.Queryable<Course>()
            .Where(c => !c.IsDeleted && c.TeacherId == teacherId)
            .CountAsync();

        var totalSessions = await db.Queryable<AttendanceSession>()
            .Where(s => !s.IsDeleted && s.TeacherId == teacherId)
            .CountAsync();

        // 教师所有会话的平均出勤率
        var stats = await db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
            .Where((r, s) => !r.IsDeleted && !s.IsDeleted && s.TeacherId == teacherId)
            .Select((r, s) => new
            {
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
            })
            .FirstAsync();

        var total = stats?.Total ?? 0;
        var present = stats?.PresentCount ?? 0;
        var late = stats?.LateCount ?? 0;

        return new TeacherStatisticsDto
        {
            TeacherId = teacherId,
            TeacherName = teacher.Name,
            TotalCourses = totalCourses,
            TotalSessions = totalSessions,
            AverageAttendanceRate = CalculateRate(present + late, total)
        };
    }

    /// <summary>
    /// 导出单个会话的考勤记录为 Excel
    /// </summary>
    public async Task<byte[]> ExportAttendanceRecordsAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var session = await db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                new JoinQueryInfos(
                    JoinType.Left, s.CourseId == c.Id,
                    JoinType.Left, s.ClassId == cls.Id,
                    JoinType.Left, s.TeacherId == t.Id))
            .Where((s, c, cls, t) => s.Id == sessionId && !s.IsDeleted)
            .Select((s, c, cls, t) => new
            {
                SessionId = s.Id,
                CourseName = c.Name,
                ClassName = cls.Name,
                TeacherName = t.Name,
                s.StartTime,
                s.EndTime
            })
            .FirstAsync();

        if (session is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"考勤会话 {sessionId}"), 404);
        }

        var records = await db.Queryable<AttendanceRecord>()
            .Where(r => !r.IsDeleted && r.SessionId == sessionId)
            .OrderBy(r => r.StudentId)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("考勤记录");

        // 标题行
        worksheet.Cell(1, 1).Value = $"考勤记录 - {session.CourseName} / {session.ClassName}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 6).Merge();

        worksheet.Cell(2, 1).Value = $"教师：{session.TeacherName}    时间：{session.StartTime:yyyy-MM-dd HH:mm} ~ {session.EndTime:HH:mm}";
        worksheet.Range(2, 1, 2, 6).Merge();

        // 表头
        var headers = new[] { "序号", "学号", "姓名", "考勤状态", "签到时间", "备注" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // 数据行
        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var row = 5 + i;
            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Cell(row, 2).Value = r.StudentId;
            worksheet.Cell(row, 3).Value = r.StudentName;
            worksheet.Cell(row, 4).Value = r.Status.GetDisplayName();
            worksheet.Cell(row, 5).Value = r.CheckInTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            worksheet.Cell(row, 6).Value = r.Remark ?? string.Empty;

            for (var col = 1; col <= 6; col++)
            {
                worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _logger.LogInformation("导出会话 {SessionId} 考勤记录，共 {Count} 条", sessionId, records.Count);
        return stream.ToArray();
    }

    /// <summary>
    /// 导出班级考勤汇总为 Excel
    /// </summary>
    public async Task<byte[]> ExportClassAttendanceAsync(long classId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == classId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"班级 {classId}"), 404);
        }

        // 查询区间内该班级所有学生的考勤汇总
        var stats = await db.Queryable<AttendanceRecord, AttendanceSession, Student>((r, s, st) =>
                new JoinQueryInfos(
                    JoinType.Inner, r.SessionId == s.Id,
                    JoinType.Inner, r.StudentId == st.Id))
            .Where((r, s, st) => !r.IsDeleted && !s.IsDeleted && !st.IsDeleted
                && s.ClassId == classId && st.ClassId == classId
                && s.StartTime >= startDate && s.StartTime <= endDate)
            .GroupBy((r, s, st) => new { st.Id, st.Name })
            .Select((r, s, st) => new
            {
                StudentId = st.Id,
                StudentName = st.Name,
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("班级考勤汇总");

        worksheet.Cell(1, 1).Value = $"班级考勤汇总 - {cls.Name}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 8).Merge();

        worksheet.Cell(2, 1).Value = $"统计区间：{startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";
        worksheet.Range(2, 1, 2, 8).Merge();

        var headers = new[] { "序号", "学号", "姓名", "总次数", "出勤", "迟到", "缺勤", "请假" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        for (var i = 0; i < stats.Count; i++)
        {
            var s = stats[i];
            var row = 5 + i;
            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Cell(row, 2).Value = s.StudentId;
            worksheet.Cell(row, 3).Value = s.StudentName;
            worksheet.Cell(row, 4).Value = s.Total;
            worksheet.Cell(row, 5).Value = s.PresentCount;
            worksheet.Cell(row, 6).Value = s.LateCount;
            worksheet.Cell(row, 7).Value = s.AbsentCount;
            worksheet.Cell(row, 8).Value = s.LeaveCount;

            for (var col = 1; col <= 8; col++)
            {
                worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _logger.LogInformation("导出班级 {ClassId} 考勤汇总，共 {Count} 名学生", classId, stats.Count);
        return stream.ToArray();
    }

    /// <summary>
    /// 导出班级学生名单为 Excel
    /// </summary>
    public async Task<byte[]> ExportStudentListAsync(long classId, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.Client;
        var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == classId && !c.IsDeleted, cancellationToken);
        if (cls is null)
        {
            throw new BusinessException(Msg.Common.EntityNotFound($"班级 {classId}"), 404);
        }

        var students = await db.Queryable<Student>()
            .Where(s => !s.IsDeleted && s.ClassId == classId)
            .OrderBy(s => s.Id)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("学生名单");

        worksheet.Cell(1, 1).Value = $"学生名单 - {cls.Name}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "序号", "学号", "姓名", "性别", "年级" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        for (var i = 0; i < students.Count; i++)
        {
            var s = students[i];
            var row = 4 + i;
            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Cell(row, 2).Value = s.Id;
            worksheet.Cell(row, 3).Value = s.Name;
            worksheet.Cell(row, 4).Value = s.Gender;
            worksheet.Cell(row, 5).Value = s.Grade;

            for (var col = 1; col <= 5; col++)
            {
                worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _logger.LogInformation("导出班级 {ClassId} 学生名单，共 {Count} 名学生", classId, students.Count);
        return stream.ToArray();
    }

    /// <summary>
    /// 计算出勤率（百分比，0-100），分母为 0 时返回 0
    /// </summary>
    /// <param name="numerator">出勤人次（Present + Late）</param>
    /// <param name="denominator">总人次</param>
    /// <returns>出勤率百分比</returns>
    private static double CalculateRate(long numerator, long denominator)
        => denominator == 0 ? 0 : Math.Round((double)numerator / denominator * 100, 2);

    /// <summary>
    /// 计算指定条件下的出勤率，供全局/今日统计复用
    /// </summary>
    /// <param name="db">数据库客户端</param>
    /// <param name="classId">班级 Id（可空，表示全部）</param>
    /// <param name="startDate">开始时间（可空）</param>
    /// <param name="endDate">结束时间（可空）</param>
    /// <returns>出勤率百分比</returns>
    private async Task<double> CalculateAttendanceRateAsync(ISqlSugarClient db, long? classId, DateTime? startDate, DateTime? endDate)
    {
        var query = db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
            .Where((r, s) => !r.IsDeleted && !s.IsDeleted);

        if (classId.HasValue)
        {
            query = query.Where((r, s) => s.ClassId == classId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where((r, s) => s.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where((r, s) => s.StartTime < endDate.Value);
        }

        var stat = await query
            .Select((r, s) => new
            {
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
            })
            .FirstAsync();

        var total = stat?.Total ?? 0;
        var present = stat?.PresentCount ?? 0;
        var late = stat?.LateCount ?? 0;
        return CalculateRate(present + late, total);
    }

}
