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

        public async Task<List<object[]>> GetDemandForecastChartData(int productId, int daysToObserve = 90)
        {
            var chartData = new List<object[]>();
            chartData.Add(new object[] { "Data", "Vendas Reais", "Previsão de Demanda" });

            // Vendas Reais
            var salesData = await _context.Vendas
                                        .Where(v => v.ProdutoId == productId && v.DataVenda >= DateTime.UtcNow.Date.AddDays(-daysToObserve))
                                        .GroupBy(v => v.DataVenda.Date)
                                        .Select(g => new { Date = g.Key, TotalSales = g.Sum(v => v.Quantidade) })
                                        .OrderBy(x => x.Date)
                                        .ToListAsync();

            // Previsões
            var forecasts = await _context.Previsoes
                                        .Where(p => p.ProdutoId == productId && p.DataCalculo >= DateTime.UtcNow.Date.AddDays(-daysToObserve))
                                        .OrderBy(p => p.DataCalculo)
                                        .ToListAsync();

            var allDates = salesData.Select(s => s.Date)
                                    .Union(forecasts.Select(f => f.DataCalculo.Date))
                                    .Distinct()
                                    .OrderBy(d => d)
                                    .ToList();

            foreach (var date in allDates)
            {
                var sales = salesData.FirstOrDefault(s => s.Date == date)?.TotalSales ?? 0;
                var forecast = forecasts.FirstOrDefault(f => f.DataCalculo.Date == date)?.DemandaPrevista ?? 0;
                chartData.Add(new object[] { date.ToString("yyyy-MM-dd"), sales, forecast });
            }

            return chartData;
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
