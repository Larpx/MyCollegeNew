using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Login
{
    /// <summary>
    /// 登录请求校验器
    /// </summary>
    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        /// <summary>
        /// 构造函数，定义校验规则
        /// </summary>
        public LoginValidator()
        {
            RuleFor(x => x.Request.Username)
                .NotEmpty().WithMessage("用户名不能为空");

            RuleFor(x => x.Request.Password)
                .NotEmpty().WithMessage("密码不能为空");
        }
    }
}