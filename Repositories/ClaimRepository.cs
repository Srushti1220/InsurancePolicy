using InsuranceAPI.Data;
using InsuranceAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly InsuranceDbContext _context;

        public ClaimRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<Claim?> GetByIdAsync(int id)
        {
            return await _context.Claims
                .Include(c => c.Policy)
                .ThenInclude(p => p!.PolicyType)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Claim>> GetAllAsync()
        {
            return await _context.Claims
                .Include(c => c.Policy)
                .ThenInclude(p => p!.PolicyType)
                .Include(c => c.Customer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Claim>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Claims
                .Include(c => c.Policy)
                .ThenInclude(p => p!.PolicyType)
                .Where(c => c.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Claim>> GetByPolicyIdAsync(int policyId)
        {
            return await _context.Claims
                .Include(c => c.Policy)
                .ThenInclude(p => p!.PolicyType)
                .Where(c => c.PolicyId == policyId)
                .ToListAsync();
        }

        public async Task<Claim> CreateAsync(Claim claim)
        {
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task UpdateAsync(Claim claim)
        {
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();
        }
    }
}
