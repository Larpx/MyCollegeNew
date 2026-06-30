using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Validation
{
    /// <summary>
    /// 密码复杂度校验扩展（L-1 修复）
    /// 策略：最少 8 位，必须包含大写字母、小写字母、数字
    /// </summary>
    public static class PasswordValidationExtensions
    {
        /// <summary>最小密码长度</summary>
        public const int MinPasswordLength = 8;

        /// <summary>
        /// 添加密码复杂度校验规则，统一应用于所有密码输入场景（修改密码、创建用户、重置密码）
        /// </summary>
        /// <typeparam name="T">验证器所属命令类型</typeparam>
        /// <param name="ruleBuilder">FluentValidation 规则构建器</param>
        /// <returns>链式调用的规则构建器</returns>
        public static IRuleBuilder<T, string> ApplyPasswordPolicy<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("密码不能为空")
                .MinimumLength(MinPasswordLength).WithMessage($"密码长度不能少于{MinPasswordLength}位")
                .Matches("[A-Z]").WithMessage("密码必须包含至少一个大写字母")
                .Matches("[a-z]").WithMessage("密码必须包含至少一个小写字母")
                .Matches("[0-9]").WithMessage("密码必须包含至少一个数字");
        }
    }
}
