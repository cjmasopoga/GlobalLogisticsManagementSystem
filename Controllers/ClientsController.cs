using Microsoft.AspNetCore.Mvc;
using Global_Logistics_Management_System.Models.Api;
using Global_Logistics_Management_System.Services;

namespace Global_Logistics_Management_System.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ApiService _api;

        public ClientsController(ApiService api) => _api = api;

        public async Task<IActionResult> Index()
            => View(await _api.GetClientsAsync());

        public async Task<IActionResult> Details(int id)
        {
            var client = await _api.GetClientAsync(id);
            if (client is null) return NotFound();
            return View(client);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClientDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _api.CreateClientAsync(dto);
            TempData["Success"] = "Client created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var client = await _api.GetClientAsync(id);
            if (client is null) return NotFound();
            return View(new CreateClientDto(client.Name, client.ContactDetails, client.Region));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateClientDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _api.UpdateClientAsync(id, dto);
            TempData["Success"] = "Client updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var client = await _api.GetClientAsync(id);
            if (client is null) return NotFound();
            return View(client);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _api.DeleteClientAsync(id);
            TempData["Success"] = "Client deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

