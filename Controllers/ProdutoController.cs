using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestaoProativaInventario.Models;
using GestaoProativaInventario.Services;

namespace GestaoProativaInventario.Controllers
{
    public class ProdutoController : Controller
    {
        private readonly ProdutoService _produtoService;
        private readonly CategoriaService _categoriaService;
        private readonly PrevisaoService _previsaoService; 	// <-- ADICIONAR
        private readonly AlertaService _alertaService; 		// <-- ADICIONAR

        public ProdutoController(ProdutoService produtoService, CategoriaService categoriaService, PrevisaoService previsaoService, AlertaService alertaService)
        {
            _produtoService = produtoService;
            _categoriaService = categoriaService;
            _previsaoService = previsaoService; 	// <-- ADICIONAR
            _alertaService = alertaService; 		// <-- ADICIONAR
        }

        // GET: Produto
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var produtos = await _produtoService.SearchProdutosAsync(searchString);
            return View(produtos);
        }

        // GET: Produto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _produtoService.GetProdutoByIdAsync(id.Value);
            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // GET: Produto/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CategoriaId"] = new SelectList(await _categoriaService.GetAllCategoriasAsync(), "Id", "Nome");
            return View();
        }

        // POST: Produto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nome,CodigoBarras,CategoriaId,PrecoMedio,DataValidade,EstoqueAtual,ProdutoIdOrigem")] Produto produto)
        {
            if (ModelState.IsValid)
            {
                produto.DataValidade = produto.DataValidade?.ToUniversalTime();
                await _produtoService.AddProdutoAsync(produto);
                // --- INÍCIO DO CONSERTO (GATILHOS) ---
                // 1. Gera a previsão inicial (o ML.NET usará o fallback, pois não há vendas)
                await _previsaoService.GerarPrevisoesDemanda(produto.Id, 30, 30); 
                
                // 2. Roda a verificação de alertas (comparando o estoque inicial com a previsão de 0)
                await _alertaService.GerarAlertasAutomaticos();
                // --- FIM DO CONSERTO ---
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(await _categoriaService.GetAllCategoriasAsync(), "Id", "Nome", produto.CategoriaId);
            return View(produto);
        }

        // GET: Produto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _produtoService.GetProdutoByIdAsync(id.Value);
            if (produto == null)
            {
                return NotFound();
            }
            ViewData["CategoriaId"] = new SelectList(await _categoriaService.GetAllCategoriasAsync(), "Id", "Nome", produto.CategoriaId);
            return View(produto);
        }

        // POST: Produto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,CodigoBarras,CategoriaId,PrecoMedio,DataValidade,EstoqueAtual,ProdutoIdOrigem")] Produto produto)
        {
            if (id != produto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    produto.DataValidade = produto.DataValidade?.ToUniversalTime();
                    await _produtoService.UpdateProdutoAsync(produto);

                    // --- INÍCIO DO CONSERTO (GATILHO DE ALERTA) ---
                    // Como o estoque pode ter mudado, rodamos a verificação de alertas
                    // (Não é necessário rodar a previsão, pois ela se baseia em vendas, não em estoque)
                    await _alertaService.GerarAlertasAutomaticos();
                    // --- FIM DO CONSERTO ---
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _produtoService.ProdutoExists(produto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(await _categoriaService.GetAllCategoriasAsync(), "Id", "Nome", produto.CategoriaId);
            return View(produto);
        }

        // GET: Produto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _produtoService.GetProdutoByIdAsync(id.Value);
            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // POST: Produto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _produtoService.DeleteProdutoAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
