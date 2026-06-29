using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses
{
    /// <summary>
    /// 接课申请请求 DTO，由教师提交，用于主动接系主任发布的课程模板
    /// </summary>
    public class CreateAssignmentRequestDto
    {
        /// <summary>课程模板 Id（关联 Course.Id）</summary>
        [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
        public long CourseId { get; set; }

        /// <summary>任课教师工号（由端点从当前登录用户填充，前端传递值将被忽略以防越权）</summary>
        [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>合班班级 Id 列表（不可为空，至少包含一个班级）</summary>
        [Required(ErrorMessage = "班级列表不能为空")]
        [MinLength(1, ErrorMessage = "至少选择一个班级")]
        public List<long> ClassIds { get; set; } = new();

        /// <summary>学期标识（如 "2026-Spring"）</summary>
        [Required(ErrorMessage = "学期标识不能为空")]
        [StringLength(16, ErrorMessage = "学期标识长度不能超过 16 个字符")]
        public string Semester { get; set; } = string.Empty;

        /// <summary>接课申请理由（可选）</summary>
        [StringLength(256, ErrorMessage = "申请理由长度不能超过 256 个字符")]
        public string? ApplyReason { get; set; }
    }

    /// <summary>
    /// 接课申请响应 DTO，含课程、教师、班级等冗余展示字段
    /// </summary>
    public class AssignmentResponseDto
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

        /// <summary>合班班级 Id 列表</summary>
        public List<long> ClassIds { get; set; } = new();

        /// <summary>合班班级名称列表（与 ClassIds 一一对应）</summary>
        public List<string> ClassNames { get; set; } = new();

        /// <summary>学期标识</summary>
        public string Semester { get; set; } = string.Empty;

        /// <summary>接课状态（Pending / Active / Withdrawn）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>接课申请理由</summary>
        public string? ApplyReason { get; set; }

        /// <summary>系主任审批备注</summary>
        public string? ReviewRemark { get; set; }

        /// <summary>创建时间（UTC）</summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 审批请求 DTO，由系主任提交，对接课申请进行通过或驳回
    /// </summary>
    public class ReviewAssignmentRequestDto
    {
        /// <summary>是否审批通过（true 通过，false 驳回）</summary>
        public bool Approved { get; set; }

        /// <summary>审批备注（可选）</summary>
        [StringLength(256, ErrorMessage = "审批备注长度不能超过 256 个字符")]
        public string? ReviewRemark { get; set; }
    }
}
