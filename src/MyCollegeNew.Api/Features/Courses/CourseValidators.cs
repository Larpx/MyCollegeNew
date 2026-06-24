using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Courses;
using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Courses;

/// <summary>创建课程校验器</summary>
public class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    /// <summary>构造函数</summary>
    public CreateCourseValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("课程名称不能为空");
        RuleFor(x => x.Dto.TeacherId).NotEmpty().WithMessage("教师工号不能为空");
        RuleFor(x => x.Dto.Credit).GreaterThan(0).WithMessage("学分必须大于0");
    }
}

/// <summary>更新课程校验器</summary>
public class UpdateCourseValidator : AbstractValidator<UpdateCourseCommand>
{
    /// <summary>构造函数</summary>
    public UpdateCourseValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("课程名称不能为空");
        RuleFor(x => x.Dto.TeacherId).NotEmpty().WithMessage("教师工号不能为空");
        RuleFor(x => x.Dto.Credit).GreaterThan(0).WithMessage("学分必须大于0");
    }
}

/// <summary>创建课表校验器</summary>
public class CreateScheduleValidator : AbstractValidator<CreateScheduleCommand>
{
    /// <summary>构造函数</summary>
    public CreateScheduleValidator()
    {
        RuleFor(x => x.Dto.CourseId).GreaterThan(0).WithMessage("课程ID无效");
        RuleFor(x => x.Dto.ClassId).GreaterThan(0).WithMessage("班级ID无效");
        RuleFor(x => x.Dto.TeacherId).NotEmpty().WithMessage("教师工号不能为空");
        RuleFor(x => x.Dto.DayOfWeek).InclusiveBetween(1, 7).WithMessage("星期必须在1-7之间");
        RuleFor(x => x.Dto.StartSection).GreaterThan(0).WithMessage("开始节次无效");
        RuleFor(x => x.Dto.EndSection).GreaterThanOrEqualTo(x => x.Dto.StartSection).WithMessage("结束节次不能小于开始节次");
        RuleFor(x => x.Dto.StartWeek).GreaterThan(0).WithMessage("开始周次无效");
        RuleFor(x => x.Dto.EndWeek).GreaterThanOrEqualTo(x => x.Dto.StartWeek).WithMessage("结束周次不能小于开始周次");
    }
}

/// <summary>更新课表校验器</summary>
public class UpdateScheduleValidator : AbstractValidator<UpdateScheduleCommand>
{
    /// <summary>构造函数</summary>
    public UpdateScheduleValidator()
    {
        RuleFor(x => x.Dto.CourseId).GreaterThan(0).WithMessage("课程ID无效");
        RuleFor(x => x.Dto.ClassId).GreaterThan(0).WithMessage("班级ID无效");
        RuleFor(x => x.Dto.TeacherId).NotEmpty().WithMessage("教师工号不能为空");
        RuleFor(x => x.Dto.DayOfWeek).InclusiveBetween(1, 7).WithMessage("星期必须在1-7之间");
        RuleFor(x => x.Dto.StartSection).GreaterThan(0).WithMessage("开始节次无效");
        RuleFor(x => x.Dto.EndSection).GreaterThanOrEqualTo(x => x.Dto.StartSection).WithMessage("结束节次不能小于开始节次");
        RuleFor(x => x.Dto.StartWeek).GreaterThan(0).WithMessage("开始周次无效");
        RuleFor(x => x.Dto.EndWeek).GreaterThanOrEqualTo(x => x.Dto.StartWeek).WithMessage("结束周次不能小于开始周次");
    }
}
