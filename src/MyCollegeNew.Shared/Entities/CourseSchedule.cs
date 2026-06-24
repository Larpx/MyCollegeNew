using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 课表实体，描述某课程在某班级的周次与节次安排
    /// </summary>
    [SugarTable("course_schedule")]
    public class CourseSchedule : EntityBase
    {
        /// <summary>课表主键，自增</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "课表主键")]
        public long Id { get; set; }

        /// <summary>课程 Id</summary>
        [SugarColumn(ColumnDescription = "课程 Id")]
        public long CourseId { get; set; }

        /// <summary>班级 Id</summary>
        [SugarColumn(ColumnDescription = "班级 Id")]
        public long ClassId { get; set; }

        /// <summary>任课教师工号（关联 Teacher.Id）</summary>
        [SugarColumn(Length = 32, ColumnDescription = "任课教师工号")]
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>星期几（1=周一, 7=周日）</summary>
        [SugarColumn(ColumnDescription = "星期几")]
        public int DayOfWeek { get; set; }

        /// <summary>起始节次（如第 1 节）</summary>
        [SugarColumn(ColumnDescription = "起始节次")]
        public int StartSection { get; set; }

        /// <summary>结束节次（如第 2 节）</summary>
        [SugarColumn(ColumnDescription = "结束节次")]
        public int EndSection { get; set; }

        /// <summary>起始周次</summary>
        [SugarColumn(ColumnDescription = "起始周次")]
        public int StartWeek { get; set; }

        /// <summary>结束周次</summary>
        [SugarColumn(ColumnDescription = "结束周次")]
        public int EndWeek { get; set; }

        /// <summary>教室</summary>
        [SugarColumn(Length = 64, ColumnDescription = "教室")]
        public string Classroom { get; set; } = string.Empty;
    }
}