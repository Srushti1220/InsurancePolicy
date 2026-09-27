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
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IPolicyRepository policyRepository,
            IMapper mapper,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _policyRepository = policyRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PaymentDto?> GetByIdAsync(int id)
        {
            await MarkOverduePaymentsAsync();
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment == null ? null : _mapper.Map<PaymentDto>(payment);
        }

        public async Task<IEnumerable<PaymentDto>> GetAllAsync()
        {
            await MarkOverduePaymentsAsync();
            var payments = await _paymentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetByPolicyIdAsync(int policyId)
        {
            await MarkOverduePaymentsAsync();
            var payments = await _paymentRepository.GetByPolicyIdAsync(policyId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetByCustomerIdAsync(int customerId)
        {
            await MarkOverduePaymentsAsync();
            var payments = await _paymentRepository.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetUpcomingDuePaymentsAsync(int? customerId = null)
        {
            await MarkOverduePaymentsAsync();
            var payments = await _paymentRepository.GetUpcomingDuePaymentsAsync(customerId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<int> MarkOverduePaymentsAsync()
        {
            var overdueList = await _paymentRepository.GetOverduePaymentsAsync();
            int count = 0;
            foreach (var payment in overdueList)
            {
                _logger.LogInformation("Marking Payment {PaymentId} as Overdue. PolicyId: {PolicyId}, Amount: {Amount}, DueDate: {DueDate}", 
                    payment.Id, payment.PolicyId, payment.Amount, payment.DueDate);
                payment.Status = PaymentStatus.Overdue;
                await _paymentRepository.UpdateAsync(payment);
                count++;
            }

            if (count > 0)
            {
                _logger.LogInformation("Overdue payments check completed: {Count} payments marked as Overdue", count);
            }
            return count;
        }

        public async Task<bool> ProcessPaymentAsync(int id, string transactionReference)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Payment processing failed: Payment {PaymentId} not found", id);
                return false;
            }

            if (payment.IsPaid || payment.Status == PaymentStatus.Completed)
            {
                _logger.LogWarning("Payment processing rejected: Payment {PaymentId} is already paid or completed (Status: {Status})", 
                    id, payment.Status);
                return false;
            }

            payment.IsPaid = true;
            payment.Status = PaymentStatus.Completed;
            payment.PaymentDate = DateTime.UtcNow;
            payment.TransactionReference = transactionReference;
            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Payment {PaymentId} of amount {Amount} processed successfully. PolicyId: {PolicyId}, TxnRef: {TxnRef}", 
                payment.Id, payment.Amount, payment.PolicyId, transactionReference);

            // If the policy was Pending, activate/approve it when paid
            if (payment.Policy != null && payment.Policy.Status == PolicyStatus.Pending)
            {
                payment.Policy.Status = PolicyStatus.Approved;
                await _policyRepository.UpdateAsync(payment.Policy);
                _logger.LogInformation("Policy {PolicyId} approved following successful initial premium payment", payment.Policy.Id);
            }

            return true;
        }
    }
}
