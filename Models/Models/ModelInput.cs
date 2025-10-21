namespace GestaoProativaInventario.Models.Models;

// Em Models/ModelInput.cs
public class ModelInput
{
    // A data da venda. O nome não importa, mas o tipo sim.
    public DateTime DataVenda { get; set; }

    // O valor que queremos prever (demanda).
    // O ML.NET espera um float por padrão.
    public float Quantidade { get; set; }
}