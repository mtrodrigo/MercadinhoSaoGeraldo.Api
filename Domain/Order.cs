using System;

namespace MercadinhoSaoGeraldo.Api.Domain
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "Criado";
        public DateTimeOffset CreatedAt { get; set; }


        public List<OrderItem> Itens { get; set; } = new();
    }

    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnit { get; set; }
    }
}