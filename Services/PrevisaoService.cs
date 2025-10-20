using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;

namespace GestaoProativaInventario.Services
{
    public class PrevisaoService
    {
        private readonly ApplicationDbContext _context;

        public PrevisaoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Previsao>> GetAllPrevisoesAsync()
        {
            return await _context.Previsoes.Include(p => p.Produto).ThenInclude(p => p.Categoria).ToListAsync();
        }

        public async Task GerarPrevisoesDemanda(int produtoId, int periodoObservacao, int intervaloPrevisao)
        {
            var vendas = await _context.Vendas
                                   .Where(v => v.ProdutoId == produtoId && v.DataVenda >= DateTime.UtcNow.AddDays(-periodoObservacao))
                                   .OrderBy(v => v.DataVenda)
                                   .ToListAsync();

            if (!vendas.Any())
            {
                // Não há vendas suficientes para gerar previsão
                return;
            }

            // Cálculo da Média Móvel Simples (MMS)
            double mediaMovel = vendas.Average(v => v.Quantidade);

            // Previsão para o intervalo futuro
            int demandaPrevista = (int)Math.Round(mediaMovel * intervaloPrevisao);

            var previsao = new Previsao
            {
                ProdutoId = produtoId,
                PeriodoObservacao = periodoObservacao,
                IntervaloPrevisao = intervaloPrevisao,
                MediaMovel = (decimal)mediaMovel,
                DemandaPrevista = demandaPrevista,
                DataCalculo = DateTime.UtcNow
            };

            _context.Previsoes.Add(previsao);
            await _context.SaveChangesAsync();
        }
    }
}
