using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;

namespace GestaoProativaInventario.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryViewModel> GetDashboardSummary()
        {
            var totalProdutos = await _context.Produtos.CountAsync();
            var totalVendas = await _context.Vendas.SumAsync(v => v.Quantidade);
            var alertasAtivos = await _context.Alertas.CountAsync(a => !a.Resolvido);

            // Cálculo da Taxa de Ruptura (exemplo simplificado)
            // Para um cálculo mais preciso, seria necessário um histórico de estoque e vendas diárias
            var produtosComRuptura = await _context.Alertas
                                                .Where(a => a.Tipo == "Ruptura Imediata" && !a.Resolvido)
                                                .Select(a => a.ProdutoId)
                                                .Distinct()
                                                .CountAsync();
            var taxaRuptura = totalProdutos > 0 ? (double)produtosComRuptura / totalProdutos * 100 : 0;

            // Cálculo do Giro de Estoque (exemplo simplificado)
            // Giro de Estoque = Custo das Mercadorias Vendidas / Estoque Médio
            // Aqui usaremos Vendas Totais / Estoque Atual Total como proxy
            var estoqueAtualTotal = await _context.Produtos.SumAsync(p => p.EstoqueAtual);
            var giroEstoque = totalVendas > 0 ? (double)estoqueAtualTotal / totalVendas * 365 : 0; // Em dias

            // Custo de Manutenção de Estoque (exemplo simplificado)
            // Um cálculo mais real envolveria custos de capital, armazenagem, obsolescência, etc.
            // Aqui, um valor fixo por unidade em estoque como exemplo
            var custoManutencaoPorUnidade = 0.50m; // R$ 0.50 por unidade
            var custoManutencaoEstoque = estoqueAtualTotal * custoManutencaoPorUnidade;

            return new DashboardSummaryViewModel
            {
                TotalProdutos = totalProdutos,
                TotalVendas = totalVendas,
                AlertasAtivos = alertasAtivos,
                TaxaRuptura = taxaRuptura,
                GiroEstoque = giroEstoque,
                CustoManutencaoEstoque = custoManutencaoEstoque
            };
        }

        public async Task<List<Alerta>> GetTopAlertas(int count = 5)
        {
            return await _context.Alertas
                                .Where(a => !a.Resolvido)
                                .Include(a => a.Produto)
                                .OrderByDescending(a => a.DataGeracao)
                                .Take(count)
                                .ToListAsync();
        }

        public async Task<List<object[]>> GetAlertsDistributionChartData()
        {
            var chartData = new List<object[]>();
            chartData.Add(new object[] { "Tipo de Alerta", "Quantidade" });

            var alertsByType = await _context.Alertas
                                            .Where(a => !a.Resolvido)
                                            .GroupBy(a => a.Tipo)
                                            .Select(g => new { Type = g.Key, Count = g.Count() })
                                            .ToListAsync();

            foreach (var item in alertsByType)
            {
                chartData.Add(new object[] { item.Type, item.Count });
            }

            return chartData;
        }

        public async Task<List<object[]>> GetStockTurnoverByCategoryChartData()
        {
            var chartData = new List<object[]>();
            chartData.Add(new object[] { "Categoria", "Giro de Estoque (dias)" });

            // Simplificação: Cálculo de giro de estoque por categoria baseado em vendas médias
            // Um cálculo mais robusto exigiria dados de estoque ao longo do tempo.
            var turnoverData = await _context.Categorias
                                            .Select(c => new
                                            {
                                                CategoryName = c.Nome,
                                                AverageDailySales = _context.Vendas
                                                                            .Where(v => v.Produto.CategoriaId == c.Id)
                                                                            .GroupBy(v => v.DataVenda.Date)
                                                                            .Select(g => g.Sum(v => v.Quantidade))
                                                                            .Average()
                                            })
                                            .ToListAsync();

            foreach (var item in turnoverData)
            {
                // Assumindo um estoque médio e calculando um giro fictício para demonstração
                // O cálculo real de giro de estoque é mais complexo e depende de dados de estoque e custo
                var giro = item.AverageDailySales > 0 ? (decimal)365 / (decimal)item.AverageDailySales : 0;
                chartData.Add(new object[] { item.CategoryName, (double)Math.Round(giro, 2) });
            }

            return chartData;
        }
    }
}



public class DashboardSummaryViewModel
{
    public int TotalProdutos { get; set; }
    public int TotalVendas { get; set; }
    public int AlertasAtivos { get; set; }
    public double TaxaRuptura { get; set; }
    public double GiroEstoque { get; set; }
    public decimal CustoManutencaoEstoque { get; set; }
}

