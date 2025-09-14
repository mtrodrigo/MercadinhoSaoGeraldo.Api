using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Dtos;
using MercadinhoSaoGeraldo.Api.Domain;
using System;
using System.Linq;


namespace MercadinhoSaoGeraldo.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AdminUsersController(AppDbContext db) { _db = db; }


        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;


            var query = _db.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(term) || (u.Nome != null && u.Nome.ToLower().Contains(term)));
            }


            var total = await query.CountAsync();
            var items = await query.OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.Nome, u.Role, u.CreatedAt })
            .ToListAsync();


            return Ok(new { total, page, pageSize, items });
        }


        [HttpGet("{userId:guid}/contact")]
        public async Task<IActionResult> GetContact(Guid userId)
        {
            var d = await _db.UserDetails.FindAsync(userId);
            if (d is null) return NotFound();
            return Ok(new ContactDto(d.Telefone, d.Cep, d.Logradouro, d.Numero, d.Complemento, d.Bairro, d.Cidade, d.Uf));
        }


        [HttpPut("{userId:guid}/contact")]
        public async Task<IActionResult> UpsertContact(Guid userId, [FromBody] ContactDto dto)
        {
            var exists = await _db.Users.AnyAsync(u => u.Id == userId);
            if (!exists) return NotFound("Usuário inexistente.");


            var d = await _db.UserDetails.FindAsync(userId);
            if (d is null)
            {
                d = new AppUserDetail
                {
                    UserId = userId,
                    Telefone = dto.Telefone,
                    Cep = dto.Cep,
                    Logradouro = dto.Logradouro,
                    Numero = dto.Numero,
                    Complemento = dto.Complemento,
                    Bairro = dto.Bairro,
                    Cidade = dto.Cidade,
                    Uf = dto.Uf
                };
                _db.UserDetails.Add(d);
            }
            else
            {
                d.Telefone = dto.Telefone;
                d.Cep = dto.Cep;
                d.Logradouro = dto.Logradouro;
                d.Numero = dto.Numero;
                d.Complemento = dto.Complemento;
                d.Bairro = dto.Bairro;
                d.Cidade = dto.Cidade;
                d.Uf = dto.Uf;
            }
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}