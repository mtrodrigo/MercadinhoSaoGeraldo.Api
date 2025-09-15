using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Domain;

namespace MercadinhoSaoGeraldo.Api.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<AppUserDetail> UserDetails => Set<AppUserDetail>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<AppUser>(e =>
            {
                e.ToTable("app_users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(x => x.Email).HasColumnName("email").IsRequired();
                e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
                e.Property(x => x.Role).HasColumnName("role").IsRequired();
                e.Property(x => x.Nome).HasColumnName("nome");
                e.Property(x => x.CpfEnc).HasColumnName("cpf_enc");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                e.HasOne(x => x.Detail)
                 .WithOne(d => d.User)
                 .HasForeignKey<AppUserDetail>(d => d.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<AppUserDetail>(e =>
            {
                e.ToTable("app_user_details");
                e.HasKey(x => x.UserId);
                e.Property(x => x.UserId).HasColumnName("user_id").ValueGeneratedNever();
                e.Property(x => x.Telefone).HasColumnName("telefone");
                e.Property(x => x.Cep).HasColumnName("cep");
                e.Property(x => x.Logradouro).HasColumnName("logradouro");
                e.Property(x => x.Numero).HasColumnName("numero");
                e.Property(x => x.Complemento).HasColumnName("complemento");
                e.Property(x => x.Bairro).HasColumnName("bairro");
                e.Property(x => x.Cidade).HasColumnName("cidade");
                e.Property(x => x.Uf).HasColumnName("uf");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });

            b.Entity<Product>(e =>
            {
                e.ToTable("products");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Nome).HasColumnName("nome").IsRequired();
                e.Property(x => x.Descricao).HasColumnName("descricao");
                e.Property(x => x.Preco).HasColumnName("preco").HasColumnType("numeric(12,2)");
                e.Property(x => x.Estoque).HasColumnName("estoque");
                e.Property(x => x.ImagemUrl).HasColumnName("imagem_url");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });

            b.Entity<Order>(e =>
            {
                e.ToTable("orders");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Total).HasColumnName("total").HasColumnType("numeric(12,2)");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.OrderId);
            });

            b.Entity<OrderItem>(e =>
            {
                e.ToTable("order_items");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(x => x.OrderId).HasColumnName("order_id");
                e.Property(x => x.ProductId).HasColumnName("product_id");
                e.Property(x => x.Quantidade).HasColumnName("quantidade");
                e.Property(x => x.PrecoUnit).HasColumnName("preco_unit").HasColumnType("numeric(12,2)");
            });
        }
    }
}
