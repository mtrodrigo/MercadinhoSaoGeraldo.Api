using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Domain;
using MercadinhoSaoGeraldo.Api.Dtos;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mercadinho.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public OrdersController(AppDbContext db) { _db = db; }


        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


        // Cliente cria pedido com itens [{productId, quantidade}]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            if (dto.Itens == null || dto.Itens.Count == 0)
                return BadRequest("Informe ao menos 1 item.");


            var ids = dto.Itens.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
            if (products.Count != ids.Count) return BadRequest("Produto inexistente.");


            // Verificar estoque
            foreach (var it in dto.Itens)
            {
                var p = products.First(x => x.Id == it.ProductId);
                if (it.Quantidade <= 0) return BadRequest("Quantidade inválida.");
                if (p.Estoque < it.Quantidade)
                    return BadRequest($"Estoque insuficiente para {p.Nome}.");
            }

            using var tx = await _db.Database.BeginTransactionAsync();


            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = GetUserId(),
                Status = "Criado",
                Total = 0,
                Itens = new List<OrderItem>()
            };


            decimal total = 0m;
            foreach (var it in dto.Itens)
            {
                var p = products.First(x => x.Id == it.ProductId);
                p.Estoque -= it.Quantidade; // baixa de estoque
                var oi = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantidade = it.Quantidade,
                    PrecoUnit = p.Preco
                };
                order.Itens.Add(oi);
                total += p.Preco * it.Quantidade;
            }
            order.Total = total;


            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return CreatedAtAction(nameof(GetMine), new { }, new { order.Id, order.Total, order.Status });
        }


        // Cliente: lista meus pedidos (+ itens)
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var uid = GetUserId();
            var list = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == uid)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.Total,
                o.Status,
                o.CreatedAt,
                Itens = _db.OrderItems.Where(i => i.OrderId == o.Id)
            .Select(i => new { i.ProductId, i.Quantidade, i.PrecoUnit })
            .ToList()
            })
            .ToListAsync();
            return Ok(list);
        }


        // Admin: lista todos os pedidos
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
            var q = _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt);
            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new { o.Id, o.UserId, o.Total, o.Status, o.CreatedAt })
            .ToListAsync();
            return Ok(new { total, page, pageSize, items });
        }
    }
}