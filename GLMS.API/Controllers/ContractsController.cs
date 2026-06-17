using GLMS.API.Data;
using GLMS.API.DTOs;
using GLMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public ContractsController(ApiDbContext context) => _context = context;

        /// <summary>Get all contracts with optional filtering by status and date range.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractDto>>> GetAll(
            [FromQuery] string? status,
            [FromQuery] DateTime? startDateFrom,
            [FromQuery] DateTime? startDateTo)
        {
            var query = _context.Contracts.Include(c => c.Client).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ContractStatus>(status, true, out var parsedStatus))
                query = query.Where(c => c.Status == parsedStatus);

            if (startDateFrom.HasValue)
                query = query.Where(c => c.StartDate >= startDateFrom.Value);

            if (startDateTo.HasValue)
                query = query.Where(c => c.StartDate <= startDateTo.Value);

            var contracts = await query.ToListAsync();
            return Ok(contracts.Select(ToDto));
        }

        /// <summary>Get a single contract by ID.</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractDto>> Get(int id)
        {
            var c = await _context.Contracts.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return NotFound();
            return Ok(ToDto(c));
        }

        /// <summary>Create a new contract.</summary>
        [HttpPost]
        public async Task<ActionResult<ContractDto>> Create([FromBody] CreateContractDto dto)
        {
            if (!Enum.TryParse<ContractStatus>(dto.Status, true, out var status))
                return BadRequest(new { message = $"Invalid status value: {dto.Status}" });

            var contract = new Contract
            {
                ClientId = dto.ClientId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = status,
                ServiceLevel = dto.ServiceLevel
            };
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            await _context.Entry(contract).Reference(c => c.Client).LoadAsync();
            return CreatedAtAction(nameof(Get), new { id = contract.Id }, ToDto(contract));
        }

        /// <summary>Update a contract.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateContractDto dto)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract is null) return NotFound();

            if (!Enum.TryParse<ContractStatus>(dto.Status, true, out var status))
                return BadRequest(new { message = $"Invalid status value: {dto.Status}" });

            contract.ClientId = dto.ClientId;
            contract.StartDate = dto.StartDate;
            contract.EndDate = dto.EndDate;
            contract.Status = status;
            contract.ServiceLevel = dto.ServiceLevel;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Patch only the status of a contract (approve / decline).</summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> PatchStatus(int id, [FromBody] PatchContractStatusDto dto)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract is null) return NotFound();

            if (!Enum.TryParse<ContractStatus>(dto.Status, true, out var status))
                return BadRequest(new { message = $"Invalid status value: {dto.Status}" });

            contract.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a contract.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract is null) return NotFound();
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Upload a signed PDF agreement for a contract.</summary>
        [HttpPost("{id}/upload")]
        public async Task<IActionResult> UploadAgreement(int id, IFormFile file)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract is null) return NotFound();

            if (file is null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                return BadRequest(new { message = "Only .pdf files are allowed." });

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            contract.SignedAgreementPath = Path.Combine("uploads", fileName);
            await _context.SaveChangesAsync();
            return Ok(new { path = contract.SignedAgreementPath });
        }

        /// <summary>Download the signed PDF agreement for a contract.</summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract is null || string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contract.SignedAgreementPath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/pdf", Path.GetFileName(filePath));
        }

        private static ContractDto ToDto(Contract c) => new(
            c.Id, c.ClientId, c.Client?.Name ?? "",
            c.StartDate, c.EndDate,
            c.Status.ToString(), c.ServiceLevel,
            !string.IsNullOrEmpty(c.SignedAgreementPath));
    }
}
