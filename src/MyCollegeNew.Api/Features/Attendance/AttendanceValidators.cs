using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Attendance;
using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Attendance;

/// <summary>创建会话校验器</summary>
public class CreateSessionValidator : AbstractValidator<CreateSessionCommand>
{
    /// <summary>构造函数</summary>
    public CreateSessionValidator()
    {
        RuleFor(x => x.Dto.CourseId).GreaterThan(0).WithMessage("课程ID无效");
        RuleFor(x => x.Dto.ClassId).GreaterThan(0).WithMessage("班级ID无效");
    }
}

/// <summary>签到校验器</summary>
public class CheckInValidator : AbstractValidator<CheckInCommand>
{
    /// <summary>构造函数</summary>
    public CheckInValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("签到令牌不能为空");
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("学生学号不能为空");
    }
}

/// <summary>手动补签校验器</summary>
public class ManualCheckInValidator : AbstractValidator<ManualCheckInCommand>
{
    /// <summary>构造函数</summary>
    public ManualCheckInValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("学生学号不能为空");
    }
}
