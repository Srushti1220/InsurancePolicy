using InsuranceAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public interface IPolicyRepository
    {
        Task<Policy?> GetByIdAsync(int id);
        Task<IEnumerable<Policy>> GetAllAsync();
        Task<IEnumerable<Policy>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Policy>> GetPendingAsync();
        Task<Policy?> GetActiveOrPendingByCustomerAndTypeAsync(int customerId, int policyTypeId);
        Task<Policy> CreateAsync(Policy policy);
        Task UpdateAsync(Policy policy);
        Task DeleteAsync(int id);
    }
}
