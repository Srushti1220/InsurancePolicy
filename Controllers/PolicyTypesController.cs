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
    [Route("api/policy-types")]
    [Authorize]
    public class PolicyTypesController : ControllerBase
    {
        private readonly IPolicyTypeService _policyTypeService;

        public PolicyTypesController(IPolicyTypeService policyTypeService)
        {
            _policyTypeService = policyTypeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PolicyTypeDto>>> GetAll()
        {
            var policyTypes = await _policyTypeService.GetAllAsync();
            return Ok(policyTypes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PolicyTypeDto>> GetById(int id)
        {
            var policyType = await _policyTypeService.GetByIdAsync(id);
            if (policyType == null)
            {
                return NotFound(new { success = false, message = "Policy type not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }
            return Ok(policyType);
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<PolicyTypeDto>> Create([FromBody] CreatePolicyTypeDto createDto)
        {
            var created = await _policyTypeService.CreateAsync(createDto);
            return StatusCode(201, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePolicyTypeDto updateDto)
        {
            var result = await _policyTypeService.UpdateAsync(id, updateDto);
            if (!result)
            {
                return NotFound(new { success = false, message = "Policy type not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "Policy type updated successfully.", statusCode = 200 });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _policyTypeService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Policy type not found.", errorCode = "NOT_FOUND", timestamp = DateTime.UtcNow.ToString("o") });
            }
            return Ok(new { success = true, message = "Policy type deleted successfully.", statusCode = 200 });
        }
    }
}
