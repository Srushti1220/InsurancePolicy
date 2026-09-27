using System;
using System.ComponentModel.DataAnnotations;

namespace InsuranceAPI.DTOs
{
    public class PolicyTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CoverageAmount { get; set; }
        public decimal BasePremium { get; set; }
        public int DurationInYears { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class CreatePolicyTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage amount must be greater than zero.")]
        public decimal CoverageAmount { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Base premium must be greater than zero.")]
        public decimal BasePremium { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Duration must be between 1 and 100 years.")]
        public int DurationInYears { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdatePolicyTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage amount must be greater than zero.")]
        public decimal CoverageAmount { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Base premium must be greater than zero.")]
        public decimal BasePremium { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Duration must be between 1 and 100 years.")]
        public int DurationInYears { get; set; }

        public bool IsActive { get; set; }
    }
}
