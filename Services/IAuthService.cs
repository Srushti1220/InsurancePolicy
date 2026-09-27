using InsuranceAPI.DTOs;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto tokenDto);
    }
}
