using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InsuranceAPI.Data;
using InsuranceAPI.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class ReportsController : ControllerBase
    {
        private readonly InsuranceDbContext _context;

        public ReportsController(InsuranceDbContext context)
        {
            _context = context;
        }

        [HttpGet("active-policies")]
        public async Task<IActionResult> GetActivePoliciesReport()
        {
            var activePoliciesQuery = _context.Policies
                .AsNoTracking()
                .Include(p => p.PolicyType)
                .Include(p => p.Customer)
                .Where(p => p.Status == PolicyStatus.Approved);

            var count = await activePoliciesQuery.CountAsync();
            var totalPremiumAmount = await activePoliciesQuery.SumAsync(p => p.PremiumAmount);
            var totalCoverageAmount = await activePoliciesQuery.SumAsync(p => p.CoverageAmount);

            var policies = await activePoliciesQuery.Select(p => new
            {
                p.Id,
                p.PolicyNumber,
                PolicyTypeName = p.PolicyType != null ? p.PolicyType.Name : string.Empty,
                CustomerName = p.Customer != null ? p.Customer.FullName : string.Empty,
                p.StartDate,
                p.EndDate,
                p.PremiumAmount,
                p.CoverageAmount,
                Status = p.Status.ToString()
            }).ToListAsync();

            var summary = new
            {
                Count = count,
                TotalPremiumAmount = totalPremiumAmount,
                TotalCoverageAmount = totalCoverageAmount,
                Policies = policies
            };

            return Ok(summary);
        }

        [HttpGet("lapsed-policies")]
        public async Task<IActionResult> GetLapsedPoliciesReport()
        {
            var lapsedPoliciesQuery = _context.Policies
                .AsNoTracking()
                .Include(p => p.PolicyType)
                .Include(p => p.Customer)
                .Where(p => p.Status == PolicyStatus.Rejected);

            var count = await lapsedPoliciesQuery.CountAsync();
            var totalPremiumAmount = await lapsedPoliciesQuery.SumAsync(p => p.PremiumAmount);

            var policies = await lapsedPoliciesQuery.Select(p => new
            {
                p.Id,
                p.PolicyNumber,
                PolicyTypeName = p.PolicyType != null ? p.PolicyType.Name : string.Empty,
                CustomerName = p.Customer != null ? p.Customer.FullName : string.Empty,
                p.StartDate,
                p.EndDate,
                p.PremiumAmount,
                Status = p.Status.ToString()
            }).ToListAsync();

            var summary = new
            {
                Count = count,
                TotalPremiumAmount = totalPremiumAmount,
                Policies = policies
            };

            return Ok(summary);
        }

        [HttpGet("claim-approval-ratio")]
        public async Task<IActionResult> GetClaimApprovalRatioReport()
        {
            var totalApproved = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Settled);
            var totalRejected = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Rejected);
            var totalPending = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.UnderReview);
            var totalAll = await _context.Claims.AsNoTracking().CountAsync();

            int processedTotal = totalApproved + totalRejected;
            decimal approvalRatio = processedTotal > 0 
                ? Math.Round(((decimal)totalApproved / processedTotal) * 100, 2) 
                : 0;

            var result = new
            {
                TotalClaims = totalAll,
                TotalApprovedOrSettled = totalApproved,
                TotalRejected = totalRejected,
                TotalPendingReview = totalPending,
                ProcessedClaimsCount = processedTotal,
                ApprovalRatioPercentage = approvalRatio
            };

            return Ok(result);
        }

        [HttpGet("total-revenue")]
        public async Task<IActionResult> GetTotalRevenueReport()
        {
            var paidPaymentsQuery = _context.Payments
                .AsNoTracking()
                .Include(p => p.Policy)
                .ThenInclude(po => po!.PolicyType)
                .Where(p => p.IsPaid || p.Status == PaymentStatus.Completed);

            var totalRevenue = await paidPaymentsQuery.SumAsync(p => p.Amount);
            var completedCount = await paidPaymentsQuery.CountAsync();

            var revenueByPolicyType = await paidPaymentsQuery
                .GroupBy(p => p.Policy != null && p.Policy.PolicyType != null ? p.Policy.PolicyType.Name : "Unknown")
                .Select(g => new
                {
                    PolicyTypeName = g.Key,
                    Revenue = g.Sum(x => x.Amount),
                    PaymentCount = g.Count()
                })
                .ToListAsync();

            var result = new
            {
                TotalRevenue = totalRevenue,
                CompletedPaymentsCount = completedCount,
                RevenueBreakdownByPolicyType = revenueByPolicyType
            };

            return Ok(result);
        }

        [HttpGet("claim-ratio")]
        public async Task<IActionResult> GetClaimRatioReport()
        {
            var totalSettledAmount = await _context.Claims
                .AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Settled || c.Status == ClaimStatus.Approved)
                .SumAsync(c => c.SettlementAmount ?? 0);

            var totalClaimAmount = await _context.Claims
                .AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Settled || c.Status == ClaimStatus.Approved)
                .SumAsync(c => c.ClaimAmount);

            var totalPremiumPaid = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsPaid || p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);

            decimal claimRatioPercent = 0;
            if (totalPremiumPaid > 0)
            {
                claimRatioPercent = Math.Round((totalSettledAmount / totalPremiumPaid) * 100, 2);
            }

            var summary = new
            {
                TotalPremiumCollected = totalPremiumPaid,
                TotalApprovedClaimAmount = totalClaimAmount,
                TotalSettledClaimAmount = totalSettledAmount,
                ClaimRatioPercentage = claimRatioPercent,
                StatusSummary = new
                {
                    TotalClaims = await _context.Claims.AsNoTracking().CountAsync(),
                    PendingClaims = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.UnderReview),
                    ApprovedClaims = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Approved),
                    RejectedClaims = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Rejected),
                    SettledClaims = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Settled)
                }
            };

            return Ok(summary);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardReport()
        {
            var activePoliciesCount = await _context.Policies.AsNoTracking().CountAsync(p => p.Status == PolicyStatus.Approved);
            var lapsedPoliciesCount = await _context.Policies.AsNoTracking().CountAsync(p => p.Status == PolicyStatus.Rejected);
            var pendingPoliciesCount = await _context.Policies.AsNoTracking().CountAsync(p => p.Status == PolicyStatus.Pending);

            var totalRevenue = await _context.Payments.AsNoTracking()
                .Where(p => p.IsPaid || p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);

            var totalSettledClaims = await _context.Claims.AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Settled)
                .SumAsync(c => c.SettlementAmount ?? 0);

            var approvedClaimsCount = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Settled);
            var rejectedClaimsCount = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Rejected);
            int processedClaims = approvedClaimsCount + rejectedClaimsCount;

            decimal claimApprovalRatio = processedClaims > 0 
                ? Math.Round(((decimal)approvedClaimsCount / processedClaims) * 100, 2) 
                : 0;

            decimal financialClaimRatio = totalRevenue > 0 
                ? Math.Round((totalSettledClaims / totalRevenue) * 100, 2) 
                : 0;

            return Ok(new
            {
                Policies = new
                {
                    Active = activePoliciesCount,
                    Lapsed = lapsedPoliciesCount,
                    Pending = pendingPoliciesCount
                },
                Financials = new
                {
                    TotalRevenue = totalRevenue,
                    TotalSettledClaims = totalSettledClaims,
                    ClaimLossRatioPercentage = financialClaimRatio
                },
                Claims = new
                {
                    ApprovalRatioPercentage = claimApprovalRatio,
                    ApprovedCount = approvedClaimsCount,
                    RejectedCount = rejectedClaimsCount
                }
            });
        }
    }
}
