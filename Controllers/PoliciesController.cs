using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models.Enums;
using InsuranceAPI.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsuranceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policyService;
        private readonly ILogger<PoliciesController> _logger;

        public PoliciesController(IPolicyService policyService, ILogger<PoliciesController> logger)
        {
            _policyService = policyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetAll()
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (role == UserRoles.Customer)
            {
                // Customers can only see their own policies
                var ownPolicies = await _policyService.GetByCustomerIdAsync(userId);
                return Ok(ownPolicies);
            }
            else
            {
                // Admin, Agent, and Staff can see all policies
                var allPolicies = await _policyService.GetAllAsync();
                return Ok(allPolicies);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PolicyDto>> GetById(int id)
        {
            var policy = await _policyService.GetByIdAsync(id);
            if (policy == null)
            {
                return NotFound(new { success = false, message = "Policy not found.", errorCode = "NOT_FOUND", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            // Ownership check: Customers can only access their own policies
            if (role == UserRoles.Customer && policy.CustomerId != userId)
            {
                _logger.LogWarning("Forbidden: User {UserId} attempted to access Policy {PolicyId} owned by Customer {OwnerId}", 
                    userId, id, policy.CustomerId);
                return StatusCode(403, new { success = false, message = "Access denied: You do not own this policy.", errorCode = "FORBIDDEN", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            return Ok(policy);
        }

        [HttpPost("apply")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<ActionResult<PolicyDto>> Apply([FromBody] ApplyPolicyDto applyDto)
        {
            var customerId = GetCurrentUserId();
            _logger.LogInformation("Customer {CustomerId} requested policy application for PolicyTypeId {PolicyTypeId}", 
                customerId, applyDto.PolicyTypeId);

            var created = await _policyService.ApplyAsync(customerId, applyDto);
            return StatusCode(201, created);
        }

        [HttpPost("calculate-premium")]
        [Authorize]
        public async Task<ActionResult<CalculatePremiumResponseDto>> CalculatePremium([FromBody] CalculatePremiumRequestDto requestDto)
        {
            var result = await _policyService.CalculatePremiumAsync(requestDto);
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested approval for Policy {PolicyId}", userId, id);

            var result = await _policyService.ApproveAsync(id);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Cannot approve policy. It must exist and be in 'Pending' status.", errorCode = "BAD_REQUEST", timestamp = System.DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "Policy approved successfully.", statusCode = 200 });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested rejection for Policy {PolicyId}", userId, id);

            var result = await _policyService.RejectAsync(id);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Cannot reject policy. It must exist and be in 'Pending' status.", errorCode = "BAD_REQUEST", timestamp = System.DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "Policy rejected successfully.", statusCode = 200 });
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<PolicyDto>> Create([FromBody] CreatePolicyDto createDto)
        {
            var created = await _policyService.CreateAsync(createDto);
            return StatusCode(201, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePolicyDto updateDto)
        {
            var result = await _policyService.UpdateAsync(id, updateDto);
            if (!result)
            {
                return NotFound(new { success = false, message = "Policy not found.", errorCode = "NOT_FOUND", timestamp = System.DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "Policy updated successfully.", statusCode = 200 });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _policyService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Policy not found.", errorCode = "NOT_FOUND", timestamp = System.DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "Policy deleted successfully.", statusCode = 200 });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private string GetCurrentUserRole()
        {
            var claim = User.FindFirst(ClaimTypes.Role);
            return claim != null ? claim.Value : string.Empty;
        }
    }
}
