using InsuranceAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public interface IClaimRepository
    {
        Task<Claim?> GetByIdAsync(int id);
        Task<IEnumerable<Claim>> GetAllAsync();
        Task<IEnumerable<Claim>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Claim>> GetByPolicyIdAsync(int policyId);
        Task<Claim> CreateAsync(Claim claim);
        Task UpdateAsync(Claim claim);
    }
}
