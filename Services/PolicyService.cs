using AutoMapper;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models;
using InsuranceAPI.Models.Enums;
using InsuranceAPI.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IPolicyTypeRepository _policyTypeRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPremiumCalculator _premiumCalculator;
        private readonly IMapper _mapper;
        private readonly ILogger<PolicyService> _logger;

        public PolicyService(
            IPolicyRepository policyRepository,
            IPolicyTypeRepository policyTypeRepository,
            IPaymentRepository paymentRepository,
            IPremiumCalculator premiumCalculator,
            IMapper mapper,
            ILogger<PolicyService> logger)
        {
            _policyRepository = policyRepository;
            _policyTypeRepository = policyTypeRepository;
            _paymentRepository = paymentRepository;
            _premiumCalculator = premiumCalculator;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PolicyDto?> GetByIdAsync(int id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            return policy == null ? null : _mapper.Map<PolicyDto>(policy);
        }

        public async Task<IEnumerable<PolicyDto>> GetAllAsync()
        {
            var policies = await _policyRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<PolicyDto>>(policies);
        }

        public async Task<IEnumerable<PolicyDto>> GetByCustomerIdAsync(int customerId)
        {
            var policies = await _policyRepository.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<PolicyDto>>(policies);
        }

        public async Task<IEnumerable<PolicyDto>> GetPendingAsync()
        {
            var policies = await _policyRepository.GetPendingAsync();
            return _mapper.Map<IEnumerable<PolicyDto>>(policies);
        }

        public async Task<PolicyDto> ApplyAsync(int customerId, ApplyPolicyDto applyDto)
        {
            var policyType = await _policyTypeRepository.GetByIdAsync(applyDto.PolicyTypeId);
            if (policyType == null)
            {
                _logger.LogWarning("Policy application failed: PolicyTypeId {PolicyTypeId} does not exist", applyDto.PolicyTypeId);
                throw new InvalidOperationException("Selected policy type does not exist.");
            }

            if (!policyType.IsActive)
            {
                _logger.LogWarning("Policy application rejected: PolicyType {PolicyTypeId} is currently inactive", applyDto.PolicyTypeId);
                throw new InvalidOperationException("Selected policy type is currently inactive.");
            }

            // Business Rule: Prevent duplicate policy applications for the same policy type
            var existingApplication = await _policyRepository.GetActiveOrPendingByCustomerAndTypeAsync(customerId, applyDto.PolicyTypeId);
            if (existingApplication != null)
            {
                _logger.LogWarning("Duplicate policy attempt detected: Customer {CustomerId} already has an active or pending policy for PolicyTypeId {PolicyTypeId}", 
                    customerId, applyDto.PolicyTypeId);
                throw new InvalidOperationException("Duplicate policy application rejected. You already have an active or pending policy for this policy type.");
            }

            // Generate Policy properties
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddYears(policyType.DurationInYears > 0 ? policyType.DurationInYears : 1);

            // Reusable calculation inside service layer
            var premiumAmount = _premiumCalculator.CalculatePremium(policyType, policyType.CoverageAmount, policyType.DurationInYears);

            var policy = new Policy
            {
                PolicyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                PolicyTypeId = policyType.Id,
                CustomerId = customerId,
                StartDate = startDate,
                EndDate = endDate,
                PremiumAmount = premiumAmount,
                CoverageAmount = policyType.CoverageAmount,
                Status = PolicyStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _policyRepository.CreateAsync(policy);

            _logger.LogInformation("Policy {PolicyId} applied successfully. CustomerId: {CustomerId}, PolicyTypeId: {PolicyTypeId}, PolicyNumber: {PolicyNumber}, Premium: {PremiumAmount}", 
                created.Id, customerId, applyDto.PolicyTypeId, created.PolicyNumber, created.PremiumAmount);

            // Eager load details for response mapping
            var loadedPolicy = await _policyRepository.GetByIdAsync(created.Id);
            return _mapper.Map<PolicyDto>(loadedPolicy);
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.LogWarning("Policy approval failed: Policy {PolicyId} not found", id);
                return false;
            }

            // Business Rule: State transition validations
            if (policy.Status == PolicyStatus.Approved)
            {
                _logger.LogWarning("Cannot approve policy {PolicyId}. It is already approved", id);
                throw new InvalidOperationException("Cannot approve policy. It is already approved.");
            }

            if (policy.Status == PolicyStatus.Rejected)
            {
                _logger.LogWarning("Cannot approve policy {PolicyId}. It has been rejected", id);
                throw new InvalidOperationException("Cannot approve policy. It has been rejected.");
            }

            if (policy.Status != PolicyStatus.Pending)
            {
                _logger.LogWarning("Cannot approve policy {PolicyId}. Status is '{Status}', expected 'Pending'", id, policy.Status);
                throw new InvalidOperationException($"Cannot approve policy. Only policies in '{PolicyStatus.Pending}' status can be approved.");
            }

            policy.Status = PolicyStatus.Approved;
            await _policyRepository.UpdateAsync(policy);

            // Create initial premium payment record
            var payment = new Payment
            {
                PolicyId = policy.Id,
                Amount = policy.PremiumAmount,
                DueDate = DateTime.UtcNow,
                IsPaid = false,
                Status = PaymentStatus.Pending,
                TransactionReference = string.Empty
            };
            await _paymentRepository.CreateAsync(payment);

            _logger.LogInformation("Policy {PolicyId} ({PolicyNumber}) approved successfully. Initial payment {PaymentId} generated", 
                policy.Id, policy.PolicyNumber, payment.Id);

            return true;
        }

        public async Task<bool> RejectAsync(int id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.LogWarning("Policy rejection failed: Policy {PolicyId} not found", id);
                return false;
            }

            // Business Rule: State transition validations
            if (policy.Status == PolicyStatus.Approved)
            {
                _logger.LogWarning("Cannot reject policy {PolicyId}. An approved policy cannot be rejected", id);
                throw new InvalidOperationException("Cannot reject policy. An approved policy cannot be rejected.");
            }

            if (policy.Status == PolicyStatus.Rejected)
            {
                _logger.LogWarning("Policy {PolicyId} is already rejected", id);
                throw new InvalidOperationException("Policy is already rejected.");
            }

            if (policy.Status != PolicyStatus.Pending)
            {
                _logger.LogWarning("Cannot reject policy {PolicyId} in '{Status}' status", id, policy.Status);
                throw new InvalidOperationException($"Cannot reject policy in '{policy.Status}' status.");
            }

            policy.Status = PolicyStatus.Rejected;
            await _policyRepository.UpdateAsync(policy);

            _logger.LogInformation("Policy {PolicyId} ({PolicyNumber}) rejected successfully", policy.Id, policy.PolicyNumber);

            return true;
        }

        public async Task<CalculatePremiumResponseDto> CalculatePremiumAsync(CalculatePremiumRequestDto requestDto)
        {
            var policyType = await _policyTypeRepository.GetByIdAsync(requestDto.PolicyTypeId);
            if (policyType == null)
            {
                _logger.LogWarning("Premium calculation failed: PolicyTypeId {PolicyTypeId} not found", requestDto.PolicyTypeId);
                throw new KeyNotFoundException("Policy type not found.");
            }

            var calculatedPremium = _premiumCalculator.CalculatePremium(
                policyType, 
                requestDto.CoverageAmount, 
                requestDto.DurationInYears);

            _logger.LogInformation("Calculated premium {CalculatedPremium} for PolicyTypeId: {PolicyTypeId}, Coverage: {CoverageAmount}, Duration: {DurationInYears} years", 
                calculatedPremium, requestDto.PolicyTypeId, requestDto.CoverageAmount, requestDto.DurationInYears);

            return new CalculatePremiumResponseDto
            {
                PolicyTypeId = policyType.Id,
                PolicyTypeName = policyType.Name,
                CoverageAmount = requestDto.CoverageAmount,
                DurationInYears = requestDto.DurationInYears,
                BasePremium = policyType.BasePremium,
                CalculatedPremium = calculatedPremium
            };
        }

        public async Task<PolicyDto> CreateAsync(CreatePolicyDto createPolicyDto)
        {
            var policy = _mapper.Map<Policy>(createPolicyDto);
            policy.PolicyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            policy.CreatedAt = DateTime.UtcNow;

            var created = await _policyRepository.CreateAsync(policy);
            _logger.LogInformation("Policy {PolicyId} created by Admin. PolicyNumber: {PolicyNumber}, CustomerId: {CustomerId}", 
                created.Id, created.PolicyNumber, created.CustomerId);

            var loadedPolicy = await _policyRepository.GetByIdAsync(created.Id);
            return _mapper.Map<PolicyDto>(loadedPolicy);
        }

        public async Task<bool> UpdateAsync(int id, UpdatePolicyDto updatePolicyDto)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.LogWarning("Policy update failed: Policy {PolicyId} not found", id);
                return false;
            }

            if (Enum.TryParse<PolicyStatus>(updatePolicyDto.Status, true, out var newStatus))
            {
                _logger.LogInformation("Updating Policy {PolicyId} status from {OldStatus} to {NewStatus}", policy.Id, policy.Status, newStatus);
                policy.Status = newStatus;
            }
            else
            {
                _logger.LogWarning("Policy update failed: Invalid policy status '{Status}'", updatePolicyDto.Status);
                throw new ArgumentException($"Invalid policy status '{updatePolicyDto.Status}'.");
            }
            await _policyRepository.UpdateAsync(policy);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.LogWarning("Policy deletion failed: Policy {PolicyId} not found", id);
                return false;
            }

            await _policyRepository.DeleteAsync(id);
            _logger.LogInformation("Policy {PolicyId} deleted successfully", id);
            return true;
        }
    }
}
