using Microsoft.EntityFrameworkCore;

namespace GestaoProativaInventario.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

                // Adicionar DbSets para os modelos aqui
        public DbSet<GestaoProativaInventario.Models.Produto> Produtos { get; set; }
        public DbSet<GestaoProativaInventario.Models.Categoria> Categorias { get; set; }
        public DbSet<GestaoProativaInventario.Models.Venda> Vendas { get; set; }
        public DbSet<GestaoProativaInventario.Models.Previsao> Previsoes { get; set; }
        public DbSet<GestaoProativaInventario.Models.Alerta> Alertas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configurações adicionais do modelo podem ser feitas aqui
        }
    }
}
