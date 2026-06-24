using FluentValidation;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Organization
{
    /// <summary>创建院系校验器</summary>
    public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
    {
        /// <summary>构造函数</summary>
        public CreateDepartmentValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("院系名称不能为空");
        }
    }

    /// <summary>更新院系校验器</summary>
    public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
    {
        /// <summary>构造函数</summary>
        public UpdateDepartmentValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("院系名称不能为空");
        }
    }

    /// <summary>创建专业校验器</summary>
    public class CreateMajorValidator : AbstractValidator<CreateMajorCommand>
    {
        /// <summary>构造函数</summary>
        public CreateMajorValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("专业名称不能为空");
            RuleFor(x => x.Dto.DepartmentId).GreaterThan(0).WithMessage("院系ID无效");
        }
    }

    /// <summary>更新专业校验器</summary>
    public class UpdateMajorValidator : AbstractValidator<UpdateMajorCommand>
    {
        /// <summary>构造函数</summary>
        public UpdateMajorValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("专业名称不能为空");
            RuleFor(x => x.Dto.DepartmentId).GreaterThan(0).WithMessage("院系ID无效");
        }
    }

    /// <summary>创建班级校验器</summary>
    public class CreateClassValidator : AbstractValidator<CreateClassCommand>
    {
        /// <summary>构造函数</summary>
        public CreateClassValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("班级名称不能为空");
            RuleFor(x => x.Dto.MajorId).GreaterThan(0).WithMessage("专业ID无效");
            RuleFor(x => x.Dto.CounselorId).NotEmpty().WithMessage("辅导员不能为空");
            RuleFor(x => x.Dto.Grade).GreaterThan(0).WithMessage("年级无效");
        }
    }

    /// <summary>更新班级校验器</summary>
    public class UpdateClassValidator : AbstractValidator<UpdateClassCommand>
    {
        /// <summary>构造函数</summary>
        public UpdateClassValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().WithMessage("班级名称不能为空");
            RuleFor(x => x.Dto.MajorId).GreaterThan(0).WithMessage("专业ID无效");
            RuleFor(x => x.Dto.CounselorId).NotEmpty().WithMessage("辅导员不能为空");
            RuleFor(x => x.Dto.Grade).GreaterThan(0).WithMessage("年级无效");
        }
    }
}