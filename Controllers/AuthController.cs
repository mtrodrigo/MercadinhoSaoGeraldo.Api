using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Domain;
using MercadinhoSaoGeraldo.Api.Dtos;
using MercadinhoSaoGeraldo.Api.Security;
using System;


namespace Mercadinho.Api.Controllers
{
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
private readonly AppDbContext _db;
private readonly JwtService _jwt;
private readonly byte[] _aesKey;


public AuthController(AppDbContext db, JwtService jwt, IConfiguration cfg)
{
_db = db; _jwt = jwt;
_aesKey = Convert.FromBase64String(cfg["AES_KEY_BASE64"]!);
}


[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterDto dto)
{
if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
return Conflict("Email já cadastrado.");


var u = new AppUser
{
Email = dto.Email,
PasswordHash = PasswordHasher.Hash(dto.Password),
Role = "Cliente",
Nome = dto.Nome,
CpfEnc = string.IsNullOrWhiteSpace(dto.Cpf) ? null : AesGcmCrypto.Encrypt(dto.Cpf, _aesKey)
};
_db.Users.Add(u);
await _db.SaveChangesAsync();
return Ok(new { message = "Registrado com sucesso." });
}


[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
if (u is null || !PasswordHasher.Verify(dto.Password, u.PasswordHash))
return Unauthorized("Credenciais inválidas.");


var access = _jwt.CreateAccessToken(u.Id, u.Email, u.Role);
var refresh = _jwt.CreateRefreshToken(); // opcional persistir
return Ok(new TokenResponse(access, refresh));
}
}
}