using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Global_Logistics_Management_System.Models;
using Global_Logistics_Management_System.Models.Api;
using Global_Logistics_Management_System.Services;

namespace Global_Logistics_Management_System.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApiService _api;

        public ContractsController(ApiService api) => _api = api;

        public async Task<IActionResult> Index()
            => View(await _api.GetContractsAsync());

        [HttpGet]
        public async Task<IActionResult> Search(string? status, DateTime? startDateFrom, DateTime? startDateTo)
        {
            var results = await _api.GetContractsAsync(status, startDateFrom, startDateTo);
            ViewBag.Status = status;
            ViewBag.StartDateFrom = startDateFrom;
            ViewBag.StartDateTo = startDateTo;
            return View(results);
        }

        public async Task<IActionResult> Details(int id)
        {
            var contract = await _api.GetContractAsync(id);
            if (contract is null) return NotFound();
            return View(contract);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = new SelectList(await _api.GetClientsAsync(), "Id", "Name");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContractDto dto, IFormFile? signedAgreement)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = new SelectList(await _api.GetClientsAsync(), "Id", "Name");
                return View(dto);
            }
            var (success, _) = await _api.CreateContractAsync(dto, signedAgreement);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to create contract. Check the API.");
                ViewBag.Clients = new SelectList(await _api.GetClientsAsync(), "Id", "Name");
                return View(dto);
            }
            TempData["Success"] = "Contract created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _api.GetContractAsync(id);
            if (contract is null) return NotFound();
            ViewBag.Clients = new SelectList(await _api.GetClientsAsync(), "Id", "Name", contract.ClientId);
            ViewBag.ContractId = id;
            var dto = new CreateContractDto(contract.ClientId, contract.StartDate, contract.EndDate,
                contract.Status, contract.ServiceLevel);
            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateContractDto dto, IFormFile? signedAgreement)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = new SelectList(await _api.GetClientsAsync(), "Id", "Name");
                ViewBag.ContractId = id;
                return View(dto);
            }
            await _api.UpdateContractAsync(id, dto);
            if (signedAgreement != null && signedAgreement.Length > 0)
                await _api.UploadAgreementAsync(id, signedAgreement);
            TempData["Success"] = "Contract updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _api.GetContractAsync(id);
            if (contract is null) return NotFound();
            return View(contract);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _api.DeleteContractAsync(id);
            TempData["Success"] = "Contract deleted.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(int id)
        {
            var result = await _api.DownloadAgreementAsync(id);
            if (result is null) return NotFound();
            return File(result.Value.Bytes, "application/pdf", result.Value.FileName);
        }
    }
}
