using InsuranceAPI.Models;

namespace InsuranceAPI.Services
{
    public interface IPremiumCalculator
    {
        decimal CalculatePremium(PolicyType policyType, decimal coverageAmount, int durationInYears);
    }
}
