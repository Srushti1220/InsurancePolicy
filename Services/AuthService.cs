using AutoMapper;
using InsuranceAPI.DTOs;
using InsuranceAPI.Helpers;
using InsuranceAPI.Models;
using InsuranceAPI.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, IMapper mapper)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email?.Trim() ?? string.Empty);
            if (user == null) return null;

            // Verify password using PasswordHasher
            if (!PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            // Generate JWT and Refresh Token
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token to user
            user.RefreshToken = refreshToken;
            int durationMinutes = _configuration.GetValue<int>("Jwt:DurationInMinutes");
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(durationMinutes * 2); // refresh token lasts longer
            await _userRepository.UpdateAsync(user);

            var userDto = _mapper.Map<UserDto>(user);

            return new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = userDto
            };
        }

        public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto tokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(tokenDto.Token);
            if (principal == null) return null;

            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            if (emailClaim == null) return null;

            var user = await _userRepository.GetByEmailAsync(emailClaim.Value);
            if (user == null || user.RefreshToken != tokenDto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            var newJwtToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userRepository.UpdateAsync(user);

            return new LoginResponseDto
            {
                Token = newJwtToken,
                RefreshToken = newRefreshToken,
                User = _mapper.Map<UserDto>(user)
            };
        }

        private string GenerateJwtToken(User user)
        {
            var keyStr = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role),
                new System.Security.Claims.Claim(ClaimTypes.Name, user.FullName)
            };

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutes = _configuration.GetValue<int>("Jwt:DurationInMinutes");

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var keyStr = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // during refresh we just want to extract identity
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr)),
                ValidateLifetime = false // we want to get claims from an expired token
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
