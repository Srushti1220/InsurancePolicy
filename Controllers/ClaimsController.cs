using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models.Enums;
using InsuranceAPI.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsuranceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimService _claimService;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(IClaimService claimService, ILogger<ClaimsController> logger)
        {
            _claimService = claimService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClaimDto>>> GetAll()
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (role == UserRoles.Customer)
            {
                var ownClaims = await _claimService.GetByCustomerIdAsync(userId);
                return Ok(ownClaims);
            }
            else
            {
                var allClaims = await _claimService.GetAllAsync();
                return Ok(allClaims);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClaimDto>> GetById(int id)
        {
            var claim = await _claimService.GetByIdAsync(id);
            if (claim == null)
            {
                return NotFound(new { success = false, message = "Claim not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }

            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (role == UserRoles.Customer && claim.CustomerId != userId)
            {
                _logger.LogWarning("Forbidden: User {UserId} attempted to access Claim {ClaimId} owned by Customer {OwnerId}", 
                    userId, id, claim.CustomerId);
                return StatusCode(403, new { success = false, message = "Access denied: You do not own this claim.", errorCode = "FORBIDDEN", timestamp = DateTime.UtcNow.ToString("o") });
            }

            return Ok(claim);
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<ActionResult<ClaimDto>> Register([FromBody] CreateClaimDto createDto)
        {
            var customerId = GetCurrentUserId();
            _logger.LogInformation("Customer {CustomerId} requested claim creation for Policy {PolicyId} (Amount: {ClaimAmount})", 
                customerId, createDto.PolicyId, createDto.ClaimAmount);

            var created = await _claimService.RegisterClaimAsync(customerId, createDto);
            return StatusCode(201, created);
        }

        [HttpPost("{id}/document")]
        public async Task<IActionResult> UploadDocument(int id, IFormFile file)
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested document upload for Claim {ClaimId} (File: {FileName}, Size: {FileSize} bytes)", 
                userId, id, file?.FileName, file?.Length);

            var documentPath = await _claimService.UploadDocumentAsync(id, userId, role, file!);

            return Ok(new 
            { 
                success = true, 
                message = "Document uploaded successfully.", 
                documentPath = documentPath, 
                statusCode = 200 
            });
        }

        [HttpPost("{id}/under-review")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> MoveToUnderReview(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested status change to UnderReview for Claim {ClaimId}", userId, id);

            var result = await _claimService.MoveToUnderReviewAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Claim not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }

            return Ok(new { success = true, message = "Claim moved to UnderReview status successfully.", statusCode = 200 });
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> ApproveClaim(int id, [FromBody] ApproveClaimDto? approveDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested approval for Claim {ClaimId}", userId, id);

            var result = await _claimService.ApproveClaimAsync(id, approveDto?.SettlementAmount);
            if (!result)
            {
                return NotFound(new { success = false, message = "Claim not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }

            return Ok(new { success = true, message = "Claim approved successfully.", statusCode = 200 });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> RejectClaim(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested rejection for Claim {ClaimId}", userId, id);

            var result = await _claimService.RejectClaimAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Claim not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }

            return Ok(new { success = true, message = "Claim rejected successfully.", statusCode = 200 });
        }

        [HttpPost("{id}/settle")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> SettleClaim(int id, [FromBody] ApproveClaimDto? settleDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested settlement for Claim {ClaimId}", userId, id);

            var result = await _claimService.SettleClaimAsync(id, settleDto?.SettlementAmount);
            if (!result)
            {
                return NotFound(new { success = false, message = "Claim not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }

            return Ok(new { success = true, message = "Claim marked as Settled successfully.", statusCode = 200 });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = UserRoles.AdminOrAgent)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateClaimStatusDto updateDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested status update for Claim {ClaimId} to '{Status}'", userId, id, updateDto.Status);

            var result = await _claimService.UpdateStatusAsync(id, updateDto);
            if (!result)
            {
                return NotFound(new { success = false, message = "Claim not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }

            return Ok(new { success = true, message = $"Claim status updated to '{updateDto.Status}' successfully.", statusCode = 200 });
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
