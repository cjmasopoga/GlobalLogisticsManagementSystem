using Global_Logistics_Management_System.Models;
using Global_Logistics_Management_System.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Global_Logistics_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _api;

        public HomeController(ApiService api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var clients = await _api.GetClientsAsync();
            var contracts = await _api.GetContractsAsync();
            var requests = await _api.GetServiceRequestsAsync();

            ViewBag.TotalClients = clients.Count;
            ViewBag.TotalContracts = contracts.Count;
            ViewBag.ActiveContracts = contracts.Count(c => c.Status == "Active");
            ViewBag.TotalServiceRequests = requests.Count;
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}


