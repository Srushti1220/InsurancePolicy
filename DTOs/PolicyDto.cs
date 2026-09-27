using System;
using System.ComponentModel.DataAnnotations;

namespace InsuranceAPI.DTOs
{
    public class PolicyDto
    {
        public int Id { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public int PolicyTypeId { get; set; }
        public string PolicyTypeName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PremiumAmount { get; set; }
        public decimal CoverageAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class ApplyPolicyDto
    {
        [Required]
        public int PolicyTypeId { get; set; }
    }

    public class CreatePolicyDto
    {
        [Required]
        public int PolicyTypeId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PremiumAmount { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal CoverageAmount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";
    }

    public class UpdatePolicyDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class CalculatePremiumRequestDto
    {
        [Required]
        public int PolicyTypeId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage amount must be greater than zero.")]
        public decimal CoverageAmount { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Duration must be between 1 and 100 years.")]
        public int DurationInYears { get; set; }
    }

    public class CalculatePremiumResponseDto
    {
        public int PolicyTypeId { get; set; }
        public string PolicyTypeName { get; set; } = string.Empty;
        public decimal CoverageAmount { get; set; }
        public int DurationInYears { get; set; }
        public decimal BasePremium { get; set; }
        public decimal CalculatedPremium { get; set; }
    }
}
