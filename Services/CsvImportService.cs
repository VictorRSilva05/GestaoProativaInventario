using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoProativaInventario.Services
{
    public class CsvImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly PrevisaoService _previsaoService;
        private readonly AlertaService _alertaService;

        public CsvImportService(ApplicationDbContext context, PrevisaoService previsaoService, AlertaService alertaService)
        {
            _context = context;
            _previsaoService = previsaoService;
            _alertaService = alertaService;
        }

        public async Task ImportSalesData(Stream fileStream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                IgnoreBlankLines = true
            };

            using (var reader = new StreamReader(fileStream))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<CsvSaleRecord>().ToList();

                foreach (var record in records)
                {
                    // Processar Categoria
                    var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Nome == record.Categoria);
                    if (categoria == null)
                    {
                        categoria = new Categoria { Nome = record.Categoria ?? "Não classificado", Descricao = "Categoria criada via importação CSV" };
                        _context.Categorias.Add(categoria);
                        await _context.SaveChangesAsync();
                    }

                    // Processar Produto
                    var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.ProdutoIdOrigem == record.ProdutoId);
                    if (produto == null)
                    {
                        produto = new Produto
                        {
                            ProdutoIdOrigem = record.ProdutoId,
                            Nome = record.Produto,
                            CodigoBarras = record.CodigoBarras,
                            CategoriaId = categoria.Id,
                            PrecoMedio = record.PrecoUnitario,
                            DataValidade = record.DataValidade.HasValue
                            ? DateTime.SpecifyKind(record.DataValidade.Value, DateTimeKind.Utc)
                            : null,
                            EstoqueAtual = record.EstoqueAtual ?? 0 // Se não vier no CSV, assume 0
                        };
                        _context.Produtos.Add(produto);
                    }
                    else
                    {
                        // Atualizar dados do produto existente
                        produto.Nome = record.Produto;
                        produto.CodigoBarras = record.CodigoBarras;
                        produto.CategoriaId = categoria.Id;
                        produto.PrecoMedio = record.PrecoUnitario; // Pode ser uma média ou o último preço
                        produto.DataValidade = record.DataValidade.HasValue
                        ? DateTime.SpecifyKind(record.DataValidade.Value, DateTimeKind.Utc)
                        : null;
                        produto.EstoqueAtual = record.EstoqueAtual ?? produto.EstoqueAtual; // Atualiza se vier no CSV
                        _context.Produtos.Update(produto);
                    }
                    await _context.SaveChangesAsync();

                    var venda = new Venda
                    {
                        ProdutoId = produto.Id,

                        DataVenda = DateTime.SpecifyKind(record.DataVenda, DateTimeKind.Utc),

                        // --- INÍCIO DA MUDANÇA (Linha 87) ---
                        // Verifica se tem valor. Se sim, converte. Se não, salva como null.
                        DataValidade = record.DataValidade.HasValue
                         ? DateTime.SpecifyKind(record.DataValidade.Value, DateTimeKind.Utc)
                         : null,
                        // --- FIM DA MUDANÇA ---

                        Quantidade = record.Quantidade,
                        PrecoUnitario = record.PrecoUnitario,
                        Fornecedor = record.Fornecedor,
                        EstoqueAtualNaVenda = record.EstoqueAtual
                    };
                    _context.Vendas.Add(venda);
                    await _context.SaveChangesAsync();
                    // Gerar previsão para o produto após a importação de suas vendas
                    await _previsaoService.GerarPrevisoesDemanda(produto.Id, 30, 30); // Padrão: 30 dias de observação, 30 dias de previsão
                }
                // Após processar todas as vendas, gerar alertas
                await _alertaService.GerarAlertasAutomaticos();
            }
        }
    }

    // Classe auxiliar para mapear os campos do CSV
    public class CsvSaleRecord
    {
        [CsvHelper.Configuration.Attributes.Name("produto_id")]
        public int ProdutoId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("codigo_barras")]
        public string? CodigoBarras { get; set; }

        [CsvHelper.Configuration.Attributes.Name("data_venda")]
        public DateTime DataVenda { get; set; }

        [CsvHelper.Configuration.Attributes.Name("data_validade")]
        public DateTime? DataValidade { get; set; }

        [CsvHelper.Configuration.Attributes.Name("produto")]
        public string Produto { get; set; }

        [CsvHelper.Configuration.Attributes.Name("quantidade")]
        public int Quantidade { get; set; }

        [CsvHelper.Configuration.Attributes.Name("preco_unitario")]
        public decimal PrecoUnitario { get; set; }

        [CsvHelper.Configuration.Attributes.Name("categoria")]
        public string? Categoria { get; set; }

        [CsvHelper.Configuration.Attributes.Name("fornecedor")]
        public string? Fornecedor { get; set; }

        [CsvHelper.Configuration.Attributes.Name("estoque_atual")]
        public int? EstoqueAtual { get; set; }
    }
}
