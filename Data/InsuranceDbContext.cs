using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InsuranceAPI.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Claim = InsuranceAPI.Models.Claim;

namespace InsuranceAPI.Data
{
    public class InsuranceDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public InsuranceDbContext(
            DbContextOptions<InsuranceDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<PolicyType> PolicyTypes { get; set; } = null!;
        public DbSet<Policy> Policies { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Claim> Claims { get; set; } = null!;

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInformation()
        {
            var currentUser = GetCurrentUsername();

            var entries = ChangeTracker.Entries<BaseAuditableEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                    }
                    entry.Entity.CreatedBy ??= currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                }
            }
        }

        private string GetCurrentUsername()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.Email)?.Value 
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? user.Identity.Name 
                    ?? "System";
            }
            return "System";
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Relationships & Cascade Behaviors (using Restrict to avoid multiple cascade paths in SQL Server)
            modelBuilder.Entity<Policy>()
                .HasOne(p => p.Customer)
                .WithMany(u => u.Policies)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Policy>()
                .HasOne(p => p.PolicyType)
                .WithMany()
                .HasForeignKey(p => p.PolicyTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Policy)
                .WithMany(po => po.Payments)
                .HasForeignKey(p => p.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Policy)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Customer)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Enum storage as string in database
            modelBuilder.Entity<Policy>()
                .Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Claim>()
                .Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Seed Data
            var staticDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // Password hash for "Password123"
            string defaultHash = "AIxwOS46v70PpHu8LtlqqZvUnhWXJ/y6Dy5qvrOp1gE=";

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "System Admin",
                    Email = "admin@insurance.com",
                    PasswordHash = defaultHash,
                    Role = "Admin",
                    Phone = "1234567890",
                    CreatedAt = staticDate,
                    CreatedBy = "System"
                },
                new User
                {
                    Id = 2,
                    FullName = "Agent Staff",
                    Email = "staff@insurance.com",
                    PasswordHash = defaultHash,
                    Role = "Staff",
                    Phone = "0987654321",
                    CreatedAt = staticDate,
                    CreatedBy = "System"
                },
                new User
                {
                    Id = 3,
                    FullName = "John Customer",
                    Email = "customer@insurance.com",
                    PasswordHash = defaultHash,
                    Role = "Customer",
                    Phone = "5551234567",
                    CreatedAt = staticDate,
                    CreatedBy = "System"
                }
            );

            modelBuilder.Entity<PolicyType>().HasData(
                new PolicyType
                {
                    Id = 1,
                    Name = "Health Guard Premium",
                    Description = "Comprehensive medical insurance covering hospitalization, surgeries, and medicines.",
                    CoverageAmount = 500000.00m,
                    BasePremium = 5000.00m,
                    DurationInYears = 1,
                    IsActive = true,
                    CreatedAt = staticDate,
                    CreatedBy = "System"
                },
                new PolicyType
                {
                    Id = 2,
                    Name = "Auto Shield Extra",
                    Description = "Full coverage insurance for cars covering accidents, thefts, and third-party liabilities.",
                    CoverageAmount = 300000.00m,
                    BasePremium = 3000.00m,
                    DurationInYears = 1,
                    IsActive = true,
                    CreatedAt = staticDate,
                    CreatedBy = "System"
                },
                new PolicyType
                {
                    Id = 3,
                    Name = "Term Life Secure",
                    Description = "Financial protection for your family. Safe long-term life cover with high death benefits.",
                    CoverageAmount = 1000000.00m,
                    BasePremium = 10000.00m,
                    DurationInYears = 10,
                    IsActive = true,
                    CreatedAt = staticDate,
                    CreatedBy = "System"
                }
            );
        }
    }
}
