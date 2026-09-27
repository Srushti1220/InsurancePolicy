using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InsuranceAPI.Models.Enums;

namespace InsuranceAPI.Models
{
    public class Policy : BaseAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required]
        public int PolicyTypeId { get; set; }

        [ForeignKey("PolicyTypeId")]
        public PolicyType? PolicyType { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public User? Customer { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PremiumAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CoverageAmount { get; set; }

        [Required]
        public PolicyStatus Status { get; set; } = PolicyStatus.Pending;

        // Navigation properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
