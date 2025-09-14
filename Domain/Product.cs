using System;

namespace MercadinhoSaoGeraldo.Api.Domain
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = default!;
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
        public string? ImagemUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}