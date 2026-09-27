using InsuranceAPI.Data;
using InsuranceAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public class PolicyTypeRepository : IPolicyTypeRepository
    {
        private readonly InsuranceDbContext _context;

        public PolicyTypeRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyType?> GetByIdAsync(int id)
        {
            return await _context.PolicyTypes.FindAsync(id);
        }

        public async Task<IEnumerable<PolicyType>> GetAllAsync()
        {
            return await _context.PolicyTypes.ToListAsync();
        }

        public async Task<PolicyType> CreateAsync(PolicyType policyType)
        {
            _context.PolicyTypes.Add(policyType);
            await _context.SaveChangesAsync();
            return policyType;
        }

        public async Task UpdateAsync(PolicyType policyType)
        {
            _context.PolicyTypes.Update(policyType);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var policyType = await GetByIdAsync(id);
            if (policyType != null)
            {
                _context.PolicyTypes.Remove(policyType);
                await _context.SaveChangesAsync();
            }
        }
    }
}
