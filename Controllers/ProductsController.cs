using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Domain;
using MercadinhoSaoGeraldo.Api.Dtos;
using MercadinhoSaoGeraldo.Api.Storage;
using System;

namespace MercadinhoSaoGeraldo.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SupabaseStorageService _storage;
        public ProductsController(AppDbContext db, SupabaseStorageService storage)
        { _db = db; _storage = storage; }


        [HttpGet]
        public async Task<IActionResult> GetAll() =>
        Ok(await _db.Products.AsNoTracking().OrderBy(p => p.Nome).ToListAsync());


        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var p = await _db.Products.FindAsync(id);
            return p is null ? NotFound() : Ok(p);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var p = new Product { Nome = dto.Nome, Descricao = dto.Descricao, Preco = dto.Preco, Estoque = dto.Estoque };
            _db.Products.Add(p);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateDto dto)
        {
            var p = await _db.Products.FindAsync(id);
            if (p is null) return NotFound();
            p.Nome = dto.Nome; p.Descricao = dto.Descricao; p.Preco = dto.Preco; p.Estoque = dto.Estoque;
            await _db.SaveChangesAsync();
            return NoContent();
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p is null) return NotFound();
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("{id:guid}/image")]
        public async Task<IActionResult> Upload(Guid id, IFormFile file)
        {
            var p = await _db.Products.FindAsync(id);
            if (p is null) return NotFound();
            if (file == null || file.Length == 0) return BadRequest("Arquivo inválido.");
            if (file.Length > 2_000_000) return BadRequest("Imagem deve ter até 2MB.");
            if (file.ContentType is not ("image/jpeg" or "image/png")) return BadRequest("Apenas JPEG/PNG.");


            using var stream = file.OpenReadStream();
            var url = await _storage.UploadProductImageAsync($"{id}-{file.FileName}", stream, file.ContentType);
            p.ImagemUrl = url;
            await _db.SaveChangesAsync();
            return Ok(new { url });
        }
    }
}