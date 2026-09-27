using System;
using System.ComponentModel.DataAnnotations;

namespace InsuranceAPI.DTOs
{
    public class ClaimDto
    {
        public int Id { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string PolicyTypeName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ClaimAmount { get; set; }
        public DateTime ClaimDate { get; set; }
        public string Status { get; set; } = string.Empty; // "Pending", "UnderReview", "Approved", "Rejected", "Settled"
        public string DocumentPath { get; set; } = string.Empty;
        public decimal? SettlementAmount { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class CreateClaimDto
    {
        [Required]
        public int PolicyId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be greater than zero.")]
        public decimal ClaimAmount { get; set; }
    }

    public class UpdateClaimStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|UnderReview|Approved|Rejected|Settled|Registered)$", ErrorMessage = "Status must be Pending, UnderReview, Approved, Rejected, or Settled")]
        public string Status { get; set; } = string.Empty;

        [Range(0.00, double.MaxValue, ErrorMessage = "Settlement amount must be positive.")]
        public decimal? SettlementAmount { get; set; }
    }

    public class ApproveClaimDto
    {
        [Range(0.00, double.MaxValue, ErrorMessage = "Settlement amount must be positive.")]
        public decimal? SettlementAmount { get; set; }
    }
}
