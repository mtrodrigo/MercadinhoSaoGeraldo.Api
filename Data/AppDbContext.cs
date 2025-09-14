using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Domain;

namespace MercadinhoSaoGeraldo.Api.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<AppUser> Users => Set<AppUser>();


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<AppUser>(e =>
            {
                e.ToTable("app_users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Email).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.Role).IsRequired();
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                e.Property(x => x.CpfEnc).HasColumnName("cpf_enc");
                e.HasOne(x => x.Detail)
    .WithOne(d => d.User)
    .HasForeignKey<AppUserDetail>(d => d.UserId)
    .OnDelete(DeleteBehavior.Cascade);
            });


            b.Entity<AppUserDetail>(e =>
            {
                e.ToTable("app_user_details");
                e.HasKey(x => x.UserId);
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });


            b.Entity<Product>(e =>
            {
                e.ToTable("products");
                e.HasKey(x => x.Id);
                e.Property(x => x.Nome).IsRequired();
                e.Property(x => x.Preco).HasColumnType("numeric(12,2)");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });


            b.Entity<Order>(e =>
            {
                e.ToTable("orders");
                e.HasKey(x => x.Id);
                e.Property(x => x.Total).HasColumnType("numeric(12,2)");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.OrderId);
            });


            b.Entity<OrderItem>(e =>
            {
                e.ToTable("order_items");
                e.HasKey(x => x.Id);
                e.Property(x => x.PrecoUnit).HasColumnType("numeric(12,2)");
            });
        }
    }
}