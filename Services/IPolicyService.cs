using InsuranceAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public interface IPolicyService
    {
        Task<PolicyDto?> GetByIdAsync(int id);
        Task<IEnumerable<PolicyDto>> GetAllAsync();
        Task<IEnumerable<PolicyDto>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<PolicyDto>> GetPendingAsync();
        Task<PolicyDto> ApplyAsync(int customerId, ApplyPolicyDto applyDto);
        Task<bool> ApproveAsync(int id);
        Task<bool> RejectAsync(int id);
        Task<PolicyDto> CreateAsync(CreatePolicyDto createPolicyDto);
        Task<bool> UpdateAsync(int id, UpdatePolicyDto updatePolicyDto);
        Task<bool> DeleteAsync(int id);
        Task<CalculatePremiumResponseDto> CalculatePremiumAsync(CalculatePremiumRequestDto requestDto);
    }
}
