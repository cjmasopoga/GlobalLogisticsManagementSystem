using Global_Logistics_Management_System.Data;
using Global_Logistics_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Global_Logistics_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalClients = await _context.Clients.CountAsync();
            ViewBag.TotalContracts = await _context.Contracts.CountAsync();
            ViewBag.ActiveContracts = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Active);
            ViewBag.TotalServiceRequests = await _context.ServiceRequests.CountAsync();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

