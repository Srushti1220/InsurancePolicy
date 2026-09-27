using FluentValidation;
using InsuranceAPI.DTOs;
using System;

namespace InsuranceAPI.Validators
{
    public class ApplyPolicyDtoValidator : AbstractValidator<ApplyPolicyDto>
    {
        public ApplyPolicyDtoValidator()
        {
            RuleFor(x => x.PolicyTypeId)
                .GreaterThan(0)
                .WithMessage("PolicyTypeId must be a valid positive identifier.");
        }
    }

    public class CreatePolicyDtoValidator : AbstractValidator<CreatePolicyDto>
    {
        public CreatePolicyDtoValidator()
        {
            RuleFor(x => x.PolicyTypeId)
                .GreaterThan(0)
                .WithMessage("PolicyTypeId must be greater than zero.");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("CustomerId must be greater than zero.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("StartDate is required.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("EndDate is required.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("EndDate must be after StartDate.");

            RuleFor(x => x.PremiumAmount)
                .GreaterThan(0)
                .WithMessage("PremiumAmount must be greater than zero.");

            RuleFor(x => x.CoverageAmount)
                .GreaterThan(0)
                .WithMessage("CoverageAmount must be greater than zero.");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(s => s == "Pending" || s == "Approved" || s == "Active" || s == "Rejected")
                .WithMessage("Status must be Pending, Approved, Active, or Rejected.");
        }
    }

    public class UpdatePolicyDtoValidator : AbstractValidator<UpdatePolicyDto>
    {
        public UpdatePolicyDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(s => s == "Pending" || s == "Approved" || s == "Active" || s == "Rejected" || s == "Lapsed" || s == "Expired")
                .WithMessage("Status must be a valid policy status.");
        }
    }

    public class CalculatePremiumRequestDtoValidator : AbstractValidator<CalculatePremiumRequestDto>
    {
        public CalculatePremiumRequestDtoValidator()
        {
            RuleFor(x => x.PolicyTypeId)
                .GreaterThan(0)
                .WithMessage("PolicyTypeId must be greater than zero.");

            RuleFor(x => x.CoverageAmount)
                .GreaterThan(0)
                .WithMessage("CoverageAmount must be greater than zero.");

            RuleFor(x => x.DurationInYears)
                .InclusiveBetween(1, 100)
                .WithMessage("Duration must be between 1 and 100 years.");
        }
    }
}
