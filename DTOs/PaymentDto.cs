using System;
using System.ComponentModel.DataAnnotations;

namespace InsuranceAPI.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string PolicyTypeName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty; // "Pending", "Completed", "Paid", "Overdue"
        public string TransactionReference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class CreatePaymentDto
    {
        [Required]
        public int PolicyId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }
    }

    public class PayPremiumDto
    {
        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;
    }
}
