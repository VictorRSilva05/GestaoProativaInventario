using Microsoft.ML.Data;

namespace GestaoProativaInventario.Models.Models;

public class ModelOutput
{
    // O ML.NET colocará a previsão (ex: os próximos 30 dias)
    // em um array de floats.
    [ColumnName("ForecastedQuantities")]
    public float[] ForecastedQuantities { get; set; }
}