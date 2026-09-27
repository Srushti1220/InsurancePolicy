using System;
using InsuranceAPI.Models;

namespace InsuranceAPI.Services
{
    public class PremiumCalculator : IPremiumCalculator
    {
        public decimal CalculatePremium(PolicyType policyType, decimal coverageAmount, int durationInYears)
        {
            if (policyType == null)
            {
                throw new ArgumentNullException(nameof(policyType), "Policy type cannot be null.");
            }

            if (coverageAmount <= 0)
            {
                throw new ArgumentException("Coverage amount must be greater than zero.", nameof(coverageAmount));
            }

            if (durationInYears <= 0)
            {
                durationInYears = policyType.DurationInYears > 0 ? policyType.DurationInYears : 1;
            }

            // Calculation based on:
            // 1. PolicyType: BasePremium
            // 2. CoverageAmount: 1% risk factor on coverage
            // 3. Duration: duration scaling factor
            decimal baseAmount = policyType.BasePremium + (coverageAmount * 0.01m);
            int baseDuration = policyType.DurationInYears > 0 ? policyType.DurationInYears : 1;

            decimal calculated = baseAmount * ((decimal)durationInYears / baseDuration);
            return Math.Round(calculated, 2);
        }
    }
}
