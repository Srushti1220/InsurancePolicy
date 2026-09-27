using InsuranceAPI.Data;
using InsuranceAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceAPI.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly InsuranceDbContext _context;

        public PaymentRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Policy)
                .ThenInclude(po => po!.PolicyType)
                .Include(p => p.Policy)
                .ThenInclude(po => po!.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .Include(p => p.Policy)
                .ThenInclude(po => po!.PolicyType)
                .Include(p => p.Policy)
                .ThenInclude(po => po!.Customer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByPolicyIdAsync(int policyId)
        {
            return await _context.Payments
                .Include(p => p.Policy)
                .ThenInclude(po => po!.PolicyType)
                .Where(p => p.PolicyId == policyId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Payments
                .Include(p => p.Policy)
                .ThenInclude(po => po!.PolicyType)
                .Where(p => p.Policy!.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetUpcomingDuePaymentsAsync(int? customerId = null)
        {
            var query = _context.Payments
                .Include(p => p.Policy)
                .ThenInclude(po => po!.PolicyType)
                .Include(p => p.Policy)
                .ThenInclude(po => po!.Customer)
                .Where(p => !p.IsPaid && p.Status != InsuranceAPI.Models.Enums.PaymentStatus.Completed);

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(p => p.Policy != null && p.Policy.CustomerId == customerId.Value);
            }

            return await query
                .OrderBy(p => p.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetOverduePaymentsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Payments
                .Where(p => !p.IsPaid && p.DueDate.Date < today && p.Status != InsuranceAPI.Models.Enums.PaymentStatus.Overdue)
                .ToListAsync();
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }
    }
}
