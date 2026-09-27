using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceAPI.Models
{
    public class PolicyType : BaseAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CoverageAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePremium { get; set; }

        [Required]
        public int DurationInYears { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
