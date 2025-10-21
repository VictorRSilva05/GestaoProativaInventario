using GestaoProativaInventario.Data;
using GestaoProativaInventario.Models;
using GestaoProativaInventario.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

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
            // --- 1. CARREGAR OS DADOS DE TREINAMENTO ---
            var dataInicioObservacao = DateTime.UtcNow.Date.AddDays(-periodoObservacao);

            var vendas = await _context.Vendas
                .Where(v => v.ProdutoId == produtoId && v.DataVenda >= dataInicioObservacao)
                .OrderBy(v => v.DataVenda)
                .ToListAsync();

            if (!vendas.Any())
            {
                return; // Sem dados, sem previsão
            }

            var trainingData = vendas.Select(v => new ModelInput
            {
                DataVenda = v.DataVenda,
                Quantidade = v.Quantidade
            }).ToList();

            // --- 2. DEFINIR PARÂMETROS E VARIÁVEIS ---
            const int windowSize = 7; // O tamanho da janela que queremos para o ML.NET
            int demandaPrevista;
            double mediaVendasObservadas = vendas.Average(v => v.Quantidade);

            // --- 3. VERIFICAR SE TEMOS DADOS SUFICIENTES PARA O ML.NET ---

            if (trainingData.Count <= (2 * windowSize))
            {
                // --- FALLBACK: DADOS INSUFICIENTES PARA ML.NET ---
                // Usamos a Média Móvel Simples (o cálculo antigo)
                demandaPrevista = (int)Math.Round(mediaVendasObservadas * intervaloPrevisao);
            }
            else
            {
                // --- SUCESSO: TEMOS DADOS PARA O ML.NET ---
                var mlContext = new MLContext();
                var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

                var pipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(ModelOutput.ForecastedQuantities),
                    inputColumnName: nameof(ModelInput.Quantidade),
                    windowSize: windowSize, // Usando a variável
                    seriesLength: trainingData.Count,
                    trainSize: trainingData.Count,
                    horizon: intervaloPrevisao
                );

                var model = pipeline.Fit(dataView);

                var predictionEngine = model.CreateTimeSeriesEngine<ModelInput, ModelOutput>(mlContext);
                var forecast = predictionEngine.Predict();

                // Somar a previsão do ML.NET
                demandaPrevista = (int)Math.Round(forecast.ForecastedQuantities.Sum());
            }

            // --- 4. SALVAR A PREVISÃO (vinda do ML.NET ou do Fallback) ---
            var previsao = new Previsao
            {
                ProdutoId = produtoId,
                PeriodoObservacao = periodoObservacao,
                IntervaloPrevisao = intervaloPrevisao,
                MediaMovel = (decimal)mediaVendasObservadas, // Média real (para referência)
                DemandaPrevista = demandaPrevista,           // O resultado (do ML ou MMS)
                DataCalculo = DateTime.UtcNow
            };

            _context.Previsoes.Add(previsao);
            await _context.SaveChangesAsync();
        }
    }
}
