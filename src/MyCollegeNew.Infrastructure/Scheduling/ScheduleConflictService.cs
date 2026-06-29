using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Scheduling
{
    /// <summary>
    /// 排课冲突校验结果
    /// </summary>
    public class ScheduleConflictResult
    {
        /// <summary>是否存在冲突</summary>
        public bool HasConflict { get; set; }

        /// <summary>冲突类型描述列表</summary>
        public List<string> Conflicts { get; set; } = new();
    }

    /// <summary>
    /// 待校验的排课信息
    /// </summary>
    public class ScheduleValidationInput
    {
        /// <summary>编辑时传入原排课 Id，新建时为 null</summary>
        public long? ScheduleId { get; set; }

        /// <summary>任课教师工号（关联 Teacher.Id）</summary>
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>班级 Id 列表（含合班课的全部班级）</summary>
        public List<long> ClassIds { get; set; } = new();

        /// <summary>星期几（1=周一, 7=周日）</summary>
        public int DayOfWeek { get; set; }

        /// <summary>起始节次</summary>
        public int StartSection { get; set; }

        /// <summary>结束节次</summary>
        public int EndSection { get; set; }

        /// <summary>起始周次</summary>
        public int StartWeek { get; set; }

        /// <summary>结束周次</summary>
        public int EndWeek { get; set; }

        /// <summary>教室</summary>
        public string Classroom { get; set; } = string.Empty;
    }

    /// <summary>
    /// 排课冲突校验服务，用于系主任/教师在排课或调换课时校验四项约束
    /// </summary>
    public interface IScheduleConflictService
    {
        /// <summary>
        /// 校验排课冲突，依次检查教师时段、班级时段、教室占用三项约束，
        /// 并将代课覆盖层计入实际任课教师（避免对原教师误报、对代课教师漏报）
        /// </summary>
        /// <param name="input">待校验的排课信息</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>冲突校验结果</returns>
        Task<ScheduleConflictResult> ValidateAsync(ScheduleValidationInput input, CancellationToken ct = default);
    }

    /// <summary>
    /// 排课冲突校验服务实现，基于 SqlSugar 查询同期排课与代课覆盖层进行四项约束校验
    /// </summary>
    public class ScheduleConflictService : IScheduleConflictService
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<ScheduleConflictService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志器</param>
        public ScheduleConflictService(IDbContext dbContext, ILogger<ScheduleConflictService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 校验排课冲突，依次检查教师时段、班级时段、教室占用三项约束
        /// </summary>
        /// <param name="input">待校验的排课信息</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>冲突校验结果</returns>
        public async Task<ScheduleConflictResult> ValidateAsync(ScheduleValidationInput input, CancellationToken ct = default)
        {
            var result = new ScheduleConflictResult();
            var db = _dbContext.Client;

            // 查询同期所有排课：同一星期、周次区间重叠、节次区间重叠、排除软删除与自身
            var scheduleQuery = db.Queryable<CourseSchedule>()
                .Where(s => !s.IsDeleted)
                .Where(s => s.DayOfWeek == input.DayOfWeek)
                .Where(s => s.StartWeek <= input.EndWeek && s.EndWeek >= input.StartWeek)
                .Where(s => !(s.StartSection > input.EndSection || s.EndSection < input.StartSection));

            // 编辑场景排除自身记录，避免与自身比较产生假冲突
            if (input.ScheduleId.HasValue)
            {
                var editingId = input.ScheduleId.Value;
                scheduleQuery = scheduleQuery.Where(s => s.Id != editingId);
            }

            var schedules = await scheduleQuery.ToListAsync(ct);
            if (schedules.Count == 0)
            {
                return result;
            }

            // 查询同期生效的代课覆盖层，用于计算实际任课教师
            var scheduleIds = schedules.Select(s => s.Id).ToList();
            var overrides = await db.Queryable<CourseScheduleOverride>()
                .Where(o => !o.IsDeleted)
                .Where(o => scheduleIds.Contains(o.ScheduleId))
                .Where(o => o.StartWeek <= input.EndWeek && o.EndWeek >= input.StartWeek)
                .ToListAsync(ct);

            // 构建实际任课教师映射：默认为原 TeacherId，若存在覆盖层则替换为代课教师
            var effectiveTeacherByScheduleId = schedules.ToDictionary(s => s.Id, s => s.TeacherId);
            foreach (var ov in overrides)
            {
                effectiveTeacherByScheduleId[ov.ScheduleId] = ov.SubstituteTeacherId;
            }

            ValidateTeacherConflict(input, schedules, effectiveTeacherByScheduleId, result);
            ValidateClassConflict(input, schedules, result);
            ValidateClassroomConflict(input, schedules, result);

            if (result.HasConflict)
            {
                _logger.LogInformation(
                    "排课冲突校验未通过：教师 {TeacherId} 周{DayOfWeek} 第{StartSection}-{EndSection} 节 第{StartWeek}-{EndWeek} 周，冲突 {Count} 项",
                    input.TeacherId, input.DayOfWeek, input.StartSection, input.EndSection, input.StartWeek, input.EndWeek, result.Conflicts.Count);
            }

            return result;
        }

        /// <summary>
        /// 校验教师时段冲突（含代课覆盖层）：同一教师在同一周次、星期、节次不能有重叠排课。
        /// 代课覆盖层生效后，原教师视为该时段空闲，代课教师视为占用，避免对双方误判。
        /// </summary>
        /// <param name="input">待校验的排课信息</param>
        /// <param name="schedules">同期重叠排课列表</param>
        /// <param name="effectiveTeacherByScheduleId">排课实际任课教师映射（含覆盖层）</param>
        /// <param name="result">冲突结果累加器</param>
        private static void ValidateTeacherConflict(
            ScheduleValidationInput input,
            List<CourseSchedule> schedules,
            Dictionary<long, string> effectiveTeacherByScheduleId,
            ScheduleConflictResult result)
        {
            var conflict = schedules.FirstOrDefault(s => effectiveTeacherByScheduleId[s.Id] == input.TeacherId);
            if (conflict is null)
            {
                return;
            }

            result.HasConflict = true;
            result.Conflicts.Add(
                $"教师时段冲突：教师 {input.TeacherId} 在第 {input.StartWeek}-{input.EndWeek} 周 周{input.DayOfWeek} 第{input.StartSection}-{input.EndSection} 节已有排课");
        }

        /// <summary>
        /// 校验班级时段冲突：同一班级（含合班课任一班级）在同一周次、星期、节次不能有重叠排课
        /// </summary>
        /// <param name="input">待校验的排课信息</param>
        /// <param name="schedules">同期重叠排课列表</param>
        /// <param name="result">冲突结果累加器</param>
        private static void ValidateClassConflict(
            ScheduleValidationInput input,
            List<CourseSchedule> schedules,
            ScheduleConflictResult result)
        {
            if (input.ClassIds.Count == 0)
            {
                return;
            }

            // 收集同期所有排课关联的班级 Id（含合班），用 HashSet 加速包含判断
            var existingClassIds = schedules
                .SelectMany(GetScheduleClassIds)
                .ToHashSet();

            foreach (var classId in input.ClassIds)
            {
                if (existingClassIds.Contains(classId))
                {
                    result.HasConflict = true;
                    result.Conflicts.Add($"班级时段冲突：班级 {classId} 在该时段已有排课");
                    return;
                }
            }
        }

        /// <summary>
        /// 校验教室冲突：同一教室在同一周次、星期、节次不能被多门课占用
        /// </summary>
        /// <param name="input">待校验的排课信息</param>
        /// <param name="schedules">同期重叠排课列表</param>
        /// <param name="result">冲突结果累加器</param>
        private static void ValidateClassroomConflict(
            ScheduleValidationInput input,
            List<CourseSchedule> schedules,
            ScheduleConflictResult result)
        {
            if (string.IsNullOrWhiteSpace(input.Classroom))
            {
                return;
            }

            var conflict = schedules.FirstOrDefault(s => s.Classroom == input.Classroom);
            if (conflict is null)
            {
                return;
            }

            result.HasConflict = true;
            result.Conflicts.Add($"教室冲突：教室 {input.Classroom} 在该时段已被占用");
        }

        /// <summary>
        /// 获取排课记录关联的全部班级 Id，兼容旧字段 ClassId（单班）与新字段 ClassIds（合班）
        /// </summary>
        /// <param name="schedule">排课记录</param>
        /// <returns>班级 Id 集合</returns>
        private static IEnumerable<long> GetScheduleClassIds(CourseSchedule schedule)
        {
            // 兼容旧数据：ClassId 单班场景
            if (schedule.ClassId > 0)
            {
                yield return schedule.ClassId;
            }

            // 新数据：ClassIds 合班场景，逗号分隔
            if (!string.IsNullOrWhiteSpace(schedule.ClassIds))
            {
                foreach (var idText in schedule.ClassIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (long.TryParse(idText, out var id) && id > 0)
                    {
                        yield return id;
                    }
                }
            }
        }
    }
}
