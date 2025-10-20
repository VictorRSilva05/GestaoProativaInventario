using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GestaoProativaInventario.Models;
using GestaoProativaInventario.Services;
using System.Threading.Tasks;

namespace GestaoProativaInventario.Controllers
{
    public class AlertaController : Controller
    {
        private readonly AlertaService _alertaService;

        public AlertaController(AlertaService alertaService)
        {
            _alertaService = alertaService;
        }

        public async Task<IActionResult> Index()
        {
            var alertas = await _alertaService.GetAllAlertasAsync();
            return View(alertas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarResolvido(int id)
        {
            await _alertaService.MarcarAlertaComoResolvidoAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
