using FluentValidation;
using InsuranceAPI.DTOs;

namespace InsuranceAPI.Validators
{
    public class CreatePolicyTypeDtoValidator : AbstractValidator<CreatePolicyTypeDto>
    {
        public CreatePolicyTypeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Policy type name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required.")
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x.CoverageAmount)
                .GreaterThan(0)
                .WithMessage("Coverage amount must be greater than zero.");

            RuleFor(x => x.BasePremium)
                .GreaterThan(0)
                .WithMessage("Base premium must be greater than zero.");

            RuleFor(x => x.DurationInYears)
                .InclusiveBetween(1, 100)
                .WithMessage("Duration must be between 1 and 100 years.");
        }
    }

    public class UpdatePolicyTypeDtoValidator : AbstractValidator<UpdatePolicyTypeDto>
    {
        public UpdatePolicyTypeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Policy type name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required.")
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x.CoverageAmount)
                .GreaterThan(0)
                .WithMessage("Coverage amount must be greater than zero.");

            RuleFor(x => x.BasePremium)
                .GreaterThan(0)
                .WithMessage("Base premium must be greater than zero.");

            RuleFor(x => x.DurationInYears)
                .InclusiveBetween(1, 100)
                .WithMessage("Duration must be between 1 and 100 years.");
        }
    }
}
