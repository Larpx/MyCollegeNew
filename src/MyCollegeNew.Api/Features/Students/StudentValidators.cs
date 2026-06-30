using FluentValidation;
using Larpx.PersonalTools.MyCollegeNew.Api.Validation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Students
{
    /// <summary>
    /// 学生相关请求校验器
    /// </summary>
    public class CreateStudentValidator : AbstractValidator<CreateStudentCommand>
    {
        /// <summary>
        /// 构造函数，定义创建学生校验规则
        /// </summary>
        public CreateStudentValidator()
        {
            RuleFor(x => x.Dto.Id).NotEmpty().WithMessage("学号不能为空");
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("姓名不能为空");
            // L-1 修复：应用统一密码复杂度策略
            RuleFor(x => x.Dto.Password).ApplyPasswordPolicy();
            RuleFor(x => x.Dto.Gender).NotEmpty().WithMessage("性别不能为空");
            RuleFor(x => x.Dto.DepartmentId).GreaterThan(0).WithMessage("院系ID无效");
            RuleFor(x => x.Dto.MajorId).GreaterThan(0).WithMessage("专业ID无效");
            RuleFor(x => x.Dto.ClassId).GreaterThan(0).WithMessage("班级ID无效");
            RuleFor(x => x.Dto.Grade).GreaterThan(0).WithMessage("年级无效");
        }
    }

    /// <summary>
    /// 更新学生校验器
    /// </summary>
    public class UpdateStudentValidator : AbstractValidator<UpdateStudentCommand>
    {
        /// <summary>
        /// 构造函数，定义更新学生校验规则
        /// </summary>
        public UpdateStudentValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("姓名不能为空");
            RuleFor(x => x.Dto.Gender).NotEmpty().WithMessage("性别不能为空");
            RuleFor(x => x.Dto.DepartmentId).GreaterThan(0).WithMessage("院系ID无效");
            RuleFor(x => x.Dto.MajorId).GreaterThan(0).WithMessage("专业ID无效");
            RuleFor(x => x.Dto.ClassId).GreaterThan(0).WithMessage("班级ID无效");
            RuleFor(x => x.Dto.Grade).GreaterThan(0).WithMessage("年级无效");
        }
    }
}