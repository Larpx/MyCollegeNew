using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Profile.ChangePassword
{
    /// <summary>
    /// 修改密码请求校验器
    /// </summary>
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        /// <summary>
        /// 构造函数，定义校验规则
        /// </summary>
        public ChangePasswordValidator()
        {
            RuleFor(x => x.Dto.OldPassword)
                .NotEmpty().WithMessage("旧密码不能为空");

            RuleFor(x => x.Dto.NewPassword)
                .NotEmpty().WithMessage("新密码不能为空")
                .MinimumLength(6).WithMessage("新密码长度不能少于6位")
                .NotEqual(x => x.Dto.OldPassword).WithMessage("新密码不能与旧密码相同");
        }
    }
}