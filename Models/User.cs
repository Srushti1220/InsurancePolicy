using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InsuranceAPI.Models
{
    public class User : BaseAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty; // "Admin", "Agent", "Staff", "Customer"

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        // Refresh token support
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public ICollection<Policy> Policies { get; set; } = new List<Policy>();
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
