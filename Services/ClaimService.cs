using AutoMapper;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models;
using InsuranceAPI.Models.Enums;
using InsuranceAPI.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public class ClaimService : IClaimService
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IMapper _mapper;
        private readonly ILogger<ClaimService> _logger;

        public ClaimService(
            IClaimRepository claimRepository,
            IPolicyRepository policyRepository,
            IWebHostEnvironment environment,
            IMapper mapper,
            ILogger<ClaimService> logger)
        {
            _claimRepository = claimRepository;
            _policyRepository = policyRepository;
            _environment = environment;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ClaimDto?> GetByIdAsync(int id)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            return claim == null ? null : _mapper.Map<ClaimDto>(claim);
        }

        public async Task<IEnumerable<ClaimDto>> GetAllAsync()
        {
            var claims = await _claimRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ClaimDto>>(claims);
        }

        public async Task<IEnumerable<ClaimDto>> GetByCustomerIdAsync(int customerId)
        {
            var claims = await _claimRepository.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<ClaimDto>>(claims);
        }

        public async Task<ClaimDto> RegisterClaimAsync(int customerId, CreateClaimDto createClaimDto)
        {
            var policy = await _policyRepository.GetByIdAsync(createClaimDto.PolicyId);
            if (policy == null)
            {
                _logger.LogWarning("Claim registration failed: Policy {PolicyId} does not exist", createClaimDto.PolicyId);
                throw new InvalidOperationException("Selected policy does not exist.");
            }

            // Verify policy ownership
            if (policy.CustomerId != customerId)
            {
                _logger.LogWarning("Claim registration unauthorized: Customer {CustomerId} attempted to file claim on Policy {PolicyId} owned by Customer {OwnerId}", 
                    customerId, policy.Id, policy.CustomerId);
                throw new InvalidOperationException("You can only register claims for your own policies.");
            }

            // Verify policy status (Cannot claim on inactive/pending/rejected policies)
            if (policy.Status != PolicyStatus.Approved)
            {
                _logger.LogWarning("Claim registration rejected: Policy {PolicyId} is in '{Status}' status, must be Approved", 
                    policy.Id, policy.Status);
                throw new InvalidOperationException($"Cannot file a claim on a policy that is in '{policy.Status}' status. Policy must be Approved.");
            }

            // Verify claim amount is within limits
            if (createClaimDto.ClaimAmount > policy.CoverageAmount)
            {
                _logger.LogWarning("Claim registration rejected: Claim amount {ClaimAmount} exceeds Policy {PolicyId} coverage limit {CoverageAmount}", 
                    createClaimDto.ClaimAmount, policy.Id, policy.CoverageAmount);
                throw new InvalidOperationException($"Claim amount cannot exceed the policy coverage limit of {policy.CoverageAmount:C}.");
            }

            var claim = new Claim
            {
                ClaimNumber = $"CLM-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                PolicyId = policy.Id,
                CustomerId = customerId,
                Description = createClaimDto.Description,
                ClaimAmount = createClaimDto.ClaimAmount,
                ClaimDate = DateTime.UtcNow,
                Status = ClaimStatus.Pending,
                DocumentPath = null,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _claimRepository.CreateAsync(claim);

            _logger.LogInformation("Claim {ClaimId} ({ClaimNumber}) registered successfully. PolicyId: {PolicyId}, CustomerId: {CustomerId}, Amount: {ClaimAmount}", 
                created.Id, created.ClaimNumber, created.PolicyId, customerId, created.ClaimAmount);
            
            // Eager load details for response mapping
            var loadedClaim = await _claimRepository.GetByIdAsync(created.Id);
            return _mapper.Map<ClaimDto>(loadedClaim);
        }

        public async Task<bool> MoveToUnderReviewAsync(int id)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                _logger.LogWarning("Move to UnderReview failed: Claim {ClaimId} not found", id);
                return false;
            }

            if (claim.Status == ClaimStatus.Settled)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Cannot modify a claim that has already been Settled", id);
                throw new InvalidOperationException("Cannot modify a claim that has already been Settled.");
            }
            if (claim.Status == ClaimStatus.Rejected)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Cannot modify a claim that has already been Rejected", id);
                throw new InvalidOperationException("Cannot modify a claim that has already been Rejected.");
            }

            claim.Status = ClaimStatus.UnderReview;
            claim.ProcessedDate = DateTime.UtcNow;
            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation("Claim {ClaimId} ({ClaimNumber}) moved to UnderReview status", claim.Id, claim.ClaimNumber);
            return true;
        }

        public async Task<bool> ApproveClaimAsync(int id, decimal? settlementAmount)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                _logger.LogWarning("Claim approval failed: Claim {ClaimId} not found", id);
                return false;
            }

            if (claim.Status == ClaimStatus.Settled)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Cannot modify a claim that has already been Settled", id);
                throw new InvalidOperationException("Cannot modify a claim that has already been Settled.");
            }
            if (claim.Status == ClaimStatus.Rejected)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Cannot approve a claim that has been Rejected", id);
                throw new InvalidOperationException("Cannot approve a claim that has been Rejected.");
            }

            decimal finalSettlement = settlementAmount ?? claim.ClaimAmount;
            if (finalSettlement > claim.ClaimAmount)
            {
                _logger.LogWarning("Claim approval rejected for Claim {ClaimId}: Settlement amount {SettlementAmount} exceeds requested claim amount {ClaimAmount}", 
                    id, finalSettlement, claim.ClaimAmount);
                throw new InvalidOperationException("Settlement amount cannot exceed the requested claim amount.");
            }

            claim.Status = ClaimStatus.Approved;
            claim.SettlementAmount = finalSettlement;
            claim.ProcessedDate = DateTime.UtcNow;
            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation("Claim {ClaimId} ({ClaimNumber}) approved with settlement amount {SettlementAmount}", 
                claim.Id, claim.ClaimNumber, finalSettlement);
            return true;
        }

        public async Task<bool> RejectClaimAsync(int id)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                _logger.LogWarning("Claim rejection failed: Claim {ClaimId} not found", id);
                return false;
            }

            if (claim.Status == ClaimStatus.Settled)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Cannot reject a claim that has already been Settled", id);
                throw new InvalidOperationException("Cannot reject a claim that has already been Settled.");
            }

            claim.Status = ClaimStatus.Rejected;
            claim.ProcessedDate = DateTime.UtcNow;
            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation("Claim {ClaimId} ({ClaimNumber}) rejected successfully", claim.Id, claim.ClaimNumber);
            return true;
        }

        public async Task<bool> SettleClaimAsync(int id, decimal? settlementAmount)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                _logger.LogWarning("Claim settlement failed: Claim {ClaimId} not found", id);
                return false;
            }

            // Business Rule: Cannot settle a rejected claim
            if (claim.Status == ClaimStatus.Rejected)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Attempted to settle a claim that has been rejected", id);
                throw new InvalidOperationException("Cannot settle a claim that has been rejected.");
            }

            if (claim.Status == ClaimStatus.Settled)
            {
                _logger.LogWarning("Invalid operation on Claim {ClaimId}: Claim is already Settled", id);
                throw new InvalidOperationException("Claim is already Settled.");
            }

            decimal finalSettlement = settlementAmount ?? claim.SettlementAmount ?? claim.ClaimAmount;
            if (finalSettlement > claim.ClaimAmount)
            {
                _logger.LogWarning("Claim settlement rejected for Claim {ClaimId}: Settlement amount {SettlementAmount} exceeds requested claim amount {ClaimAmount}", 
                    id, finalSettlement, claim.ClaimAmount);
                throw new InvalidOperationException("Settlement amount cannot exceed the requested claim amount.");
            }

            claim.Status = ClaimStatus.Settled;
            claim.SettlementAmount = finalSettlement;
            claim.ProcessedDate = DateTime.UtcNow;
            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation("Claim {ClaimId} ({ClaimNumber}) settled successfully with final payout {SettlementAmount}", 
                claim.Id, claim.ClaimNumber, finalSettlement);
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, UpdateClaimStatusDto updateClaimStatusDto)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                _logger.LogWarning("Claim status update failed: Claim {ClaimId} not found", id);
                return false;
            }

            // Map string to ClaimStatus enum (treating 'Registered' as 'Pending')
            ClaimStatus targetStatus;
            if (string.Equals(updateClaimStatusDto.Status, "Registered", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = ClaimStatus.Pending;
            }
            else if (!Enum.TryParse<ClaimStatus>(updateClaimStatusDto.Status, true, out targetStatus))
            {
                _logger.LogWarning("Claim status update failed: Invalid claim status '{Status}'", updateClaimStatusDto.Status);
                throw new ArgumentException($"Invalid claim status '{updateClaimStatusDto.Status}'.");
            }

            // Business Rules & State Transitions:
            // 1. Cannot settle a rejected claim
            if (claim.Status == ClaimStatus.Rejected && targetStatus == ClaimStatus.Settled)
            {
                _logger.LogWarning("Invalid state transition on Claim {ClaimId}: Cannot settle a claim that has been rejected", id);
                throw new InvalidOperationException("Cannot settle a claim that has been rejected.");
            }

            // 2. Cannot modify settled or rejected claims
            if (claim.Status == ClaimStatus.Settled)
            {
                _logger.LogWarning("Invalid state transition on Claim {ClaimId}: Cannot modify a claim that has already been Settled", id);
                throw new InvalidOperationException("Cannot modify a claim that has already been Settled.");
            }
            if (claim.Status == ClaimStatus.Rejected && targetStatus != ClaimStatus.Rejected)
            {
                _logger.LogWarning("Invalid state transition on Claim {ClaimId}: Cannot modify a claim that has already been Rejected", id);
                throw new InvalidOperationException("Cannot modify a claim that has already been Rejected.");
            }

            // 3. Validate valid state transitions
            bool isValidTransition = false;
            if (claim.Status == ClaimStatus.Pending)
            {
                isValidTransition = (targetStatus == ClaimStatus.UnderReview || targetStatus == ClaimStatus.Approved || targetStatus == ClaimStatus.Rejected);
            }
            else if (claim.Status == ClaimStatus.UnderReview)
            {
                isValidTransition = (targetStatus == ClaimStatus.Approved || targetStatus == ClaimStatus.Rejected);
            }
            else if (claim.Status == ClaimStatus.Approved)
            {
                isValidTransition = (targetStatus == ClaimStatus.Settled || targetStatus == ClaimStatus.Rejected);
            }
            else if (claim.Status == targetStatus)
            {
                isValidTransition = true;
            }

            if (!isValidTransition)
            {
                _logger.LogWarning("Invalid state transition on Claim {ClaimId}: Cannot transition from '{CurrentStatus}' to '{TargetStatus}'", 
                    id, claim.Status, targetStatus);
                throw new InvalidOperationException($"Invalid state transition: Cannot transition claim status from '{claim.Status}' to '{targetStatus}'.");
            }

            _logger.LogInformation("Updating Claim {ClaimId} status from {OldStatus} to {NewStatus}", id, claim.Status, targetStatus);
            claim.Status = targetStatus;
            claim.ProcessedDate = DateTime.UtcNow;

            if (targetStatus == ClaimStatus.Approved || targetStatus == ClaimStatus.Settled)
            {
                if (updateClaimStatusDto.SettlementAmount.HasValue)
                {
                    if (updateClaimStatusDto.SettlementAmount.Value > claim.ClaimAmount)
                    {
                        _logger.LogWarning("Settlement amount {SettlementAmount} exceeds claim amount {ClaimAmount} for Claim {ClaimId}", 
                            updateClaimStatusDto.SettlementAmount.Value, claim.ClaimAmount, id);
                        throw new InvalidOperationException("Settlement amount cannot exceed the requested claim amount.");
                    }
                    claim.SettlementAmount = updateClaimStatusDto.SettlementAmount.Value;
                }
                else if (!claim.SettlementAmount.HasValue)
                {
                    claim.SettlementAmount = claim.ClaimAmount;
                }
            }

            await _claimRepository.UpdateAsync(claim);
            return true;
        }

        public async Task<string> UploadDocumentAsync(int id, int currentUserId, string currentUserRole, IFormFile file)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                _logger.LogWarning("Document upload failed: Claim {ClaimId} not found", id);
                throw new KeyNotFoundException("Claim not found.");
            }

            // Verify customer ownership if Customer role
            if (currentUserRole == UserRoles.Customer && claim.CustomerId != currentUserId)
            {
                _logger.LogWarning("Unauthorized document upload: Customer {UserId} attempted to upload document to Claim {ClaimId} owned by Customer {OwnerId}", 
                    currentUserId, id, claim.CustomerId);
                throw new SecurityException("Access denied: You can only upload documents for your own claims.");
            }

            // Business rule: Cannot upload document to finished claims
            if (claim.Status == ClaimStatus.Settled || claim.Status == ClaimStatus.Rejected)
            {
                _logger.LogWarning("Document upload rejected: Claim {ClaimId} is already in '{Status}' status", id, claim.Status);
                throw new InvalidOperationException("Cannot upload documents to a claim that is already Settled or Rejected.");
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Document upload failed: No file was provided for Claim {ClaimId}", id);
                throw new ArgumentException("No file was uploaded.");
            }

            // Validate file size (Max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("Document upload rejected: File '{FileName}' ({FileSize} bytes) exceeds 5MB limit for Claim {ClaimId}", 
                    file.FileName, file.Length, id);
                throw new ArgumentException("File size exceeds the maximum limit of 5MB.");
            }

            // Validate file extension (Only .pdf, .jpg, .jpeg, .png)
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (string.IsNullOrEmpty(extension) || Array.IndexOf(allowedExtensions, extension) < 0)
            {
                _logger.LogWarning("Document upload rejected: File '{FileName}' with extension '{Extension}' is not allowed for Claim {ClaimId}", 
                    file.FileName, extension, id);
                throw new ArgumentException("Invalid file format. Only PDF, JPG, JPEG, and PNG files are allowed.");
            }

            try
            {
                // Ensure destination folder exists: /wwwroot/uploads/claims/
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRoot, "uploads", "claims");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // File naming strategy: Use GUID to prevent collision / overwriting
                var fileName = $"{Guid.NewGuid()}{extension}";
                var physicalPath = Path.Combine(uploadsFolder, fileName);

                // Save file using FileStream
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Store relative path in DB: "uploads/claims/{filename}"
                var relativePath = $"uploads/claims/{fileName}";
                claim.DocumentPath = relativePath;
                await _claimRepository.UpdateAsync(claim);

                _logger.LogInformation("Document uploaded successfully for Claim {ClaimId}. Stored path: {DocumentPath}, Size: {FileSize} bytes", 
                    id, relativePath, file.Length);

                return relativePath;
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is SecurityException || ex is InvalidOperationException || ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Unexpected error while storing document for Claim {ClaimId}", id);
                throw;
            }
        }
    }
}
