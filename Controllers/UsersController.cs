using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models.Enums;
using InsuranceAPI.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createDto)
        {
            var user = await _userService.CreateAsync(createDto);
            return StatusCode(201, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto updateDto)
        {
            var result = await _userService.UpdateAsync(id, updateDto);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "User updated successfully.", statusCode = 200 });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _userService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "User deleted successfully.", statusCode = 200 });
        }
    }
}
