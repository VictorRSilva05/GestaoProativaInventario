using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoProativaInventario.Models
{
    public class Produto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(200, ErrorMessage = "O nome do produto não pode exceder 200 caracteres.")]
        public string Nome { get; set; }

        [StringLength(50, ErrorMessage = "O código de barras não pode exceder 50 caracteres.")]
        public string? CodigoBarras { get; set; }

        [ForeignKey("Categoria")]
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecoMedio { get; set; }

        public DateTime? DataValidade { get; set; }

        public int EstoqueAtual { get; set; }

        // Propriedade para armazenar o ID do produto do ERP/CSV
        public int ProdutoIdOrigem { get; set; }

        [NotMapped] // Não mapear para o banco de dados, será preenchido via serviço
        public DateTime? UltimaDataVenda { get; set; }
    }
}
