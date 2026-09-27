using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models.Enums;
using InsuranceAPI.Services;
using System;
using System.Threading.Tasks;

namespace InsuranceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var response = await _authService.LoginAsync(loginDto);
            if (response == null)
            {
                return Unauthorized(new 
                { 
                    success = false, 
                    message = "Invalid email or password.", 
                    errorCode = "UNAUTHORIZED", 
                    timestamp = DateTime.UtcNow.ToString("o") 
                });
            }
            return Ok(response);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto tokenDto)
        {
            var response = await _authService.RefreshTokenAsync(tokenDto);
            if (response == null)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = "Invalid access token or refresh token.", 
                    errorCode = "BAD_REQUEST", 
                    timestamp = DateTime.UtcNow.ToString("o") 
                });
            }
            return Ok(response);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createDto)
        {
            // For public registration, restrict the role to Customer
            createDto.Role = UserRoles.Customer;
            var user = await _userService.CreateAsync(createDto);
            return StatusCode(201, user);
        }
    }
}
