using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestaoProativaInventario.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome da categoria não pode exceder 100 caracteres.")]
        public string Nome { get; set; }

        [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
        public string? Descricao { get; set; }

        public bool Ativa { get; set; } = true;

        public ICollection<Produto> Produtos { get; set; }
    }
}
