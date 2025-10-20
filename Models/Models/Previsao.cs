using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoProativaInventario.Models
{
    public class Previsao
    {
        public int Id { get; set; }

        [ForeignKey("Produto")]
        public int ProdutoId { get; set; }
        public Produto Produto { get; set; }

        [Required]
        public int PeriodoObservacao { get; set; } // Janela de observação (ex: 7, 14, 30 dias)

        [Required]
        public int IntervaloPrevisao { get; set; } // Intervalo de previsão (ex: 15 ou 30 dias)

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MediaMovel { get; set; }

        [Required]
        public int DemandaPrevista { get; set; }

        [Required]
        public DateTime DataCalculo { get; set; }
    }
}
