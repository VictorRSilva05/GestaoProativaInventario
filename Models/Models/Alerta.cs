using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoProativaInventario.Models
{
    public class Alerta
    {
        public int Id { get; set; }

        [ForeignKey("Produto")]
        public int ProdutoId { get; set; }
        public Produto Produto { get; set; }

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } // ruptura, excesso, inativo, vencido

        [Required]
        [StringLength(500)]
        public string Mensagem { get; set; }

        public DateTime DataGeracao { get; set; } = DateTime.UtcNow;

        public bool Resolvido { get; set; } = false;
    }
}
