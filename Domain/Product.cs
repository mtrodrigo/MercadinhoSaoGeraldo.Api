// MercadinhoSaoGeraldo.Api/Domain/Product.cs
namespace MercadinhoSaoGeraldo.Api.Domain;

public class Product
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string? ImagemUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
