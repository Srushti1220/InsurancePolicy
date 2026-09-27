using InsuranceAPI.Data;
using InsuranceAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly InsuranceDbContext _context;

        public PolicyRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<Policy?> GetByIdAsync(int id)
        {
            return await _context.Policies
                .Include(p => p.PolicyType)
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Policy>> GetAllAsync()
        {
            return await _context.Policies
                .Include(p => p.PolicyType)
                .Include(p => p.Customer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Policy>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Policies
                .Include(p => p.PolicyType)
                .Where(p => p.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Policy>> GetPendingAsync()
        {
            return await _context.Policies
                .Include(p => p.PolicyType)
                .Include(p => p.Customer)
                .Where(p => p.Status == InsuranceAPI.Models.Enums.PolicyStatus.Pending)
                .ToListAsync();
        }

        public async Task<Policy?> GetActiveOrPendingByCustomerAndTypeAsync(int customerId, int policyTypeId)
        {
            return await _context.Policies
                .Where(p => p.CustomerId == customerId && 
                            p.PolicyTypeId == policyTypeId && 
                            (p.Status == InsuranceAPI.Models.Enums.PolicyStatus.Pending || p.Status == InsuranceAPI.Models.Enums.PolicyStatus.Approved))
                .FirstOrDefaultAsync();
        }

        public async Task<Policy> CreateAsync(Policy policy)
        {
            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();
            return policy;
        }

        public async Task UpdateAsync(Policy policy)
        {
            _context.Policies.Update(policy);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var policy = await _context.Policies.FindAsync(id);
            if (policy != null)
            {
                _context.Policies.Remove(policy);
                await _context.SaveChangesAsync();
            }
        }
    }
}
