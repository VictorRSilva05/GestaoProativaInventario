using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Buffers.Text;

namespace GestaoProativaInventario.Services
{
    public class AlertaService
    {
        private readonly ApplicationDbContext _context;

        public AlertaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Alerta>> GetAllAlertasAsync()
        {
            return await _context.Alertas.Include(a => a.Produto).ToListAsync();
        }

        public async Task GerarAlertasAutomaticos()
        {
            var produtos = await _context.Produtos.ToListAsync();

            // --- CONSERTO DE FUSO HORÁRIO (BUG 1) ---
            // Usamos UTC para todas as comparações de "agora" e "hoje"
            var dataAtualUtc = DateTime.UtcNow;
            var dataHojeUtc = dataAtualUtc.Date;

            foreach (var produto in produtos)
            {
                // --- CONSERTO DE LÓGICA (BUSCA) ---
                // Buscamos a previsão de 30 dias (a única que o ML.NET gera)
                var previsao30Dias = await _context.Previsoes
                        .Where(p => p.ProdutoId == produto.Id && p.IntervaloPrevisao == 30)
                        .OrderByDescending(p => p.DataCalculo)
                        .FirstOrDefaultAsync();

                // --- CONSERTO DE LÓGICA (RUPTURA IMEDIATA) ---
                // Removemos a busca pela previsão de 7 dias (que não existe)
                if (previsao30Dias != null)
                {
                    // Calculamos a previsão de 7 dias (pro-rata)
                    // Usamos 'm' (decimal) para garantir a precisão
                    var demandaPrevista7Dias = (previsao30Dias.DemandaPrevista / 30.0m) * 7.0m;

                    if (produto.EstoqueAtual < demandaPrevista7Dias)
                    {
                        // Passamos 'dataAtualUtc' para o método de criação
                        await CreateOrUpdateAlerta(produto.Id, "Ruptura Imediata", $"Estoque atual ({produto.EstoqueAtual}) é menor que a demanda prevista para os próximos 7 dias ({demandaPrevista7Dias:N2}).", dataAtualUtc);
                    }
                }

                // Excesso de Estoque (Lógica OK, apenas adicionamos 'm' para decimal)
                if (previsao30Dias != null && produto.EstoqueAtual > (previsao30Dias.DemandaPrevista * 1.5m))
                {
                    await CreateOrUpdateAlerta(produto.Id, "Excesso de Estoque", $"Estoque atual ({produto.EstoqueAtual}) é superior a 1.5x a demanda prevista para os próximos 30 dias ({previsao30Dias.DemandaPrevista}).", dataAtualUtc);
                }

                // Produto Parado: (CONSERTO DE FUSO HORÁRIO (BUG 2))
                var ultimaVenda = await _context.Vendas.Where(v => v.ProdutoId == produto.Id).OrderByDescending(v => v.DataVenda).FirstOrDefaultAsync();

                // Compara dataAtualUtc (UTC) com ultimaVenda.DataVenda (UTC)
                if (ultimaVenda == null || (dataAtualUtc - ultimaVenda.DataVenda).TotalDays > 60)
                {
                    await CreateOrUpdateAlerta(produto.Id, "Produto Parado", $"Produto sem vendas há mais de 60 dias.", dataAtualUtc);
                }

                // Produto Vencido: (CONSERTO DE FUSO HORÁRIO (BUG 3))
                // Compara DataValidade.Value (UTC) com dataHojeUtc (UTC)
                if (produto.DataValidade.HasValue && produto.DataValidade.Value < dataHojeUtc)
                {
                    await CreateOrUpdateAlerta(produto.Id, "Produto Vencido", $"Produto com data de validade expirada em {produto.DataValidade.Value.ToShortDateString()}.", dataAtualUtc);
                }
            }
            await _context.SaveChangesAsync();
        }

        // Alteramos o método para aceitar a data UTC
        private async Task CreateOrUpdateAlerta(int produtoId, string tipo, string mensagem, DateTime dataAtualUtc)
        {
            var alertaExistente = await _context.Alertas
                                                .FirstOrDefaultAsync(a => a.ProdutoId == produtoId && a.Tipo == tipo && !a.Resolvido);

            if (alertaExistente == null)
            {
                _context.Alertas.Add(new Alerta
                {
                    ProdutoId = produtoId,
                    Tipo = tipo,
                    Mensagem = mensagem,
                    // A linha do bug (93) foi removida
                    DataGeracao = dataAtualUtc, // <-- CONSERTO DE FUSO HORÁRIO (BUG 4)
                    Resolvido = false
                });
            }
            else
            {
                alertaExistente.Mensagem = mensagem;
                alertaExistente.DataGeracao = dataAtualUtc; // <-- CONSERTO DE FUSO HORÁRIO (BUG 5)
                _context.Alertas.Update(alertaExistente);
            }
        }
        public async Task MarcarAlertaComoResolvidoAsync(int id)
        {
            var alerta = await _context.Alertas.FindAsync(id);
            if (alerta != null)
            {
                alerta.Resolvido = true;
                _context.Alertas.Update(alerta);
                await _context.SaveChangesAsync();
            }
        }
    }
}