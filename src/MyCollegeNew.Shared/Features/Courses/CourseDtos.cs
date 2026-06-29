using System.ComponentModel.DataAnnotations;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses
{
    /// <summary>课程创建 DTO</summary>
    public class CourseCreateDto
    {
        [Required(ErrorMessage = "课程名称不能为空")]
        [StringLength(64, ErrorMessage = "课程名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "任课教师工号不能为空")]
        [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
        public string TeacherId { get; set; } = string.Empty;

        [Range(0.5, 10, ErrorMessage = "学分范围无效（0.5-10）")]
        public decimal Credit { get; set; }

        [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
        public string? Remark { get; set; }
    }

    /// <summary>课程更新 DTO</summary>
    public class CourseUpdateDto
    {
        [Required(ErrorMessage = "课程名称不能为空")]
        [StringLength(64, ErrorMessage = "课程名称长度不能超过 64 个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "任课教师工号不能为空")]
        [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
        public string TeacherId { get; set; } = string.Empty;

        [Range(0.5, 10, ErrorMessage = "学分范围无效（0.5-10）")]
        public decimal Credit { get; set; }

        [StringLength(256, ErrorMessage = "备注长度不能超过 256 个字符")]
        public string? Remark { get; set; }
    }

    /// <summary>课程响应 DTO</summary>
    public class CourseResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public decimal Credit { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>课表创建 DTO</summary>
    public class ScheduleCreateDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
        public long CourseId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
        public long ClassId { get; set; }

        [Required(ErrorMessage = "任课教师工号不能为空")]
        [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
        public string TeacherId { get; set; } = string.Empty;

        [Range(1, 7, ErrorMessage = "星期几范围无效（1-7）")]
        public int DayOfWeek { get; set; }

        [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
        public int StartSection { get; set; }

        [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
        public int EndSection { get; set; }

        [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
        public int StartWeek { get; set; }

        [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
        public int EndWeek { get; set; }

        [Required(ErrorMessage = "教室不能为空")]
        [StringLength(64, ErrorMessage = "教室长度不能超过 64 个字符")]
        public string Classroom { get; set; } = string.Empty;
    }

    /// <summary>课表更新 DTO</summary>
    public class ScheduleUpdateDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "课程 Id 无效")]
        public long CourseId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "班级 Id 无效")]
        public long ClassId { get; set; }

        [Required(ErrorMessage = "任课教师工号不能为空")]
        [StringLength(32, ErrorMessage = "任课教师工号长度不能超过 32 个字符")]
        public string TeacherId { get; set; } = string.Empty;

        [Range(1, 7, ErrorMessage = "星期几范围无效（1-7）")]
        public int DayOfWeek { get; set; }

        [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
        public int StartSection { get; set; }

        [Range(1, 12, ErrorMessage = "节次范围无效（1-12）")]
        public int EndSection { get; set; }

        [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
        public int StartWeek { get; set; }

        [Range(1, 20, ErrorMessage = "周次范围无效（1-20）")]
        public int EndWeek { get; set; }

        [Required(ErrorMessage = "教室不能为空")]
        [StringLength(64, ErrorMessage = "教室长度不能超过 64 个字符")]
        public string Classroom { get; set; } = string.Empty;
    }

    /// <summary>课表响应 DTO</summary>
    public class ScheduleResponseDto
    {
        public long Id { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public long ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public int StartSection { get; set; }
        public int EndSection { get; set; }
        public int StartWeek { get; set; }
        public int EndWeek { get; set; }
        public string Classroom { get; set; } = string.Empty;

        /// <summary>原任课教师工号（CourseSchedule.TeacherId）</summary>
        public string OriginalTeacherId { get; set; } = string.Empty;

        /// <summary>原任课教师姓名</summary>
        public string OriginalTeacherName { get; set; } = string.Empty;

        /// <summary>当前周次实际讲课教师工号（如有覆盖层则为代课教师，否则同原任课）</summary>
        public string EffectiveTeacherId { get; set; } = string.Empty;

        /// <summary>当前周次实际讲课教师姓名</summary>
        public string EffectiveTeacherName { get; set; } = string.Empty;

        /// <summary>是否当前周次被代课覆盖</summary>
        public bool IsSubstituted { get; set; }

        /// <summary>覆盖层信息列表（每个覆盖层对应一个周次范围）</summary>
        public List<ScheduleOverrideDto> Overrides { get; set; } = new();
    }

    /// <summary>课表代课覆盖层 DTO，描述某周次范围内的代课教师</summary>
    public class ScheduleOverrideDto
    {
        /// <summary>代课教师工号</summary>
        public string SubstituteTeacherId { get; set; } = string.Empty;

        /// <summary>代课教师姓名</summary>
        public string SubstituteTeacherName { get; set; } = string.Empty;

        /// <summary>覆盖生效起始周</summary>
        public int StartWeek { get; set; }

        /// <summary>覆盖生效结束周</summary>
        public int EndWeek { get; set; }

        /// <summary>关联调换课申请 Id</summary>
        public long SwapRequestId { get; set; }
    }

    /// <summary>周课表 DTO</summary>
    public class WeeklyScheduleDto
    {
        public int Week { get; set; }
        public Dictionary<int, List<ScheduleResponseDto>> Days { get; set; } = new();
    }
}