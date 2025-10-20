using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;

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
            // Carrega todos os produtos sem incluir vendas diretamente para evitar o erro CS1061
            var produtos = await _context.Produtos.ToListAsync();

            foreach (var produto in produtos)
            {
                // Ruptura Imediata: estoque atual menor que demanda prevista para próximos 7 dias.
                var previsao7Dias = await _context.Previsoes
                                                .Where(p => p.ProdutoId == produto.Id && p.IntervaloPrevisao == 7)
                                                .OrderByDescending(p => p.DataCalculo)
                                                .FirstOrDefaultAsync();
                if (previsao7Dias != null && produto.EstoqueAtual < previsao7Dias.DemandaPrevista)
                {
                    await CreateOrUpdateAlerta(produto.Id, "Ruptura Imediata", $"Estoque atual ({produto.EstoqueAtual}) é menor que a demanda prevista para os próximos 7 dias ({previsao7Dias.DemandaPrevista}).");
                }

                // Excesso de Estoque: estoque atual superior à demanda prevista para próximos 30 dias multiplicada por fator (ex: 1,5x).
                var previsao30Dias = await _context.Previsoes
                                                .Where(p => p.ProdutoId == produto.Id && p.IntervaloPrevisao == 30)
                                                .OrderByDescending(p => p.DataCalculo)
                                                .FirstOrDefaultAsync();
                if (previsao30Dias != null && produto.EstoqueAtual > (previsao30Dias.DemandaPrevista * 1.5))
                {
                    await CreateOrUpdateAlerta(produto.Id, "Excesso de Estoque", $"Estoque atual ({produto.EstoqueAtual}) é superior a 1.5x a demanda prevista para os próximos 30 dias ({previsao30Dias.DemandaPrevista}).");
                }

                // Produto Parado: sem vendas nos últimos 60 dias.
                // Busca a última venda diretamente do contexto de Vendas
                var ultimaVenda = await _context.Vendas.Where(v => v.ProdutoId == produto.Id).OrderByDescending(v => v.DataVenda).FirstOrDefaultAsync();
                if (ultimaVenda == null || (DateTime.Now - ultimaVenda.DataVenda).TotalDays > 60)
                {
                    await CreateOrUpdateAlerta(produto.Id, "Produto Parado", $"Produto sem vendas há mais de 60 dias.");
                }

                // Produto Vencido: data de validade expirou.
                if (produto.DataValidade.HasValue && produto.DataValidade.Value < DateTime.Today)
                {
                    await CreateOrUpdateAlerta(produto.Id, "Produto Vencido", $"Produto com data de validade expirada em {produto.DataValidade.Value.ToShortDateString()}.");
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task CreateOrUpdateAlerta(int produtoId, string tipo, string mensagem)
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
                    DataGeracao = DateTime.Now,
                    Resolvido = false
                });
            }
            else
            {
                alertaExistente.Mensagem = mensagem; // Atualiza a mensagem se o alerta já existe
                alertaExistente.DataGeracao = DateTime.Now; // Atualiza a data de geração
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
