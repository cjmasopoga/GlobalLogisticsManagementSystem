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
    public class ClientsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public ClientsController(ApiDbContext context) => _context = context;

        /// <summary>Get all clients.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll()
        {
            var clients = await _context.Clients
                .Include(c => c.Contracts)
                .ToListAsync();

            return Ok(clients.Select(c => new ClientDto(
                c.Id, c.Name, c.ContactDetails, c.Region, c.Contracts.Count)));
        }

        /// <summary>Get a single client by ID.</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> Get(int id)
        {
            var c = await _context.Clients.Include(x => x.Contracts).FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return NotFound();
            return Ok(new ClientDto(c.Id, c.Name, c.ContactDetails, c.Region, c.Contracts.Count));
        }

        /// <summary>Create a new client.</summary>
        [HttpPost]
        public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto dto)
        {
            var client = new Client
            {
                Name = dto.Name,
                ContactDetails = dto.ContactDetails,
                Region = dto.Region
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = client.Id },
                new ClientDto(client.Id, client.Name, client.ContactDetails, client.Region, 0));
        }

        /// <summary>Update an existing client.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateClientDto dto)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client is null) return NotFound();
            client.Name = dto.Name;
            client.ContactDetails = dto.ContactDetails;
            client.Region = dto.Region;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a client.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client is null) return NotFound();
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
