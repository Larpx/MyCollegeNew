using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.SystemUsers
{
    /// <summary>
    /// 创建系统用户校验器
    /// </summary>
    public class CreateSystemUserValidator : AbstractValidator<CreateSystemUserCommand>
    {
        /// <summary>
        /// 构造函数，定义创建系统用户校验规则
        /// </summary>
        public CreateSystemUserValidator()
        {
            RuleFor(x => x.Dto.Username).NotEmpty().WithMessage("用户名不能为空");
            RuleFor(x => x.Dto.Password).NotEmpty().WithMessage("密码不能为空")
                .MinimumLength(6).WithMessage("密码长度不能少于6位");
            RuleFor(x => x.Dto.RealName).NotEmpty().WithMessage("真实姓名不能为空");
        }
    }

    /// <summary>
    /// 更新系统用户校验器
    /// </summary>
    public class UpdateSystemUserValidator : AbstractValidator<UpdateSystemUserCommand>
    {
        /// <summary>
        /// 构造函数，定义更新系统用户校验规则
        /// </summary>
        public UpdateSystemUserValidator()
        {
            RuleFor(x => x.Dto.RealName).NotEmpty().WithMessage("真实姓名不能为空");
        }
    }

    /// <summary>
    /// 重置系统用户密码校验器
    /// </summary>
    public class ResetSystemUserPasswordValidator : AbstractValidator<ResetSystemUserPasswordCommand>
    {
        /// <summary>
        /// 构造函数，定义重置密码校验规则
        /// </summary>
        public ResetSystemUserPasswordValidator()
        {
            RuleFor(x => x.Dto.NewPassword).NotEmpty().WithMessage("新密码不能为空")
                .MinimumLength(6).WithMessage("新密码长度不能少于6位");
        }
    }
}
