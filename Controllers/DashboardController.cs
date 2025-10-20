using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GestaoProativaInventario.Models;
using GestaoProativaInventario.Services;
using System.Threading.Tasks;

namespace GestaoProativaInventario.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly AlertaService _alertaService;

        public DashboardController(DashboardService dashboardService, AlertaService alertaService)
        {
            _dashboardService = dashboardService;
            _alertaService = alertaService;
        }

        public async Task<IActionResult> Index()
        {
            var summary = await _dashboardService.GetDashboardSummary();
            var topAlertas = await _dashboardService.GetTopAlertas();

            var viewModel = new DashboardViewModel
            {
                Summary = summary,
                TopAlertas = topAlertas
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class DashboardViewModel
    {
        public DashboardSummaryViewModel Summary { get; set; }
        public List<Alerta> TopAlertas { get; set; }
    }
}
