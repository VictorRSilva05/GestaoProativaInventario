using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;

namespace GestaoProativaInventario.Services
{
    public class ProdutoService
    {
        private readonly ApplicationDbContext _context;

        public ProdutoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Produto>> GetAllProdutosAsync()
        {
            var produtos = await _context.Produtos.Include(p => p.Categoria).ToListAsync();
            foreach (var produto in produtos)
            {
                produto.UltimaDataVenda = await GetLastSaleDateForProduct(produto.Id);
            }
            return produtos;
        }

        public async Task<Produto> GetProdutoByIdAsync(int id)
        {
            var produto = await _context.Produtos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produto != null)
            {
                produto.UltimaDataVenda = await GetLastSaleDateForProduct(produto.Id);
            }
            return produto;
        }

        public async Task AddProdutoAsync(Produto produto)
        {
            _context.Add(produto);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProdutoAsync(Produto produto)
        {
            _context.Update(produto);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProdutoAsync(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
            {
                return; // Produto não encontrado
            }

            // Verificar se o produto está vinculado a alguma venda
            var hasSales = await _context.Vendas.AnyAsync(v => v.ProdutoId == id);
            if (hasSales)
            {
                throw new InvalidOperationException("Não é possível excluir um produto que possui vendas registradas.");
            }

            _context.Produtos.Remove(produto);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProdutoExists(int id)
        {
            return await _context.Produtos.AnyAsync(e => e.Id == id);
        }

        public async Task<DateTime?> GetLastSaleDateForProduct(int productId)
        {
            return await _context.Vendas
                                 .Where(v => v.ProdutoId == productId)
                                 .OrderByDescending(v => v.DataVenda)
                                 .Select(v => (DateTime?)v.DataVenda)
                                 .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Produto>> SearchProdutosAsync(string searchString)
        {
            var produtosQuery = _context.Produtos.Include(p => p.Categoria).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                produtosQuery = produtosQuery.Where(p => p.Nome.Contains(searchString) ||
                                                       (p.CodigoBarras != null && p.CodigoBarras.Contains(searchString)) ||
                                                       p.Categoria.Nome.Contains(searchString));
            }

            var produtos = await produtosQuery.ToListAsync();
            foreach (var produto in produtos)
            {
                produto.UltimaDataVenda = await GetLastSaleDateForProduct(produto.Id);
            }
            return produtos;
        }
    }
}
