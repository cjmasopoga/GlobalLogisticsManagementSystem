using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Global_Logistics_Management_System.Models.Api;
using Global_Logistics_Management_System.Services;

namespace Global_Logistics_Management_System.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly ApiService _api;

        public ServiceRequestsController(ApiService api) => _api = api;

        public async Task<IActionResult> Index()
            => View(await _api.GetServiceRequestsAsync());

        public async Task<IActionResult> Details(int id)
        {
            var sr = await _api.GetServiceRequestAsync(id);
            if (sr is null) return NotFound();
            return View(sr);
        }

        public async Task<IActionResult> Create()
        {
            var contracts = await _api.GetContractsAsync();
            var active = contracts.Where(c => c.Status is "Active" or "Draft").ToList();
            ViewData["ContractId"] = new SelectList(
                active.Select(c => new { c.Id, Display = $"{c.ClientName} - {c.ServiceLevel} ({c.Status})" }),
                "Id", "Display");
            ViewBag.ExchangeRate = await _api.GetExchangeRateAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateServiceRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var contracts = await _api.GetContractsAsync();
                var active = contracts.Where(c => c.Status is "Active" or "Draft").ToList();
                ViewData["ContractId"] = new SelectList(
                    active.Select(c => new { c.Id, Display = $"{c.ClientName} - {c.ServiceLevel} ({c.Status})" }),
                    "Id", "Display", dto.ContractId);
                ViewBag.ExchangeRate = await _api.GetExchangeRateAsync();
                return View(dto);
            }

            var (success, error) = await _api.CreateServiceRequestAsync(dto);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create service request.");
                var contracts = await _api.GetContractsAsync();
                var active = contracts.Where(c => c.Status is "Active" or "Draft").ToList();
                ViewData["ContractId"] = new SelectList(
                    active.Select(c => new { c.Id, Display = $"{c.ClientName} - {c.ServiceLevel} ({c.Status})" }),
                    "Id", "Display", dto.ContractId);
                ViewBag.ExchangeRate = await _api.GetExchangeRateAsync();
                return View(dto);
            }

            TempData["Success"] = "Service request created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var sr = await _api.GetServiceRequestAsync(id);
            if (sr is null) return NotFound();
            return View(sr);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _api.DeleteServiceRequestAsync(id);
            TempData["Success"] = "Service request deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetExchangeRate()
        {
            var rate = await _api.GetExchangeRateAsync();
            return Json(new { rate });
        }
    }
}

