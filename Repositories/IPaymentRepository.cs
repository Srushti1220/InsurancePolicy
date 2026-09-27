using InsuranceAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<IEnumerable<Payment>> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Payment>> GetUpcomingDuePaymentsAsync(int? customerId = null);
        Task<IEnumerable<Payment>> GetOverduePaymentsAsync();
        Task<Payment> CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
    }
}
