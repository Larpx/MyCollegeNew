using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses
{
    /// <summary>
    /// 调换课申请请求 DTO，由原任课教师提交，委托代课教师在指定周次范围内代课
    /// </summary>
    public class CreateSwapRequestDto
    {
        /// <summary>原排课 Id（关联 CourseSchedule.Id）</summary>
        [Range(1, long.MaxValue, ErrorMessage = "排课 Id 无效")]
        public long ScheduleId { get; set; }

        /// <summary>代课教师工号（关联 Teacher.Id）</summary>
        [Required(ErrorMessage = "代课教师工号不能为空")]
        [StringLength(32, ErrorMessage = "代课教师工号长度不能超过 32 个字符")]
        public string SubstituteTeacherId { get; set; } = string.Empty;

        /// <summary>代课起始周次</summary>
        [Range(1, 50, ErrorMessage = "起始周次必须在 1-50 之间")]
        public int StartWeek { get; set; }

        /// <summary>代课结束周次</summary>
        [Range(1, 50, ErrorMessage = "结束周次必须在 1-50 之间")]
        public int EndWeek { get; set; }

        /// <summary>调换原因（可选）</summary>
        [StringLength(256, ErrorMessage = "调换原因长度不能超过 256 个字符")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 调换课申请响应 DTO，含课程、原任课教师、代课教师等冗余展示字段
    /// </summary>
    public class SwapRequestResponseDto
    {
        /// <summary>调换课申请主键</summary>
        public long Id { get; set; }

        /// <summary>原排课 Id</summary>
        public long ScheduleId { get; set; }

        /// <summary>课程 Id</summary>
        public long CourseId { get; set; }

        /// <summary>课程名称</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>原任课教师工号</summary>
        public string OriginalTeacherId { get; set; } = string.Empty;

        /// <summary>原任课教师姓名</summary>
        public string OriginalTeacherName { get; set; } = string.Empty;

        /// <summary>代课教师工号</summary>
        public string SubstituteTeacherId { get; set; } = string.Empty;

        /// <summary>代课教师姓名</summary>
        public string SubstituteTeacherName { get; set; } = string.Empty;

        /// <summary>代课起始周次</summary>
        public int StartWeek { get; set; }

        /// <summary>代课结束周次</summary>
        public int EndWeek { get; set; }

        /// <summary>调换原因</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>调换课状态（Pending / Accepted / Rejected / Cancelled）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>代课人确认备注</summary>
        public string? SubstituteRemark { get; set; }

        /// <summary>代课人确认时间（UTC），未确认时为空</summary>
        public DateTime? ConfirmedTime { get; set; }

        /// <summary>创建时间（UTC）</summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 代课人确认请求 DTO，代课教师对接收到的调换课申请进行接受或拒绝
    /// </summary>
    public class ConfirmSwapRequestDto
    {
        /// <summary>是否接受（true 接受并生效，false 拒绝）</summary>
        public bool Accepted { get; set; }

        /// <summary>代课人确认备注（可选）</summary>
        [StringLength(256, ErrorMessage = "确认备注长度不能超过 256 个字符")]
        public string? SubstituteRemark { get; set; }
    }
}
