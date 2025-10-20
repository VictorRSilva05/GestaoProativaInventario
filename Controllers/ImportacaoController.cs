using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GestaoProativaInventario.Models;
using GestaoProativaInventario.Services;

namespace GestaoProativaInventario.Controllers;

public class ImportacaoController : Controller
{
    private readonly ILogger<ImportacaoController> _logger;
    private readonly CsvImportService _csvImportService;

    public ImportacaoController(ILogger<ImportacaoController> logger, CsvImportService csvImportService)
    {
        _logger = logger;
        _csvImportService = csvImportService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ViewBag.Message = "Por favor, selecione um arquivo para upload.";
            return View("Index");
        }

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Message = "Formato de arquivo inválido. Por favor, selecione um arquivo CSV.";
            return View("Index");
        }

        try
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await _csvImportService.ImportSalesData(stream);
            }
            ViewBag.Message = "Dados importados com sucesso!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar arquivo CSV.");
            ViewBag.Message = $"Erro ao importar dados: {ex.Message}";
        }

        return View("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

