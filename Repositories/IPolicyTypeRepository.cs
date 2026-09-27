using InsuranceAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public interface IPolicyTypeRepository
    {
        Task<PolicyType?> GetByIdAsync(int id);
        Task<IEnumerable<PolicyType>> GetAllAsync();
        Task<PolicyType> CreateAsync(PolicyType policyType);
        Task UpdateAsync(PolicyType policyType);
        Task DeleteAsync(int id);
    }
}
