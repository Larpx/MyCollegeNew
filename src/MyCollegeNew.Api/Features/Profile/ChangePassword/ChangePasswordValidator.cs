using FluentValidation;
using Larpx.PersonalTools.MyCollegeNew.Api.Validation;

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

            // L-1 修复：应用统一密码复杂度策略（最少 8 位，含大小写字母与数字）
            RuleFor(x => x.Dto.NewPassword)
                .ApplyPasswordPolicy()
                .NotEqual(x => x.Dto.OldPassword).WithMessage("新密码不能与旧密码相同");
        }
    }
}