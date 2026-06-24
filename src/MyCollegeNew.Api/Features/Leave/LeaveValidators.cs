using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Leave;
using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Leave;

/// <summary>创建请假校验器</summary>
public class CreateLeaveValidator : AbstractValidator<CreateLeaveCommand>
{
    /// <summary>构造函数</summary>
    public CreateLeaveValidator()
    {
        RuleFor(x => x.Dto.StartTime).NotEmpty().WithMessage("开始时间不能为空");
        RuleFor(x => x.Dto.EndTime).NotEmpty().WithMessage("结束时间不能为空");
        RuleFor(x => x.Dto.LeaveType).IsInEnum().WithMessage("请假类型无效");
        RuleFor(x => x.Dto.Reason).NotEmpty().WithMessage("请假原因不能为空");
    }
}
