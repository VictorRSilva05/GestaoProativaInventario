using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoProativaInventario.Models
{
    public class Venda
    {
        public int Id { get; set; }

        [ForeignKey("Produto")]
        public int ProdutoId { get; set; }
        public Produto Produto { get; set; }

        [Required(ErrorMessage = "A data da venda é obrigatória.")]
        public DateTime DataVenda { get; set; }

        public DateTime? DataValidade { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        [Required(ErrorMessage = "O preço unitário é obrigatório.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O preço unitário deve ser maior que zero.")]
        public decimal PrecoUnitario { get; set; }

        [StringLength(200, ErrorMessage = "O nome do fornecedor não pode exceder 200 caracteres.")]
        public string? Fornecedor { get; set; }

        public int? EstoqueAtualNaVenda { get; set; } // Estoque no momento da importação da venda
    }
}
