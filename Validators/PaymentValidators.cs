using FluentValidation;
using InsuranceAPI.DTOs;

namespace InsuranceAPI.Validators
{
    public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentDtoValidator()
        {
            RuleFor(x => x.PolicyId)
                .GreaterThan(0)
                .WithMessage("PolicyId must be greater than zero.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than zero.");

            RuleFor(x => x.DueDate)
                .NotEmpty()
                .WithMessage("DueDate is required.");
        }
    }

    public class PayPremiumDtoValidator : AbstractValidator<PayPremiumDto>
    {
        public PayPremiumDtoValidator()
        {
            RuleFor(x => x.TransactionReference)
                .NotEmpty()
                .WithMessage("Transaction reference is required.")
                .MaximumLength(100)
                .WithMessage("Transaction reference cannot exceed 100 characters.");
        }
    }
}
