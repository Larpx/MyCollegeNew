using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers;

/// <summary>
/// 创建教师校验器
/// </summary>
public class CreateTeacherValidator : AbstractValidator<CreateTeacherCommand>
{
    /// <summary>
    /// 构造函数，定义创建教师校验规则
    /// </summary>
    public CreateTeacherValidator()
    {
        RuleFor(x => x.Dto.Id).NotEmpty().WithMessage("工号不能为空");
        RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("姓名不能为空");
        RuleFor(x => x.Dto.Password).NotEmpty().WithMessage("密码不能为空").MinimumLength(6).WithMessage("密码长度不能少于6位");
        RuleFor(x => x.Dto.Gender).NotEmpty().WithMessage("性别不能为空");
        RuleFor(x => x.Dto.DepartmentId).GreaterThan(0).WithMessage("院系ID无效");
    }
}

/// <summary>
/// 更新教师校验器
/// </summary>
public class UpdateTeacherValidator : AbstractValidator<UpdateTeacherCommand>
{
    /// <summary>
    /// 构造函数，定义更新教师校验规则
    /// </summary>
    public UpdateTeacherValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("姓名不能为空");
        RuleFor(x => x.Dto.Gender).NotEmpty().WithMessage("性别不能为空");
        RuleFor(x => x.Dto.DepartmentId).GreaterThan(0).WithMessage("院系ID无效");
    }
}
