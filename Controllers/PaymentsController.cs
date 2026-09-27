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
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll()
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (role == UserRoles.Customer)
            {
                var ownPayments = await _paymentService.GetByCustomerIdAsync(userId);
                return Ok(ownPayments);
            }
            else
            {
                var allPayments = await _paymentService.GetAllAsync();
                return Ok(allPayments);
            }
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetUpcomingDuePayments()
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (role == UserRoles.Customer)
            {
                var ownUpcoming = await _paymentService.GetUpcomingDuePaymentsAsync(userId);
                return Ok(ownUpcoming);
            }
            else
            {
                var allUpcoming = await _paymentService.GetUpcomingDuePaymentsAsync(null);
                return Ok(allUpcoming);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDto>> GetById(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound(new { success = false, message = "Payment record not found.", errorCode = "NOT_FOUND", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            // Ownership check: Customer can only view their own payment details
            if (role == UserRoles.Customer && payment.CustomerId != userId)
            {
                _logger.LogWarning("Forbidden: User {UserId} attempted to view Payment {PaymentId} owned by Customer {OwnerId}", 
                    userId, id, payment.CustomerId);
                return StatusCode(403, new { success = false, message = "Access denied: You do not own this payment record.", errorCode = "FORBIDDEN", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            return Ok(payment);
        }

        [HttpPost("{id}/pay")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> Pay(int id, [FromBody] PayPremiumDto payDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Customer {CustomerId} initiated payment for PaymentId {PaymentId}", userId, id);

            // Verify ownership first
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound(new { success = false, message = "Payment record not found.", errorCode = "NOT_FOUND", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            if (payment.CustomerId != userId)
            {
                _logger.LogWarning("Forbidden: Customer {CustomerId} attempted to pay for Payment {PaymentId} owned by Customer {OwnerId}", 
                    userId, id, payment.CustomerId);
                return StatusCode(403, new { success = false, message = "Access denied: You do not own this payment record.", errorCode = "FORBIDDEN", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            // Pay the premium
            var result = await _paymentService.ProcessPaymentAsync(id, payDto.TransactionReference);
            if (!result)
            {
                _logger.LogWarning("Payment {PaymentId} was rejected during processing. It may already be completed", id);
                return BadRequest(new { success = false, message = "Payment failed. It may have already been completed.", errorCode = "BAD_REQUEST", timestamp = System.DateTime.UtcNow.ToString("o") });
            }

            return Ok(new { success = true, message = "Premium payment processed successfully.", statusCode = 200 });
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
