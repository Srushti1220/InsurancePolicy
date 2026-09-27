using InsuranceAPI.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public interface IClaimService
    {
        Task<ClaimDto?> GetByIdAsync(int id);
        Task<IEnumerable<ClaimDto>> GetAllAsync();
        Task<IEnumerable<ClaimDto>> GetByCustomerIdAsync(int customerId);
        Task<ClaimDto> RegisterClaimAsync(int customerId, CreateClaimDto createClaimDto);
        Task<bool> MoveToUnderReviewAsync(int id);
        Task<bool> ApproveClaimAsync(int id, decimal? settlementAmount);
        Task<bool> RejectClaimAsync(int id);
        Task<bool> SettleClaimAsync(int id, decimal? settlementAmount);
        Task<bool> UpdateStatusAsync(int id, UpdateClaimStatusDto updateClaimStatusDto);
        Task<string> UploadDocumentAsync(int id, int currentUserId, string currentUserRole, IFormFile file);
    }
}
