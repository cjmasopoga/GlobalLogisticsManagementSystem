using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Global_Logistics_Management_System.Data;
using Global_Logistics_Management_System.Models;
using Global_Logistics_Management_System.Services;

namespace Global_Logistics_Management_System.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyService _currencyService;

        public ServiceRequestsController(ApplicationDbContext context, ICurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .ToListAsync();
            return View(requests);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(sr => sr.Id == id);
            if (serviceRequest == null) return NotFound();
            return View(serviceRequest);
        }

        public async Task<IActionResult> Create()
        {
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Draft)
                .ToListAsync();

            ViewData["ContractId"] = new SelectList(
                activeContracts.Select(c => new { c.Id, Display = $"{c.Client!.Name} - {c.ServiceLevel} ({c.Status})" }),
                "Id", "Display");

            var rate = await _currencyService.GetUsdToZarRateAsync();
            ViewBag.ExchangeRate = rate;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContractId,Description,CostUsd,Status")] ServiceRequest serviceRequest)
        {
            var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);

            if (contract == null)
            {
                ModelState.AddModelError("ContractId", "Selected contract does not exist.");
            }
            else if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                ModelState.AddModelError("ContractId",
                    $"Cannot create a service request for a contract with status '{contract.Status}'. Only Active or Draft contracts are allowed.");
            }

            if (ModelState.IsValid)
            {
                var rate = await _currencyService.GetUsdToZarRateAsync();
                serviceRequest.ExchangeRateUsed = rate;
                serviceRequest.CostZar = _currencyService.ConvertUsdToZar(serviceRequest.CostUsd, rate);
                serviceRequest.CreatedAt = DateTime.UtcNow;

                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Service request created successfully.";
                return RedirectToAction(nameof(Index));
            }

            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Draft)
                .ToListAsync();

            ViewData["ContractId"] = new SelectList(
                activeContracts.Select(c => new { c.Id, Display = $"{c.Client!.Name} - {c.ServiceLevel} ({c.Status})" }),
                "Id", "Display", serviceRequest.ContractId);

            var currentRate = await _currencyService.GetUsdToZarRateAsync();
            ViewBag.ExchangeRate = currentRate;

            return View(serviceRequest);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null) return NotFound();

            ViewData["ContractId"] = new SelectList(_context.Contracts.Include(c => c.Client),
                "Id", "ServiceLevel", serviceRequest.ContractId);
            return View(serviceRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ContractId,Description,CostUsd,CostZar,ExchangeRateUsed,Status,CreatedAt")] ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(serviceRequest);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Service request updated.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractId"] = new SelectList(_context.Contracts.Include(c => c.Client),
                "Id", "ServiceLevel", serviceRequest.ContractId);
            return View(serviceRequest);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(sr => sr.Id == id);
            if (serviceRequest == null) return NotFound();
            return View(serviceRequest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null) _context.ServiceRequests.Remove(serviceRequest);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Service request deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetExchangeRate()
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            return Json(new { rate });
        }
    }
}
