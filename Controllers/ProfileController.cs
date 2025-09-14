using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Dtos;
using MercadinhoSaoGeraldo.Api.Security;
using System;
using System.Security.Claims;

namespace MercadinhoSaoGeraldo.Api.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly byte[] _aesKey;
        public ProfileController(AppDbContext db, IConfiguration cfg)
        { _db = db; _aesKey = Convert.FromBase64String(cfg["AES_KEY_BASE64"]!); }


        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var id = GetUserId();
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound();
            string? cpf = string.IsNullOrEmpty(u.CpfEnc) ? null : AesGcmCrypto.Decrypt(u.CpfEnc, _aesKey);
            return Ok(new { u.Email, u.Nome, Cpf = cpf, u.Role, u.CreatedAt });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProfileDto dto)
        {
            var id = GetUserId();
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound();
            u.Nome = dto.Nome;
            u.CpfEnc = string.IsNullOrWhiteSpace(dto.Cpf) ? null : AesGcmCrypto.Encrypt(dto.Cpf, _aesKey);
            await _db.SaveChangesAsync();
            return NoContent();
        }


        [HttpGet("contact")]
        public async Task<IActionResult> GetContact()
        {
            var id = GetUserId();
            var d = await _db.UserDetails.FindAsync(id);
            return Ok(new ContactDto(d?.Telefone, d?.Cep, d?.Logradouro, d?.Numero, d?.Complemento, d?.Bairro, d?.Cidade, d?.Uf));
        }


        [HttpPut("contact")]
        public async Task<IActionResult> UpsertContact([FromBody] ContactDto dto)
        {
            var id = GetUserId();
            var d = await _db.UserDetails.FindAsync(id);
            if (d is null)
            {
                d = new Domain.AppUserDetail
                {
                    UserId = id,
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