using GLMS.API.Data;
using GLMS.API.DTOs;
using GLMS.API.Models;
using GLMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly ICurrencyService _currency;

        public ServiceRequestsController(ApiDbContext context, ICurrencyService currency)
        {
            _context = context;
            _currency = currency;
        }

        /// <summary>Get all service requests.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceRequestDto>>> GetAll()
        {
            var list = await _context.ServiceRequests
                .Include(sr => sr.Contract).ThenInclude(c => c!.Client)
                .ToListAsync();
            return Ok(list.Select(ToDto));
        }

        /// <summary>Get a single service request by ID.</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceRequestDto>> Get(int id)
        {
            var sr = await _context.ServiceRequests
                .Include(x => x.Contract).ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (sr is null) return NotFound();
            return Ok(ToDto(sr));
        }

        /// <summary>Create a service request. Only allowed on Active or Draft contracts.</summary>
        [HttpPost]
        public async Task<ActionResult<ServiceRequestDto>> Create([FromBody] CreateServiceRequestDto dto)
        {
            var contract = await _context.Contracts.FindAsync(dto.ContractId);
            if (contract is null)
                return BadRequest(new { message = "Contract not found." });

            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
                return BadRequest(new { message = $"Cannot create service request for a contract with status '{contract.Status}'." });

            if (!Enum.TryParse<ServiceRequestStatus>(dto.Status, true, out var srStatus))
                return BadRequest(new { message = $"Invalid status: {dto.Status}" });

            var rate = await _currency.GetUsdToZarRateAsync();
            var sr = new ServiceRequest
            {
                ContractId = dto.ContractId,
                Description = dto.Description,
                CostUsd = dto.CostUsd,
                CostZar = _currency.ConvertUsdToZar(dto.CostUsd, rate),
                ExchangeRateUsed = rate,
                Status = srStatus,
                CreatedAt = DateTime.UtcNow
            };
            _context.ServiceRequests.Add(sr);
            await _context.SaveChangesAsync();
            await _context.Entry(sr).Reference(x => x.Contract).LoadAsync();
            await _context.Entry(sr.Contract!).Reference(c => c.Client).LoadAsync();
            return CreatedAtAction(nameof(Get), new { id = sr.Id }, ToDto(sr));
        }

        /// <summary>Update a service request status.</summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> PatchStatus(int id, [FromBody] PatchContractStatusDto dto)
        {
            var sr = await _context.ServiceRequests.FindAsync(id);
            if (sr is null) return NotFound();

            if (!Enum.TryParse<ServiceRequestStatus>(dto.Status, true, out var status))
                return BadRequest(new { message = $"Invalid status: {dto.Status}" });

            sr.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a service request.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sr = await _context.ServiceRequests.FindAsync(id);
            if (sr is null) return NotFound();
            _context.ServiceRequests.Remove(sr);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Get the current USD to ZAR exchange rate.</summary>
        [HttpGet("exchange-rate")]
        public async Task<ActionResult<object>> GetRate()
        {
            var rate = await _currency.GetUsdToZarRateAsync();
            return Ok(new { rate });
        }

        private static ServiceRequestDto ToDto(ServiceRequest sr) => new(
            sr.Id, sr.ContractId,
            sr.Contract?.ServiceLevel ?? "",
            sr.Contract?.Client?.Name ?? "",
            sr.Description, sr.CostUsd, sr.CostZar,
            sr.ExchangeRateUsed, sr.Status.ToString(), sr.CreatedAt);
    }
}
