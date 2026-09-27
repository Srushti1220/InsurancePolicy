using FluentValidation;
using InsuranceAPI.DTOs;

namespace InsuranceAPI.Validators
{
    public class CreateClaimDtoValidator : AbstractValidator<CreateClaimDto>
    {
        public CreateClaimDtoValidator()
        {
            RuleFor(x => x.PolicyId)
                .GreaterThan(0)
                .WithMessage("PolicyId must be greater than zero.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Claim description is required.")
                .MaximumLength(1000)
                .WithMessage("Claim description cannot exceed 1000 characters.");

            RuleFor(x => x.ClaimAmount)
                .GreaterThan(0)
                .WithMessage("Claim amount must be greater than zero.");
        }
    }

    public class UpdateClaimStatusDtoValidator : AbstractValidator<UpdateClaimStatusDto>
    {
        public UpdateClaimStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(s => s == "Pending" || s == "UnderReview" || s == "Approved" || s == "Rejected" || s == "Settled" || s == "Registered")
                .WithMessage("Status must be Pending, UnderReview, Approved, Rejected, or Settled.");

            RuleFor(x => x.SettlementAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.SettlementAmount.HasValue)
                .WithMessage("Settlement amount must be greater than or equal to zero.");
        }
    }

    public class ApproveClaimDtoValidator : AbstractValidator<ApproveClaimDto>
    {
        public ApproveClaimDtoValidator()
        {
            RuleFor(x => x.SettlementAmount)
                .GreaterThan(0)
                .When(x => x.SettlementAmount.HasValue)
                .WithMessage("Settlement amount must be greater than zero when provided.");
        }
    }
}
