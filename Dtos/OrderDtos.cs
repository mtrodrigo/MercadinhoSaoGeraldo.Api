using System;
using System.Collections.Generic;


namespace MercadinhoSaoGeraldo.Api.Dtos
{
    public record OrderItemIn(Guid ProductId, int Quantidade);
    public record CreateOrderDto(List<OrderItemIn> Itens);
}