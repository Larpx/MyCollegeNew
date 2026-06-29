using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Constants;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses
{
    /// <summary>
    /// 接课申请静态处理器，提供教师主动接课、系主任查询待审批与审批操作。
    /// 类本身非静态仅为支持 <c>ILogger&lt;T&gt;</c> 类型参数，所有方法均为静态，
    /// 可直接作为 Minimal API 方法组委托注册
    /// </summary>
    public class AssignmentHandlers
    {
        /// <summary>
        /// 教师主动接课申请：校验当前用户为教师、课程模板处于开放接课状态，
        /// 创建 CourseAssignment 记录（Status=Pending）
        /// </summary>
        /// <param name="request">接课申请请求 DTO</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="logger">日志器</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>创建结果，含详情响应 DTO</returns>
        public static async Task<IResult> ApplyAsync(
            CreateAssignmentRequestDto request,
            IDbContext dbContext,
            ICurrentUser currentUser,
            ILogger<AssignmentHandlers> logger,
            CancellationToken ct)
        {
            // 安全考虑：始终使用当前登录用户的工号，忽略请求体中的 TeacherId 以防越权
            var teacherId = currentUser.UserId;
            if (string.IsNullOrEmpty(teacherId))
            {
                return Results.Ok(ApiResponse<AssignmentResponseDto>.Fail(Msg.Common.NoPermission, 401));
            }

            var db = dbContext.Client;

            // 校验：当前用户必须是教师身份（教师记录存在且未删除）
            var teacherExists = await db.Queryable<Teacher>().AnyAsync(t => t.Id == teacherId && !t.IsDeleted, ct);
            if (!teacherExists)
            {
                return Results.Ok(ApiResponse<AssignmentResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {teacherId}"), 404));
            }

            // 校验：课程存在且 Status=OpenForPick
            var course = await db.Queryable<Course>().FirstAsync(c => c.Id == request.CourseId && !c.IsDeleted, ct);
            if (course is null)
            {
                return Results.Ok(ApiResponse<AssignmentResponseDto>.Fail(Msg.Common.EntityNotFound($"课程 {request.CourseId}"), 404));
            }

            if (course.Status != CourseStatus.OpenForPick)
            {
                return Results.Ok(ApiResponse<AssignmentResponseDto>.Fail("当前课程模板未开放接课", 400));
            }

            // 校验：班级 Id 列表去重后全部存在
            var classIds = request.ClassIds.Distinct().ToList();
            if (classIds.Count == 0)
            {
                return Results.Ok(ApiResponse<AssignmentResponseDto>.Fail("班级列表不能为空", 400));
            }

            var existingClassCount = await db.Queryable<Class>()
                .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                .CountAsync(ct);
            if (existingClassCount != classIds.Count)
            {
                return Results.Ok(ApiResponse<AssignmentResponseDto>.Fail("部分班级不存在", 404));
            }

            // 创建 CourseAssignment 记录，ClassIds 以逗号分隔字符串存储
            var assignment = new CourseAssignment
            {
                CourseId = request.CourseId,
                TeacherId = teacherId,
                ClassIds = string.Join(",", classIds),
                Semester = request.Semester,
                Status = AssignmentStatus.Pending,
                ApplyReason = request.ApplyReason,
                CreateTime = DateTime.UtcNow
            };
            var id = await db.Insertable(assignment).ExecuteReturnIdentityAsync(ct);
            logger.LogInformation("教师 {TeacherId} 接课申请课程 {CourseId}，分配记录 {AssignmentId}", teacherId, request.CourseId, id);

            var dto = await GetAssignmentByIdAsync(db, id, ct);
            return Results.Ok(ApiResponse<AssignmentResponseDto>.Success(dto!));
        }

        /// <summary>
        /// 查询待审批接课列表（系主任视角）：仅返回当前系主任创建的课程模板下的 Pending 申请
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>待审批接课申请列表</returns>
        public static async Task<IResult> GetPendingAsync(
            IDbContext dbContext,
            ICurrentUser currentUser,
            CancellationToken ct)
        {
            var headId = currentUser.UserId;
            var db = dbContext.Client;

            // 关联 Course / Teacher / CourseAssignment，过滤 Pending 且课程由当前系主任创建
            var rows = await db.Queryable<CourseAssignment, Course, Teacher>((a, c, t) =>
                    new JoinQueryInfos(JoinType.Left, a.CourseId == c.Id, JoinType.Left, a.TeacherId == t.Id))
                .Where((a, c, t) => a.Status == AssignmentStatus.Pending && !a.IsDeleted
                    && !c.IsDeleted && !t.IsDeleted && c.CreatorId == headId)
                .OrderBy((a, c, t) => a.CreateTime, OrderByType.Desc)
                .Select((a, c, t) => new PendingAssignmentRow
                {
                    Id = a.Id,
                    CourseId = a.CourseId,
                    CourseName = c.Name,
                    TeacherId = a.TeacherId,
                    TeacherName = t.Name,
                    ClassIds = a.ClassIds,
                    Semester = a.Semester,
                    Status = a.Status,
                    ApplyReason = a.ApplyReason,
                    ReviewRemark = a.ReviewRemark,
                    CreateTime = a.CreateTime
                }).ToListAsync(ct);

            if (rows.Count == 0)
            {
                return Results.Ok(ApiResponse<List<AssignmentResponseDto>>.Success(new List<AssignmentResponseDto>()));
            }

            // 批量查询班级名称，构造 ClassNames
            var allClassIds = rows.SelectMany(r => ParseClassIds(r.ClassIds)).Distinct().ToList();
            var classList = await db.Queryable<Class>()
                .Where(c => allClassIds.Contains(c.Id) && !c.IsDeleted)
                .Select(c => new ClassNameRow { Id = c.Id, Name = c.Name })
                .ToListAsync(ct);
            var classDict = classList.ToDictionary(c => c.Id, c => c.Name);

            var dtos = rows.Select(r => ToResponseDto(r, classDict)).ToList();
            return Results.Ok(ApiResponse<List<AssignmentResponseDto>>.Success(dtos));
        }

        /// <summary>
        /// 系主任审批接课申请：通过则将 CourseAssignment.Status 置为 Active 并更新 Course.TeacherId；
        /// 驳回则置为 Withdrawn。通过操作在事务中执行以保证一致性
        /// </summary>
        /// <param name="assignmentId">接课分配 Id</param>
        /// <param name="request">审批请求 DTO</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="logger">日志器</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>审批结果</returns>
        public static async Task<IResult> ReviewAsync(
            long assignmentId,
            ReviewAssignmentRequestDto request,
            IDbContext dbContext,
            ICurrentUser currentUser,
            ILogger<AssignmentHandlers> logger,
            CancellationToken ct)
        {
            var headId = currentUser.UserId;
            var db = dbContext.Client;

            // 校验：Assignment 存在
            var assignment = await db.Queryable<CourseAssignment>()
                .FirstAsync(a => a.Id == assignmentId && !a.IsDeleted, ct);
            if (assignment is null)
            {
                return Results.Ok(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"接课申请 {assignmentId}"), 404));
            }

            // 校验：状态必须为 Pending
            if (assignment.Status != AssignmentStatus.Pending)
            {
                return Results.Ok(ApiResponse<object>.Fail("该接课申请已审批，无法重复操作", 400));
            }

            // 校验：归属当前系主任（课程创建者为当前系主任）
            var courseBelongsToHead = await db.Queryable<Course>()
                .AnyAsync(c => c.Id == assignment.CourseId && !c.IsDeleted && c.CreatorId == headId, ct);
            if (!courseBelongsToHead)
            {
                return Results.Ok(ApiResponse<object>.Fail("无权审批该接课申请", 403));
            }

            var now = DateTime.UtcNow;
            assignment.ReviewRemark = request.ReviewRemark;
            assignment.UpdateTime = now;

            if (request.Approved)
            {
                // 审批通过：Assignment.Status=Active，Course.TeacherId=Assignment.TeacherId
                assignment.Status = AssignmentStatus.Active;

                // 取出课程实体以更新 TeacherId，并在事务中保持一致性
                var course = await db.Queryable<Course>()
                    .FirstAsync(c => c.Id == assignment.CourseId && !c.IsDeleted, ct);
                if (course is null)
                {
                    return Results.Ok(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"课程 {assignment.CourseId}"), 404));
                }

                course.TeacherId = assignment.TeacherId;
                course.UpdateTime = now;

                await db.Ado.UseTranAsync(async () =>
                {
                    await db.Updateable(assignment)
                        .UpdateColumns(a => new { a.Status, a.ReviewRemark, a.UpdateTime })
                        .ExecuteCommandAsync(ct);
                    await db.Updateable(course)
                        .UpdateColumns(c => new { c.TeacherId, c.UpdateTime })
                        .ExecuteCommandAsync(ct);
                });

                logger.LogInformation("系主任 {HeadId} 通过接课申请 {AssignmentId}，课程 {CourseId} 任课教师更新为 {TeacherId}",
                    headId, assignmentId, assignment.CourseId, assignment.TeacherId);
                return Results.Ok(ApiResponse<object>.Success("审批通过"));
            }
            else
            {
                // 审批驳回：Assignment.Status=Withdrawn
                assignment.Status = AssignmentStatus.Withdrawn;
                await db.Updateable(assignment)
                    .UpdateColumns(a => new { a.Status, a.ReviewRemark, a.UpdateTime })
                    .ExecuteCommandAsync(ct);

                logger.LogInformation("系主任 {HeadId} 驳回接课申请 {AssignmentId}", headId, assignmentId);
                return Results.Ok(ApiResponse<object>.Success("已驳回"));
            }
        }

        /// <summary>
        /// 根据 Id 查询接课申请详情并构造响应 DTO（含班级名称）
        /// </summary>
        /// <param name="db">SqlSugar 客户端</param>
        /// <param name="id">接课分配 Id</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应 DTO，未找到返回 null</returns>
        private static async Task<AssignmentResponseDto?> GetAssignmentByIdAsync(ISqlSugarClient db, long id, CancellationToken ct)
        {
            var row = await db.Queryable<CourseAssignment, Course, Teacher>((a, c, t) =>
                    new JoinQueryInfos(JoinType.Left, a.CourseId == c.Id, JoinType.Left, a.TeacherId == t.Id))
                .Where((a, c, t) => a.Id == id && !a.IsDeleted)
                .Select((a, c, t) => new PendingAssignmentRow
                {
                    Id = a.Id,
                    CourseId = a.CourseId,
                    CourseName = c.Name,
                    TeacherId = a.TeacherId,
                    TeacherName = t.Name,
                    ClassIds = a.ClassIds,
                    Semester = a.Semester,
                    Status = a.Status,
                    ApplyReason = a.ApplyReason,
                    ReviewRemark = a.ReviewRemark,
                    CreateTime = a.CreateTime
                }).FirstAsync(ct);

            if (row is null)
            {
                return null;
            }

            var classIds = ParseClassIds(row.ClassIds);
            var classList = await db.Queryable<Class>()
                .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                .Select(c => new ClassNameRow { Id = c.Id, Name = c.Name })
                .ToListAsync(ct);
            var classDict = classList.ToDictionary(c => c.Id, c => c.Name);

            return ToResponseDto(row, classDict);
        }

        /// <summary>
        /// 将逗号分隔的班级 Id 字符串解析为去重的 Id 列表
        /// </summary>
        /// <param name="classIds">逗号分隔的班级 Id 字符串</param>
        /// <returns>班级 Id 列表</returns>
        private static List<long> ParseClassIds(string classIds)
        {
            if (string.IsNullOrWhiteSpace(classIds))
            {
                return new List<long>();
            }

            return classIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 将中间查询行转换为响应 DTO，并根据班级字典填充 ClassNames
        /// </summary>
        /// <param name="row">中间查询行</param>
        /// <param name="classDict">班级 Id → 名称字典</param>
        /// <returns>响应 DTO</returns>
        private static AssignmentResponseDto ToResponseDto(PendingAssignmentRow row, Dictionary<long, string> classDict)
        {
            var classIds = ParseClassIds(row.ClassIds);
            return new AssignmentResponseDto
            {
                Id = row.Id,
                CourseId = row.CourseId,
                CourseName = row.CourseName,
                TeacherId = row.TeacherId,
                TeacherName = row.TeacherName,
                ClassIds = classIds,
                ClassNames = classIds.Select(id => classDict.GetValueOrDefault(id, string.Empty)).ToList(),
                Semester = row.Semester,
                Status = row.Status.ToString(),
                ApplyReason = row.ApplyReason,
                ReviewRemark = row.ReviewRemark,
                CreateTime = row.CreateTime
            };
        }

        /// <summary>
        /// 中间查询行：用于 SqlSugar 多表联查后承接结果
        /// </summary>
        private sealed class PendingAssignmentRow
        {
            /// <summary>接课分配主键</summary>
            public long Id { get; set; }

            /// <summary>课程模板 Id</summary>
            public long CourseId { get; set; }

            /// <summary>课程名称</summary>
            public string CourseName { get; set; } = string.Empty;

            /// <summary>任课教师工号</summary>
            public string TeacherId { get; set; } = string.Empty;

            /// <summary>任课教师姓名</summary>
            public string TeacherName { get; set; } = string.Empty;

            /// <summary>合班班级 Id 列表（逗号分隔字符串）</summary>
            public string ClassIds { get; set; } = string.Empty;

            /// <summary>学期标识</summary>
            public string Semester { get; set; } = string.Empty;

            /// <summary>接课状态</summary>
            public AssignmentStatus Status { get; set; }

            /// <summary>接课申请理由</summary>
            public string? ApplyReason { get; set; }

            /// <summary>系主任审批备注</summary>
            public string? ReviewRemark { get; set; }

            /// <summary>创建时间（UTC）</summary>
            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 班级名称查询行
        /// </summary>
        private sealed class ClassNameRow
        {
            /// <summary>班级 Id</summary>
            public long Id { get; set; }

            /// <summary>班级名称</summary>
            public string Name { get; set; } = string.Empty;
        }
    }
}
