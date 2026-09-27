using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InsuranceAPI.Models.Enums;

namespace InsuranceAPI.Models
{
    public class Claim : BaseAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ClaimNumber { get; set; } = string.Empty;

        [Required]
        public int PolicyId { get; set; }

        [ForeignKey("PolicyId")]
        public Policy? Policy { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public User? Customer { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ClaimAmount { get; set; }

        public DateTime ClaimDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [MaxLength(500)]
        public string? DocumentPath { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SettlementAmount { get; set; }

        public DateTime? ProcessedDate { get; set; }
    }
}
