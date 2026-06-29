using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Constants;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using Larpx.PersonalTools.MyCollegeNew.Infrastructure.Scheduling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses
{
    /// <summary>
    /// 调换课申请静态处理器，提供原任课教师发起调换、代课教师确认/拒绝、
    /// 原任课教师撤销及查询个人相关申请等操作。类本身非静态仅为支持
    /// <c>ILogger&lt;T&gt;</c> 类型参数，所有方法均为静态，可直接作为 Minimal API 方法组委托注册
    /// </summary>
    public class SwapHandlers
    {
        /// <summary>
        /// 原任课教师发起调换课申请：校验当前用户为该排课的实际讲课人（含覆盖层判断）、
        /// 代课教师存在且与原教师不同、代课教师在目标周次范围内空闲，创建 CourseSwapRequest（Status=Pending）
        /// </summary>
        /// <param name="request">调换课申请请求 DTO</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="conflictService">排课冲突校验服务</param>
        /// <param name="logger">日志器</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>创建结果，含调换课申请详情响应 DTO</returns>
        public static async Task<IResult> CreateAsync(
            CreateSwapRequestDto request,
            IDbContext dbContext,
            ICurrentUser currentUser,
            IScheduleConflictService conflictService,
            ILogger<SwapHandlers> logger,
            CancellationToken ct)
        {
            // 安全考虑：始终使用当前登录用户的工号，忽略请求体中可能携带的发起人工号以防越权
            var teacherId = currentUser.UserId;
            if (string.IsNullOrEmpty(teacherId))
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail(Msg.Common.NoPermission, 401));
            }

            var db = dbContext.Client;

            // 校验：周次范围合法（起始不能大于结束）
            if (request.StartWeek > request.EndWeek)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail(Msg.Course.StartWeekAfterEnd, 400));
            }

            // 校验：排课记录存在且未删除
            var schedule = await db.Queryable<CourseSchedule>()
                .FirstAsync(s => s.Id == request.ScheduleId && !s.IsDeleted, ct);
            if (schedule is null)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail(Msg.Common.EntityNotFound($"排课 {request.ScheduleId}"), 404));
            }

            // 校验：调换周次必须在排课自身的周次区间内，避免对不存在的周次发起无效调换
            if (request.StartWeek < schedule.StartWeek || request.EndWeek > schedule.EndWeek)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail("调换周次超出排课周次范围", 400));
            }

            // 计算实际讲课人：查询与请求周次区间重叠的代课覆盖层，若存在则以覆盖层代课教师为准
            // 这与 ScheduleConflictService 中的 effectiveTeacherByScheduleId 逻辑一致
            var overlappingOverride = await db.Queryable<CourseScheduleOverride>()
                .Where(o => !o.IsDeleted && o.ScheduleId == schedule.Id)
                .Where(o => o.StartWeek <= request.EndWeek && o.EndWeek >= request.StartWeek)
                .OrderBy(o => o.Id, OrderByType.Desc)
                .FirstAsync(ct);
            var effectiveTeacherId = overlappingOverride?.SubstituteTeacherId ?? schedule.TeacherId;

            // 校验：当前用户必须为该排课在目标周次范围内的实际讲课人
            if (effectiveTeacherId != teacherId)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail("仅当前排课的任课教师可发起调换课", 403));
            }

            // 校验：代课教师存在且未删除
            var substituteExists = await db.Queryable<Teacher>()
                .AnyAsync(t => t.Id == request.SubstituteTeacherId && !t.IsDeleted, ct);
            if (!substituteExists)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail(Msg.Common.EntityNotFound($"教师 {request.SubstituteTeacherId}"), 404));
            }

            // 校验：代课教师不能与当前实际讲课人相同
            if (request.SubstituteTeacherId == effectiveTeacherId)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail("代课教师与当前任课教师相同", 400));
            }

            // 校验：代课教师在目标周次范围内空闲（调用排课冲突校验服务）
            // 传入 ScheduleId 以排除自身排课，避免对原班级/教室产生假冲突
            var conflictInput = new ScheduleValidationInput
            {
                ScheduleId = schedule.Id,
                TeacherId = request.SubstituteTeacherId,
                ClassIds = ParseScheduleClassIds(schedule),
                DayOfWeek = schedule.DayOfWeek,
                StartSection = schedule.StartSection,
                EndSection = schedule.EndSection,
                StartWeek = request.StartWeek,
                EndWeek = request.EndWeek,
                Classroom = schedule.Classroom
            };
            var conflictResult = await conflictService.ValidateAsync(conflictInput, ct);
            if (conflictResult.HasConflict)
            {
                return Results.Ok(ApiResponse<SwapRequestResponseDto>.Fail(string.Join("；", conflictResult.Conflicts), 409));
            }

            // 创建 CourseSwapRequest，OriginalTeacherId 记录当前实际讲课人（含覆盖层场景）
            var swapRequest = new CourseSwapRequest
            {
                ScheduleId = request.ScheduleId,
                OriginalTeacherId = effectiveTeacherId,
                SubstituteTeacherId = request.SubstituteTeacherId,
                StartWeek = request.StartWeek,
                EndWeek = request.EndWeek,
                Reason = request.Reason,
                Status = SwapStatus.Pending,
                CreateTime = DateTime.UtcNow
            };
            var id = await db.Insertable(swapRequest).ExecuteReturnIdentityAsync(ct);
            logger.LogInformation("教师 {TeacherId} 发起调换课申请 {SwapId}，排课 {ScheduleId}，代课 {SubstituteTeacherId}",
                teacherId, id, request.ScheduleId, request.SubstituteTeacherId);

            var dto = await GetSwapByIdAsync(db, id, ct);
            return Results.Ok(ApiResponse<SwapRequestResponseDto>.Success(dto!));
        }

        /// <summary>
        /// 查询当前用户相关的调换课申请：role=initiator 查询自己发起的；
        /// role=substitute 查询发给自己且待确认的
        /// </summary>
        /// <param name="role">角色标识：initiator 或 substitute</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>调换课申请列表</returns>
        public static async Task<IResult> GetMyAsync(
            string? role,
            IDbContext dbContext,
            ICurrentUser currentUser,
            CancellationToken ct)
        {
            var teacherId = currentUser.UserId;
            if (string.IsNullOrEmpty(teacherId))
            {
                return Results.Ok(ApiResponse<List<SwapRequestResponseDto>>.Fail(Msg.Common.NoPermission, 401));
            }

            // 默认按发起人视角查询，substitute 时按代课人视角且仅返回 Pending
            var isSubstituteView = string.Equals(role, "substitute", StringComparison.OrdinalIgnoreCase);
            if (!isSubstituteView && !string.IsNullOrEmpty(role)
                && !string.Equals(role, "initiator", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Ok(ApiResponse<List<SwapRequestResponseDto>>.Fail("role 参数无效，仅支持 initiator 或 substitute", 400));
            }

            var db = dbContext.Client;

            // 五表联查：调换课申请 + 排课 + 课程 + 原教师 + 代课教师，拼装响应 DTO
            var rows = await db.Queryable<CourseSwapRequest, CourseSchedule, Course, Teacher, Teacher>((s, sch, c, orig, sub) =>
                    new JoinQueryInfos(
                        JoinType.Left, s.ScheduleId == sch.Id,
                        JoinType.Left, sch.CourseId == c.Id,
                        JoinType.Left, s.OriginalTeacherId == orig.Id,
                        JoinType.Left, s.SubstituteTeacherId == sub.Id))
                .Where((s, sch, c, orig, sub) => !s.IsDeleted && !orig.IsDeleted && !sub.IsDeleted)
                .WhereIF(isSubstituteView, (s, sch, c, orig, sub) =>
                    s.SubstituteTeacherId == teacherId && s.Status == SwapStatus.Pending)
                .WhereIF(!isSubstituteView, (s, sch, c, orig, sub) =>
                    s.OriginalTeacherId == teacherId)
                .OrderBy((s, sch, c, orig, sub) => s.CreateTime, OrderByType.Desc)
                .Select((s, sch, c, orig, sub) => new SwapRequestResponseDto
                {
                    Id = s.Id,
                    ScheduleId = s.ScheduleId,
                    CourseId = sch.CourseId,
                    CourseName = c.Name,
                    OriginalTeacherId = s.OriginalTeacherId,
                    OriginalTeacherName = orig.Name,
                    SubstituteTeacherId = s.SubstituteTeacherId,
                    SubstituteTeacherName = sub.Name,
                    StartWeek = s.StartWeek,
                    EndWeek = s.EndWeek,
                    Reason = s.Reason,
                    Status = s.Status.ToString(),
                    SubstituteRemark = s.SubstituteRemark,
                    ConfirmedTime = s.ConfirmedTime,
                    CreateTime = s.CreateTime
                }).ToListAsync(ct);

            return Results.Ok(ApiResponse<List<SwapRequestResponseDto>>.Success(rows));
        }

        /// <summary>
        /// 代课教师确认或拒绝调换课申请：接受则更新申请状态并创建代课覆盖层记录；
        /// 拒绝则仅更新申请状态。接受操作在事务中执行以保证申请与覆盖层记录的原子性
        /// </summary>
        /// <param name="swapId">调换课申请 Id</param>
        /// <param name="request">确认请求 DTO</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="logger">日志器</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>确认结果</returns>
        public static async Task<IResult> ConfirmAsync(
            long swapId,
            ConfirmSwapRequestDto request,
            IDbContext dbContext,
            ICurrentUser currentUser,
            ILogger<SwapHandlers> logger,
            CancellationToken ct)
        {
            var teacherId = currentUser.UserId;
            if (string.IsNullOrEmpty(teacherId))
            {
                return Results.Ok(ApiResponse<object>.Fail(Msg.Common.NoPermission, 401));
            }

            var db = dbContext.Client;

            // 校验：调换课申请存在
            var swap = await db.Queryable<CourseSwapRequest>()
                .FirstAsync(s => s.Id == swapId && !s.IsDeleted, ct);
            if (swap is null)
            {
                return Results.Ok(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"调换课申请 {swapId}"), 404));
            }

            // 校验：当前用户必须为该申请的代课教师
            if (swap.SubstituteTeacherId != teacherId)
            {
                return Results.Ok(ApiResponse<object>.Fail("仅代课教师可确认调换课申请", 403));
            }

            // 校验：申请状态必须为 Pending，避免重复确认
            if (swap.Status != SwapStatus.Pending)
            {
                return Results.Ok(ApiResponse<object>.Fail("该调换课申请已处理，无法重复操作", 400));
            }

            var now = DateTime.UtcNow;
            swap.SubstituteRemark = request.SubstituteRemark;
            swap.UpdateTime = now;

            if (request.Accepted)
            {
                // 接受：更新申请状态为 Accepted 并创建代课覆盖层记录，事务保证原子性
                swap.Status = SwapStatus.Accepted;
                swap.ConfirmedTime = now;

                var overrideRecord = new CourseScheduleOverride
                {
                    ScheduleId = swap.ScheduleId,
                    SubstituteTeacherId = swap.SubstituteTeacherId,
                    StartWeek = swap.StartWeek,
                    EndWeek = swap.EndWeek,
                    SwapRequestId = swap.Id,
                    CreateTime = now
                };

                // 使用事务包装申请状态更新与覆盖层创建，任一失败则整体回滚
                await db.Ado.UseTranAsync(async () =>
                {
                    await db.Updateable(swap)
                        .UpdateColumns(s => new { s.Status, s.SubstituteRemark, s.ConfirmedTime, s.UpdateTime })
                        .ExecuteCommandAsync(ct);
                    await db.Insertable(overrideRecord).ExecuteReturnIdentityAsync(ct);
                });

                logger.LogInformation("代课教师 {TeacherId} 接受调换课申请 {SwapId}，已创建覆盖层（排课 {ScheduleId}）",
                    teacherId, swapId, swap.ScheduleId);
                return Results.Ok(ApiResponse<object>.Success("已接受调换课申请"));
            }
            else
            {
                // 拒绝：仅更新申请状态为 Rejected
                swap.Status = SwapStatus.Rejected;
                await db.Updateable(swap)
                    .UpdateColumns(s => new { s.Status, s.SubstituteRemark, s.UpdateTime })
                    .ExecuteCommandAsync(ct);

                logger.LogInformation("代课教师 {TeacherId} 拒绝调换课申请 {SwapId}", teacherId, swapId);
                return Results.Ok(ApiResponse<object>.Success("已拒绝调换课申请"));
            }
        }

        /// <summary>
        /// 原任课教师撤销调换课申请：仅 Pending 状态可撤销，已生效/已拒绝的不可撤销
        /// </summary>
        /// <param name="swapId">调换课申请 Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="currentUser">当前登录用户上下文</param>
        /// <param name="logger">日志器</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>撤销结果</returns>
        public static async Task<IResult> CancelAsync(
            long swapId,
            IDbContext dbContext,
            ICurrentUser currentUser,
            ILogger<SwapHandlers> logger,
            CancellationToken ct)
        {
            var teacherId = currentUser.UserId;
            if (string.IsNullOrEmpty(teacherId))
            {
                return Results.Ok(ApiResponse<object>.Fail(Msg.Common.NoPermission, 401));
            }

            var db = dbContext.Client;

            // 校验：调换课申请存在
            var swap = await db.Queryable<CourseSwapRequest>()
                .FirstAsync(s => s.Id == swapId && !s.IsDeleted, ct);
            if (swap is null)
            {
                return Results.Ok(ApiResponse<object>.Fail(Msg.Common.EntityNotFound($"调换课申请 {swapId}"), 404));
            }

            // 校验：当前用户必须为该申请的发起人（原任课教师）
            if (swap.OriginalTeacherId != teacherId)
            {
                return Results.Ok(ApiResponse<object>.Fail("仅发起人可撤销调换课申请", 403));
            }

            // 校验：仅 Pending 状态可撤销，已生效/已拒绝/已撤销的不可重复操作
            if (swap.Status != SwapStatus.Pending)
            {
                return Results.Ok(ApiResponse<object>.Fail("该调换课申请已处理，无法撤销", 400));
            }

            swap.Status = SwapStatus.Cancelled;
            swap.UpdateTime = DateTime.UtcNow;
            await db.Updateable(swap)
                .UpdateColumns(s => new { s.Status, s.UpdateTime })
                .ExecuteCommandAsync(ct);

            logger.LogInformation("教师 {TeacherId} 撤销调换课申请 {SwapId}", teacherId, swapId);
            return Results.Ok(ApiResponse<object>.Success("已撤销调换课申请"));
        }

        /// <summary>
        /// 根据 Id 查询调换课申请详情并构造响应 DTO（含课程与教师冗余展示字段）
        /// </summary>
        /// <param name="db">SqlSugar 客户端</param>
        /// <param name="id">调换课申请 Id</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应 DTO，未找到返回 null</returns>
        private static async Task<SwapRequestResponseDto?> GetSwapByIdAsync(ISqlSugarClient db, long id, CancellationToken ct)
        {
            var dto = await db.Queryable<CourseSwapRequest, CourseSchedule, Course, Teacher, Teacher>((s, sch, c, orig, sub) =>
                    new JoinQueryInfos(
                        JoinType.Left, s.ScheduleId == sch.Id,
                        JoinType.Left, sch.CourseId == c.Id,
                        JoinType.Left, s.OriginalTeacherId == orig.Id,
                        JoinType.Left, s.SubstituteTeacherId == sub.Id))
                .Where((s, sch, c, orig, sub) => s.Id == id && !s.IsDeleted)
                .Select((s, sch, c, orig, sub) => new SwapRequestResponseDto
                {
                    Id = s.Id,
                    ScheduleId = s.ScheduleId,
                    CourseId = sch.CourseId,
                    CourseName = c.Name,
                    OriginalTeacherId = s.OriginalTeacherId,
                    OriginalTeacherName = orig.Name,
                    SubstituteTeacherId = s.SubstituteTeacherId,
                    SubstituteTeacherName = sub.Name,
                    StartWeek = s.StartWeek,
                    EndWeek = s.EndWeek,
                    Reason = s.Reason,
                    Status = s.Status.ToString(),
                    SubstituteRemark = s.SubstituteRemark,
                    ConfirmedTime = s.ConfirmedTime,
                    CreateTime = s.CreateTime
                }).FirstAsync(ct);

            return dto;
        }

        /// <summary>
        /// 解析排课记录关联的全部班级 Id，兼容旧字段 ClassId（单班）与新字段 ClassIds（合班）
        /// 与 ScheduleConflictService.GetScheduleClassIds 逻辑保持一致
        /// </summary>
        /// <param name="schedule">排课记录</param>
        /// <returns>班级 Id 列表</returns>
        private static List<long> ParseScheduleClassIds(CourseSchedule schedule)
        {
            var ids = new List<long>();

            // 兼容旧数据：ClassId 单班场景
            if (schedule.ClassId > 0)
            {
                ids.Add(schedule.ClassId);
            }

            // 新数据：ClassIds 合班场景，逗号分隔
            if (!string.IsNullOrWhiteSpace(schedule.ClassIds))
            {
                foreach (var idText in schedule.ClassIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (long.TryParse(idText.Trim(), out var id) && id > 0 && !ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }
    }
}
