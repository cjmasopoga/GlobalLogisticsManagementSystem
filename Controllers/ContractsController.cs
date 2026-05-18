using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Global_Logistics_Management_System.Data;
using Global_Logistics_Management_System.Models;
using Global_Logistics_Management_System.Services;

namespace Global_Logistics_Management_System.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public ContractsController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Client)
                .ToListAsync();
            return View(contracts);
        }

        [HttpGet]
        public async Task<IActionResult> Search(ContractSearchViewModel model)
        {
            var query = _context.Contracts.Include(c => c.Client).AsQueryable();

            if (model.StartDateFrom.HasValue)
                query = query.Where(c => c.StartDate >= model.StartDateFrom.Value);

            if (model.StartDateTo.HasValue)
                query = query.Where(c => c.StartDate <= model.StartDateTo.Value);

            if (model.Status.HasValue)
                query = query.Where(c => c.Status == model.Status.Value);

            model.Results = await query.ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        public IActionResult Create()
        {
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientId,StartDate,EndDate,Status,ServiceLevel")] Contract contract, IFormFile? signedAgreement)
        {
            if (ModelState.IsValid)
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    try
                    {
                        contract.SignedAgreementPath = await _fileService.SaveSignedAgreementAsync(signedAgreement);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("signedAgreement", ex.Message);
                        ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
                        return View(contract);
                    }
                }

                _context.Add(contract);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Contract created successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
            return View(contract);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,StartDate,EndDate,Status,ServiceLevel,SignedAgreementPath")] Contract contract, IFormFile? signedAgreement)
        {
            if (id != contract.Id) return NotFound();
            if (ModelState.IsValid)
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    try
                    {
                        contract.SignedAgreementPath = await _fileService.SaveSignedAgreementAsync(signedAgreement);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("signedAgreement", ex.Message);
                        ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
                        return View(contract);
                    }
                }

                _context.Update(contract);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Contract updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
            return View(contract);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null) _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Contract deleted.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound();

            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, contract.SignedAgreementPath);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileName = Path.GetFileName(filePath);
            return PhysicalFile(filePath, "application/pdf", fileName);
        }
    }
}
