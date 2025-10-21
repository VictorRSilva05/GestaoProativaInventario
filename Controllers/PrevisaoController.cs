using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GestaoProativaInventario.Models;
using GestaoProativaInventario.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestaoProativaInventario.Controllers
{
    public class PrevisaoController : Controller
    {
        private readonly PrevisaoService _previsaoService;
        private readonly ProdutoService _produtoService;

        public PrevisaoController(PrevisaoService previsaoService, ProdutoService produtoService)
        {
            _previsaoService = previsaoService;
            _produtoService = produtoService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["ProdutoId"] = new SelectList(await _produtoService.GetAllProdutosAsync(), "Id", "Nome");
            var previsoes = await _previsaoService.GetAllPrevisoesAsync();
            return View(previsoes);
        }

        [HttpGet]
        public async Task<JsonResult> GetDemandForecastChartData(int productId)
        {
            var chartData = await _previsaoService.GetDemandForecastChartData(productId);
            return Json(chartData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarPrevisao(int produtoId, int periodoObservacao, int intervaloPrevisao)
        {
            if (produtoId <= 0 || periodoObservacao <= 0 || intervaloPrevisao <= 0)
            {
                TempData["ErrorMessage"] = "Parâmetros inválidos para geração de previsão.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _previsaoService.GerarPrevisoesDemanda(produtoId, periodoObservacao, intervaloPrevisao);
                TempData["SuccessMessage"] = "Previsão gerada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao gerar previsão: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
