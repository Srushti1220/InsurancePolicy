using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InsuranceAPI.Models.Enums;

namespace InsuranceAPI.Models
{
    public class Payment : BaseAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [ForeignKey("PolicyId")]
        public Policy? Policy { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? PaymentDate { get; set; }

        public bool IsPaid { get; set; } = false;

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;
    }
}
