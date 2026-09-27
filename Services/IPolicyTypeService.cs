using InsuranceAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public interface IPolicyTypeService
    {
        Task<PolicyTypeDto?> GetByIdAsync(int id);
        Task<IEnumerable<PolicyTypeDto>> GetAllAsync();
        Task<PolicyTypeDto> CreateAsync(CreatePolicyTypeDto createPolicyTypeDto);
        Task<bool> UpdateAsync(int id, UpdatePolicyTypeDto updatePolicyTypeDto);
        Task<bool> DeleteAsync(int id);
    }
}
