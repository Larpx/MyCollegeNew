using ClosedXML.Excel;
using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Constants;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Statistics;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;
using SqlSugar;
using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Statistics
{
    /// <summary>
    /// 考勤统计与报表处理器
    /// </summary>
    public class StatisticsHandlers :
        IRequestHandler<GetOverviewQuery, ApiResponse<OverviewStatisticsDto>>,
        IRequestHandler<GetDepartmentRankingQuery, ApiResponse<List<DepartmentRankingDto>>>,
        IRequestHandler<GetAttendanceTrendQuery, ApiResponse<List<AttendanceTrendDto>>>,
        IRequestHandler<GetClassStatisticsQuery, ApiResponse<ClassStatisticsDto>>,
        IRequestHandler<GetCourseStatisticsQuery, ApiResponse<ClassStatisticsDto>>,
        IRequestHandler<GetStudentStatisticsQuery, ApiResponse<StudentStatisticsDto>>,
        IRequestHandler<GetTeacherStatisticsQuery, ApiResponse<TeacherStatisticsDto>>,
        IRequestHandler<ExportSessionRecordsQuery, IResult>,
        IRequestHandler<ExportClassAttendanceQuery, IResult>,
        IRequestHandler<ExportStudentListQuery, IResult>,
        IRequestHandler<GetDepartmentTeacherAttendanceSummaryQuery, ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>>,
        IRequestHandler<GetDepartmentSwapSummaryQuery, ApiResponse<DepartmentSwapSummaryDto>>,
        IRequestHandler<GetDepartmentCourseCoverageQuery, ApiResponse<DepartmentCourseCoverageDto>>
    {
        private readonly IDbContext _dbContext;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<StatisticsHandlers> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前用户上下文</param>
        /// <param name="logger">日志器</param>
        public StatisticsHandlers(IDbContext dbContext, ICurrentUser currentUser, ILogger<StatisticsHandlers> logger)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>全局统计</summary>
        public async Task<ApiResponse<OverviewStatisticsDto>> Handle(GetOverviewQuery _, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var totalStudents = await db.Queryable<Student>().Where(s => !s.IsDeleted).CountAsync();
            var totalTeachers = await db.Queryable<Teacher>().Where(t => !t.IsDeleted).CountAsync();
            var todaySessions = await db.Queryable<AttendanceSession>()
                .Where(s => !s.IsDeleted && s.StartTime >= today && s.StartTime < tomorrow).CountAsync();

            var overallRate = await CalculateAttendanceRateAsync(db, null, null, null);
            var todayRate = await CalculateAttendanceRateAsync(db, null, today, tomorrow);

            return ApiResponse<OverviewStatisticsDto>.Success(new OverviewStatisticsDto
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TodaySessions = todaySessions,
                OverallAttendanceRate = overallRate,
                TodayAttendanceRate = todayRate
            });
        }

        /// <summary>院系出勤率排名</summary>
        public async Task<ApiResponse<List<DepartmentRankingDto>>> Handle(GetDepartmentRankingQuery _, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var departments = await db.Queryable<Department>().Where(d => !d.IsDeleted).OrderBy(d => d.Id).ToListAsync();
            if (departments.Count == 0)
            {
                return ApiResponse<List<DepartmentRankingDto>>.Success(new List<DepartmentRankingDto>());
            }

            var departmentIds = departments.Select(d => d.Id).ToList();
            var studentCounts = await db.Queryable<Student>()
                .Where(s => !s.IsDeleted && departmentIds.Contains(s.DepartmentId))
                .GroupBy(s => s.DepartmentId)
                .Select(s => new { s.DepartmentId, Count = SqlFunc.AggregateCount(s.Id) })
                .ToListAsync();
            var studentCountMap = studentCounts.ToDictionary(x => x.DepartmentId, x => x.Count);

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
                }).ToListAsync();

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

            var ranked = result.OrderByDescending(x => x.AttendanceRate).ThenBy(x => x.DepartmentId).ToList();
            for (var i = 0; i < ranked.Count; i++)
            {
                ranked[i].Rank = i + 1;
            }

            return ApiResponse<List<DepartmentRankingDto>>.Success(ranked);
        }

        /// <summary>出勤趋势</summary>
        public async Task<ApiResponse<List<AttendanceTrendDto>>> Handle(GetAttendanceTrendQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            if (query.EndDate < query.StartDate)
            {
                return ApiResponse<List<AttendanceTrendDto>>.Fail(Msg.Statistics.EndDateBeforeStart, 400);
            }

            var stats = await db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                    new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
                .Where((r, s) => !r.IsDeleted && !s.IsDeleted && s.StartTime >= query.StartDate && s.StartTime <= query.EndDate)
                .GroupBy((r, s) => s.StartTime.Date)
                .Select((r, s) => new
                {
                    Date = s.StartTime.Date,
                    Total = SqlFunc.AggregateCount(r.Id),
                    PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                    LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                    AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                    LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
                }).ToListAsync();

            var result = stats.Select(x => new AttendanceTrendDto
            {
                Date = x.Date,
                AttendanceRate = CalculateRate(x.PresentCount + x.LateCount, x.Total),
                LateCount = x.LateCount,
                AbsentCount = x.AbsentCount,
                LeaveCount = x.LeaveCount
            }).OrderBy(x => x.Date).ToList();

            return ApiResponse<List<AttendanceTrendDto>>.Success(result);
        }

        /// <summary>班级考勤统计</summary>
        public async Task<ApiResponse<ClassStatisticsDto>> Handle(GetClassStatisticsQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == query.ClassId && !c.IsDeleted, cancellationToken);
            if (cls is null)
            {
                return ApiResponse<ClassStatisticsDto>.Fail(Msg.Common.EntityNotFound($"班级 {query.ClassId}"), 404);
            }

            var sessionQuery = db.Queryable<AttendanceSession>().Where(s => !s.IsDeleted && s.ClassId == query.ClassId);
            if (query.StartDate.HasValue) sessionQuery = sessionQuery.Where(s => s.StartTime >= query.StartDate.Value);
            if (query.EndDate.HasValue) sessionQuery = sessionQuery.Where(s => s.StartTime <= query.EndDate.Value);
            var totalSessions = await sessionQuery.CountAsync();

            var stats = await db.Queryable<AttendanceRecord>()
                .Where(r => !r.IsDeleted && SqlFunc.Subqueryable<AttendanceSession>()
                    .Where(s => s.Id == r.SessionId && !s.IsDeleted && s.ClassId == query.ClassId
                        && (!query.StartDate.HasValue || s.StartTime >= query.StartDate.Value)
                        && (!query.EndDate.HasValue || s.StartTime <= query.EndDate.Value)).Any())
                .Select(r => new
                {
                    Total = SqlFunc.AggregateCount(r.Id),
                    PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                    LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                    AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                    LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
                }).FirstAsync();

            return ApiResponse<ClassStatisticsDto>.Success(new ClassStatisticsDto
            {
                ClassId = query.ClassId,
                ClassName = cls.Name,
                TotalSessions = totalSessions,
                AttendanceRate = CalculateRate(stats?.PresentCount ?? 0 + (stats?.LateCount ?? 0), stats?.Total ?? 0),
                LateCount = stats?.LateCount ?? 0,
                AbsentCount = stats?.AbsentCount ?? 0,
                LeaveCount = stats?.LeaveCount ?? 0
            });
        }

        /// <summary>课程考勤统计</summary>
        public async Task<ApiResponse<ClassStatisticsDto>> Handle(GetCourseStatisticsQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var course = await db.Queryable<Course>().FirstAsync(c => c.Id == query.CourseId && !c.IsDeleted, cancellationToken);
            if (course is null)
            {
                return ApiResponse<ClassStatisticsDto>.Fail(Msg.Common.EntityNotFound($"课程 {query.CourseId}"), 404);
            }

            var sessionQuery = db.Queryable<AttendanceSession>().Where(s => !s.IsDeleted && s.CourseId == query.CourseId);
            if (query.StartDate.HasValue) sessionQuery = sessionQuery.Where(s => s.StartTime >= query.StartDate.Value);
            if (query.EndDate.HasValue) sessionQuery = sessionQuery.Where(s => s.StartTime <= query.EndDate.Value);
            var totalSessions = await sessionQuery.CountAsync();

            var stats = await db.Queryable<AttendanceRecord>()
                .Where(r => !r.IsDeleted && SqlFunc.Subqueryable<AttendanceSession>()
                    .Where(s => s.Id == r.SessionId && !s.IsDeleted && s.CourseId == query.CourseId
                        && (!query.StartDate.HasValue || s.StartTime >= query.StartDate.Value)
                        && (!query.EndDate.HasValue || s.StartTime <= query.EndDate.Value)).Any())
                .Select(r => new
                {
                    Total = SqlFunc.AggregateCount(r.Id),
                    PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                    LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                    AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                    LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
                }).FirstAsync();

            return ApiResponse<ClassStatisticsDto>.Success(new ClassStatisticsDto
            {
                ClassId = query.CourseId,
                ClassName = course.Name,
                TotalSessions = totalSessions,
                AttendanceRate = CalculateRate(stats?.PresentCount ?? 0 + (stats?.LateCount ?? 0), stats?.Total ?? 0),
                LateCount = stats?.LateCount ?? 0,
                AbsentCount = stats?.AbsentCount ?? 0,
                LeaveCount = stats?.LeaveCount ?? 0
            });
        }

        /// <summary>学生个人统计</summary>
        public async Task<ApiResponse<StudentStatisticsDto>> Handle(GetStudentStatisticsQuery query, CancellationToken cancellationToken)
        {
            // 学生只能查询自己的统计
            if (_currentUser.Role == UserRole.Student && _currentUser.UserId != query.StudentId)
            {
                return ApiResponse<StudentStatisticsDto>.Fail(Msg.Statistics.OnlyOwnStatistics, 403);
            }

            var db = _dbContext.Client;
            var student = await db.Queryable<Student>().FirstAsync(s => s.Id == query.StudentId && !s.IsDeleted, cancellationToken);
            if (student is null)
            {
                return ApiResponse<StudentStatisticsDto>.Fail(Msg.Common.EntityNotFound($"学生 {query.StudentId}"), 404);
            }

            var stats = await db.Queryable<AttendanceRecord>()
                .Where(r => !r.IsDeleted && r.StudentId == query.StudentId)
                .Select(r => new
                {
                    Total = SqlFunc.AggregateCount(r.Id),
                    PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                    LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0)),
                    AbsentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Absent, 1, 0)),
                    LeaveCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Leave, 1, 0))
                }).FirstAsync();

            var courseStats = await db.Queryable<AttendanceRecord, AttendanceSession, Course>((r, s, c) =>
                    new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id, JoinType.Inner, s.CourseId == c.Id))
                .Where((r, s, c) => !r.IsDeleted && !s.IsDeleted && !c.IsDeleted && r.StudentId == query.StudentId)
                .GroupBy((r, s, c) => new { c.Id, c.Name })
                .Select((r, s, c) => new
                {
                    CourseId = c.Id,
                    CourseName = c.Name,
                    TotalSessions = SqlFunc.AggregateCount(r.Id),
                    PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                    LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
                }).ToListAsync();

            var total = stats?.Total ?? 0;
            var present = stats?.PresentCount ?? 0;
            var late = stats?.LateCount ?? 0;

            return ApiResponse<StudentStatisticsDto>.Success(new StudentStatisticsDto
            {
                StudentId = query.StudentId,
                StudentName = student.Name,
                TotalSessions = total,
                PresentCount = present,
                LateCount = stats?.LateCount ?? 0,
                AbsentCount = stats?.AbsentCount ?? 0,
                LeaveCount = stats?.LeaveCount ?? 0,
                AttendanceRate = CalculateRate(present + late, total),
                CourseStatistics = courseStats.Select(cs => new CourseStatisticsItemDto
                {
                    CourseId = cs.CourseId,
                    CourseName = cs.CourseName,
                    TotalSessions = cs.TotalSessions,
                    AttendanceRate = CalculateRate(cs.PresentCount + cs.LateCount, cs.TotalSessions)
                }).ToList()
            });
        }

        /// <summary>教师统计</summary>
        public async Task<ApiResponse<TeacherStatisticsDto>> Handle(GetTeacherStatisticsQuery query, CancellationToken cancellationToken)
        {
            // 教师只能查询自己的统计
            if ((_currentUser.Role == UserRole.Teacher || _currentUser.Role == UserRole.Counselor) && _currentUser.UserId != query.TeacherId)
            {
                return ApiResponse<TeacherStatisticsDto>.Fail(Msg.Statistics.OnlyOwnStatistics, 403);
            }

            var db = _dbContext.Client;
            var teacher = await db.Queryable<Teacher>().FirstAsync(t => t.Id == query.TeacherId && !t.IsDeleted, cancellationToken);
            if (teacher is null)
            {
                return ApiResponse<TeacherStatisticsDto>.Fail(Msg.Common.EntityNotFound($"教师 {query.TeacherId}"), 404);
            }

            var totalCourses = await db.Queryable<Course>().Where(c => !c.IsDeleted && c.TeacherId == query.TeacherId).CountAsync();
            var totalSessions = await db.Queryable<AttendanceSession>().Where(s => !s.IsDeleted && s.TeacherId == query.TeacherId).CountAsync();

            var stats = await db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                    new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
                .Where((r, s) => !r.IsDeleted && !s.IsDeleted && s.TeacherId == query.TeacherId)
                .Select((r, s) => new
                {
                    Total = SqlFunc.AggregateCount(r.Id),
                    PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                    LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
                }).FirstAsync();

            var total = stats?.Total ?? 0;
            var present = stats?.PresentCount ?? 0;
            var late = stats?.LateCount ?? 0;

            return ApiResponse<TeacherStatisticsDto>.Success(new TeacherStatisticsDto
            {
                TeacherId = query.TeacherId,
                TeacherName = teacher.Name,
                TotalCourses = totalCourses,
                TotalSessions = totalSessions,
                AverageAttendanceRate = CalculateRate(present + late, total)
            });
        }

        /// <summary>导出会话考勤记录</summary>
        public async Task<IResult> Handle(ExportSessionRecordsQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var session = await db.Queryable<AttendanceSession, Course, Class, Teacher>((s, c, cls, t) =>
                    new JoinQueryInfos(JoinType.Left, s.CourseId == c.Id, JoinType.Left, s.ClassId == cls.Id, JoinType.Left, s.TeacherId == t.Id))
                .Where((s, c, cls, t) => s.Id == query.SessionId && !s.IsDeleted)
                .Select((s, c, cls, t) => new { SessionId = s.Id, CourseName = c.Name, ClassName = cls.Name, TeacherName = t.Name, s.StartTime, s.EndTime })
                .FirstAsync();

            if (session is null)
            {
                return Results.NotFound(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"考勤会话 {query.SessionId}"), 404));
            }

            var records = await db.Queryable<AttendanceRecord>()
                .Where(r => !r.IsDeleted && r.SessionId == query.SessionId)
                .OrderBy(r => r.StudentId).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("考勤记录");
            worksheet.Cell(1, 1).Value = $"考勤记录 - {session.CourseName} / {session.ClassName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 6).Merge();
            worksheet.Cell(2, 1).Value = $"教师：{session.TeacherName}    时间：{session.StartTime:yyyy-MM-dd HH:mm} ~ {session.EndTime:HH:mm}";
            worksheet.Range(2, 1, 2, 6).Merge();

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
                for (var col = 1; col <= 6; col++) worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"考勤记录_{query.SessionId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>导出班级考勤汇总</summary>
        public async Task<IResult> Handle(ExportClassAttendanceQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == query.ClassId && !c.IsDeleted, cancellationToken);
            if (cls is null)
            {
                return Results.NotFound(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"班级 {query.ClassId}"), 404));
            }

            var stats = await db.Queryable<AttendanceRecord, AttendanceSession, Student>((r, s, st) =>
                    new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id, JoinType.Inner, r.StudentId == st.Id))
                .Where((r, s, st) => !r.IsDeleted && !s.IsDeleted && !st.IsDeleted
                    && s.ClassId == query.ClassId && st.ClassId == query.ClassId
                    && s.StartTime >= query.StartDate && s.StartTime <= query.EndDate)
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
                }).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("班级考勤汇总");
            worksheet.Cell(1, 1).Value = $"班级考勤汇总 - {cls.Name}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 8).Merge();
            worksheet.Cell(2, 1).Value = $"统计区间：{query.StartDate:yyyy-MM-dd} ~ {query.EndDate:yyyy-MM-dd}";
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
                for (var col = 1; col <= 8; col++) worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"班级考勤汇总_{query.ClassId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>导出班级学生名单</summary>
        public async Task<IResult> Handle(ExportStudentListQuery query, CancellationToken cancellationToken)
        {
            var db = _dbContext.Client;
            var cls = await db.Queryable<Class>().FirstAsync(c => c.Id == query.ClassId && !c.IsDeleted, cancellationToken);
            if (cls is null)
            {
                return Results.NotFound(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"班级 {query.ClassId}"), 404));
            }

            var students = await db.Queryable<Student>().Where(s => !s.IsDeleted && s.ClassId == query.ClassId).OrderBy(s => s.Id).ToListAsync();

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
                for (var col = 1; col <= 5; col++) worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"学生名单_{query.ClassId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>系主任本系教师考勤汇总</summary>
        public async Task<ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>> Handle(
            GetDepartmentTeacherAttendanceSummaryQuery query, CancellationToken cancellationToken)
        {
            // 鉴权：当前用户必须为指定院系的系主任
            var headCheck = await EnsureDepartmentHeadAsync(query.DepartmentId, cancellationToken);
            if (headCheck.Failed) return ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>.Fail(headCheck.Message!, headCheck.Code);

            var db = _dbContext.Client;

            // 默认本周（周一 00:00 至 周日 23:59:59）
            var (startDate, endDate) = NormalizeDateRange(query.StartDate, query.EndDate);
            if (endDate < startDate)
            {
                return ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>.Fail(Msg.Statistics.EndDateBeforeStart, 400);
            }

            // 1) 查询该系所有未删除教师，避免 N+1
            var teachers = await db.Queryable<Teacher>()
                .Where(t => t.DepartmentId == query.DepartmentId && !t.IsDeleted)
                .OrderBy(t => t.Id)
                .ToListAsync(cancellationToken);
            if (teachers.Count == 0)
            {
                return ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>.Success(new List<DepartmentTeacherAttendanceSummaryDto>());
            }

            var teacherIds = teachers.Select(t => t.Id).ToList();

            // 2) 批量查询该系教师在日期范围内的考勤会话
            var sessions = await db.Queryable<AttendanceSession>()
                .Where(s => !s.IsDeleted
                    && teacherIds.Contains(s.TeacherId)
                    && s.StartTime >= startDate && s.StartTime <= endDate)
                .ToListAsync(cancellationToken);
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var sessionsByTeacher = sessions.GroupBy(s => s.TeacherId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3) 批量查询这些会话的考勤记录并按会话聚合
            var records = sessionIds.Count == 0
                ? new List<AttendanceRecord>()
                : await db.Queryable<AttendanceRecord>()
                    .Where(r => !r.IsDeleted && sessionIds.Contains(r.SessionId))
                    .ToListAsync(cancellationToken);
            var recordsBySession = records.GroupBy(r => r.SessionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 4) 内存聚合：教师维度汇总
            var result = new List<DepartmentTeacherAttendanceSummaryDto>();
            foreach (var teacher in teachers)
            {
                var teacherSessions = sessionsByTeacher.GetValueOrDefault(teacher.Id) ?? new List<AttendanceSession>();
                var sessionRecords = teacherSessions
                    .SelectMany(s => recordsBySession.GetValueOrDefault(s.Id) ?? new List<AttendanceRecord>())
                    .ToList();

                var expected = sessionRecords.Count;
                var present = sessionRecords.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
                var leave = sessionRecords.Count(r => r.Status == AttendanceStatus.Leave);
                var absent = sessionRecords.Count(r => r.Status == AttendanceStatus.Absent);

                result.Add(new DepartmentTeacherAttendanceSummaryDto
                {
                    TeacherId = teacher.Id,
                    TeacherName = teacher.Name,
                    SessionCount = teacherSessions.Count,
                    ExpectedCount = expected,
                    PresentCount = present,
                    LeaveCount = leave,
                    AbsentCount = absent,
                    AttendanceRate = CalculateRate(present, expected)
                });
            }

            _logger.LogInformation("系主任 {TeacherId} 查询院系 {DepartmentId} 教师考勤汇总，教师 {Count} 人",
                _currentUser.UserId, query.DepartmentId, result.Count);

            return ApiResponse<List<DepartmentTeacherAttendanceSummaryDto>>.Success(result);
        }

        /// <summary>系主任本系调换课统计</summary>
        public async Task<ApiResponse<DepartmentSwapSummaryDto>> Handle(
            GetDepartmentSwapSummaryQuery query, CancellationToken cancellationToken)
        {
            // 鉴权：当前用户必须为指定院系的系主任
            var headCheck = await EnsureDepartmentHeadAsync(query.DepartmentId, cancellationToken);
            if (headCheck.Failed) return ApiResponse<DepartmentSwapSummaryDto>.Fail(headCheck.Message!, headCheck.Code);

            var db = _dbContext.Client;

            // 默认本周
            var (startDate, endDate) = NormalizeDateRange(query.StartDate, query.EndDate);
            if (endDate < startDate)
            {
                return ApiResponse<DepartmentSwapSummaryDto>.Fail(Msg.Statistics.EndDateBeforeStart, 400);
            }

            // 1) 查询该系所有教师工号
            var teachers = await db.Queryable<Teacher>()
                .Where(t => t.DepartmentId == query.DepartmentId && !t.IsDeleted)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync(cancellationToken);
            if (teachers.Count == 0)
            {
                return ApiResponse<DepartmentSwapSummaryDto>.Success(new DepartmentSwapSummaryDto());
            }

            var teacherIds = teachers.Select(t => t.Id).ToList();
            var teacherMap = teachers.ToDictionary(t => t.Id);

            // 2) 批量查询本系教师发起或被委托的调换课申请（按创建时间过滤）
            var swaps = await db.Queryable<CourseSwapRequest>()
                .Where(s => !s.IsDeleted
                    && (teacherIds.Contains(s.OriginalTeacherId) || teacherIds.Contains(s.SubstituteTeacherId))
                    && s.CreateTime >= startDate && s.CreateTime <= endDate)
                .ToListAsync(cancellationToken);

            // 3) 状态分布 + 已逾期（Pending 且超 SLA）统计
            var now = DateTime.UtcNow;
            var slaDeadline = now.AddHours(-CourseSwapSlaConstants.SlaHours);

            var summary = new DepartmentSwapSummaryDto
            {
                TotalCount = swaps.Count,
                PendingCount = swaps.Count(s => s.Status == SwapStatus.Pending),
                AcceptedCount = swaps.Count(s => s.Status == SwapStatus.Accepted),
                RejectedCount = swaps.Count(s => s.Status == SwapStatus.Rejected),
                CancelledCount = swaps.Count(s => s.Status == SwapStatus.Cancelled),
                ExpiredCount = swaps.Count(s => s.Status == SwapStatus.Pending && s.CreateTime < slaDeadline)
            };

            // 4) 涉及教师明细聚合：发起数 + 被委托数
            var statsMap = new Dictionary<string, TeacherSwapStatDto>();
            foreach (var swap in swaps)
            {
                if (!statsMap.TryGetValue(swap.OriginalTeacherId, out var initiatorStat))
                {
                    initiatorStat = new TeacherSwapStatDto
                    {
                        TeacherId = swap.OriginalTeacherId,
                        TeacherName = teacherMap.GetValueOrDefault(swap.OriginalTeacherId)?.Name ?? swap.OriginalTeacherId
                    };
                    statsMap[swap.OriginalTeacherId] = initiatorStat;
                }
                initiatorStat.InitiatedCount++;

                if (!statsMap.TryGetValue(swap.SubstituteTeacherId, out var substituteStat))
                {
                    substituteStat = new TeacherSwapStatDto
                    {
                        TeacherId = swap.SubstituteTeacherId,
                        TeacherName = teacherMap.GetValueOrDefault(swap.SubstituteTeacherId)?.Name ?? swap.SubstituteTeacherId
                    };
                    statsMap[swap.SubstituteTeacherId] = substituteStat;
                }
                substituteStat.SubstitutedCount++;
            }

            summary.TeacherStats = statsMap.Values
                .OrderByDescending(t => t.InitiatedCount + t.SubstitutedCount)
                .ThenBy(t => t.TeacherId)
                .ToList();

            _logger.LogInformation("系主任 {TeacherId} 查询院系 {DepartmentId} 调换课统计，总申请 {Total} 笔",
                _currentUser.UserId, query.DepartmentId, summary.TotalCount);

            return ApiResponse<DepartmentSwapSummaryDto>.Success(summary);
        }

        /// <summary>系主任本系课程开课率</summary>
        public async Task<ApiResponse<DepartmentCourseCoverageDto>> Handle(
            GetDepartmentCourseCoverageQuery query, CancellationToken cancellationToken)
        {
            // 鉴权：当前用户必须为指定院系的系主任
            var headCheck = await EnsureDepartmentHeadAsync(query.DepartmentId, cancellationToken);
            if (headCheck.Failed) return ApiResponse<DepartmentCourseCoverageDto>.Fail(headCheck.Message!, headCheck.Code);

            var db = _dbContext.Client;

            // 1) 查询该系所有教师工号
            var teacherIds = await db.Queryable<Teacher>()
                .Where(t => t.DepartmentId == query.DepartmentId && !t.IsDeleted)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
            if (teacherIds.Count == 0)
            {
                return ApiResponse<DepartmentCourseCoverageDto>.Success(new DepartmentCourseCoverageDto());
            }

            // 2) 本系教师承接的课程（Course.TeacherId ∈ 教师列表）
            var courses = await db.Queryable<Course>()
                .Where(c => !c.IsDeleted && teacherIds.Contains(c.TeacherId))
                .ToListAsync(cancellationToken);
            var courseIds = courses.Select(c => c.Id).ToList();

            // 3) 批量查询排课条目
            var schedules = courseIds.Count == 0
                ? new List<CourseSchedule>()
                : await db.Queryable<CourseSchedule>()
                    .Where(s => !s.IsDeleted && courseIds.Contains(s.CourseId))
                    .ToListAsync(cancellationToken);
            var scheduledCourseIds = schedules.Select(s => s.CourseId).Distinct().ToHashSet();

            var total = courses.Count;
            var scheduled = scheduledCourseIds.Count;
            var unscheduled = total - scheduled;

            // 4) 班级维度开课明细：班级 -> 专业 -> 院系
            var classInfos = await db.Queryable<Class, Major>((c, m) =>
                    new JoinQueryInfos(JoinType.Inner, c.MajorId == m.Id))
                .Where((c, m) => !c.IsDeleted && !m.IsDeleted && m.DepartmentId == query.DepartmentId)
                .Select((c, m) => new { c.Id, c.Name })
                .ToListAsync(cancellationToken);

            // 5) 按班级聚合排课条目（含合班：ClassIds 逗号分隔）
            // 使用一次 schedules 查询结果在内存中聚合，避免 N+1
            var classCoverage = new List<ClassCoverageDto>(classInfos.Count);
            foreach (var cls in classInfos)
            {
                var classIdStr = cls.Id.ToString();
                var classSchedules = schedules
                    .Where(s => s.ClassId == cls.Id
                        || (!string.IsNullOrEmpty(s.ClassIds) && s.ClassIds.Split(',').Contains(classIdStr)))
                    .ToList();
                classCoverage.Add(new ClassCoverageDto
                {
                    ClassId = cls.Id,
                    ClassName = cls.Name,
                    ScheduledCourseCount = classSchedules.Select(s => s.CourseId).Distinct().Count(),
                    WeeklySessionCount = classSchedules.Count
                });
            }

            var result = new DepartmentCourseCoverageDto
            {
                TotalCourseCount = total,
                ScheduledCourseCount = scheduled,
                UnscheduledCourseCount = unscheduled,
                CoverageRate = CalculateRate(scheduled, total),
                ClassCoverage = classCoverage
                    .OrderByDescending(c => c.WeeklySessionCount)
                    .ThenBy(c => c.ClassId)
                    .ToList()
            };

            _logger.LogInformation("系主任 {TeacherId} 查询院系 {DepartmentId} 课程开课率，总课程 {Total} 门",
                _currentUser.UserId, query.DepartmentId, result.TotalCourseCount);

            return ApiResponse<DepartmentCourseCoverageDto>.Success(result);
        }

        /// <summary>
        /// 校验当前用户是否为指定院系的系主任
        /// </summary>
        /// <param name="departmentId">路由参数中的院系 Id</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>校验结果：Failed=true 表示拒绝访问</returns>
        private async Task<(bool Failed, string? Message, int Code)> EnsureDepartmentHeadAsync(
            long departmentId, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return (true, Msg.Common.NoPermission, 401);
            }

            var teacher = await _dbContext.Client.Queryable<Teacher>()
                .FirstAsync(t => t.Id == userId && !t.IsDeleted, cancellationToken);
            if (teacher is null || !teacher.IsDepartmentHead)
            {
                _logger.LogWarning("用户 {UserId} 不具备系主任身份，拒绝访问院系 {DepartmentId} 报表", userId, departmentId);
                return (true, Msg.Common.NoPermission, 403);
            }

            // 系主任所辖院系必须与路由参数一致，防止跨院系越权
            var headDepartmentId = teacher.HeadDepartmentId ?? teacher.DepartmentId;
            if (headDepartmentId != departmentId)
            {
                _logger.LogWarning("系主任 {UserId} 所辖院系 {HeadDept} 与请求院系 {RequestDept} 不匹配，拒绝访问",
                    userId, headDepartmentId, departmentId);
                return (true, Msg.Common.NoPermission, 403);
            }

            return (false, null, 0);
        }

        /// <summary>
        /// 规范化日期范围：未传则默认本周（周一 00:00:00 至 周日 23:59:59）
        /// </summary>
        /// <param name="startDate">查询参数中的起始日期</param>
        /// <param name="endDate">查询参数中的结束日期</param>
        /// <returns>规范化后的日期范围（UTC）</returns>
        private static (DateTime StartDate, DateTime EndDate) NormalizeDateRange(DateTime? startDate, DateTime? endDate)
        {
            // 计算本周周一（DateTime.DayOfWeek：Sunday=0, Monday=1...Saturday=6；转为周一=1...周日=7）
            var todayUtc = DateTime.UtcNow.Date;
            var dayOfWeek = (int)todayUtc.DayOfWeek;
            var mondayOffset = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
            var monday = todayUtc.AddDays(mondayOffset);
            var sunday = monday.AddDays(7).AddSeconds(-1);

            var start = startDate?.Date ?? monday;
            var end = endDate?.Date.AddDays(1).AddSeconds(-1) ?? sunday;
            return (start, end);
        }

        /// <summary>计算出勤率</summary>
        private static double CalculateRate(long numerator, long denominator)
            => denominator == 0 ? 0 : Math.Round((double)numerator / denominator * 100, 2);

        /// <summary>计算指定条件下的出勤率</summary>
        private async Task<double> CalculateAttendanceRateAsync(ISqlSugarClient db, long? classId, DateTime? startDate, DateTime? endDate)
        {
            var query = db.Queryable<AttendanceRecord, AttendanceSession>((r, s) =>
                    new JoinQueryInfos(JoinType.Inner, r.SessionId == s.Id))
                .Where((r, s) => !r.IsDeleted && !s.IsDeleted);

            if (classId.HasValue) query = query.Where((r, s) => s.ClassId == classId.Value);
            if (startDate.HasValue) query = query.Where((r, s) => s.StartTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where((r, s) => s.StartTime < endDate.Value);

            var stat = await query.Select((r, s) => new
            {
                Total = SqlFunc.AggregateCount(r.Id),
                PresentCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Present, 1, 0)),
                LateCount = SqlFunc.AggregateSum(SqlFunc.IIF(r.Status == AttendanceStatus.Late, 1, 0))
            }).FirstAsync();

            return CalculateRate(stat?.PresentCount ?? 0 + (stat?.LateCount ?? 0), stat?.Total ?? 0);
        }
    }
}