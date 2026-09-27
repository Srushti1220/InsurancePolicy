using InsuranceAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public interface IPaymentService
    {
        Task<PaymentDto?> GetByIdAsync(int id);
        Task<IEnumerable<PaymentDto>> GetAllAsync();
        Task<IEnumerable<PaymentDto>> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<PaymentDto>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<PaymentDto>> GetUpcomingDuePaymentsAsync(int? customerId = null);
        Task<int> MarkOverduePaymentsAsync();
        Task<bool> ProcessPaymentAsync(int id, string transactionReference);
    }
}
