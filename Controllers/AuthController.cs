using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Domain;
using MercadinhoSaoGeraldo.Api.Dtos;
using MercadinhoSaoGeraldo.Api.Security;

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
            _db = db;
            _jwt = jwt;
            _aesKey = Convert.FromBase64String(cfg["AES_KEY_BASE64"]!);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = dto.Email?.Trim().ToLowerInvariant();
            var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email);
            if (exists) return new JsonResult(new { message = "Email já cadastrado." }) { StatusCode = StatusCodes.Status409Conflict };

            var hash = PasswordHasher.Hash(dto.Password);
            string? cpfEnc = string.IsNullOrWhiteSpace(dto.Cpf) ? null : AesGcmCrypto.Encrypt(dto.Cpf, _aesKey);

            var now = DateTime.UtcNow;
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = email!,
                PasswordHash = hash,
                Role = "Cliente",
                Nome = dto.Nome?.Trim(),
                CpfEnc = cpfEnc,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new JsonResult(new { message = "Registrado com sucesso.", id = user.Id, email = user.Email, nome = user.Nome, role = user.Role }) { StatusCode = StatusCodes.Status200OK };
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = dto.Email?.Trim().ToLowerInvariant();
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
            if (u is null || !PasswordHasher.Verify(dto.Password, u.PasswordHash))
                return Unauthorized(new { message = "Credenciais inválidas." });

            var access = _jwt.CreateAccessToken(u.Id, u.Email, u.Role);
            var refresh = _jwt.CreateRefreshToken();

            return Ok(new TokenResponse(access, refresh));
        }
    }
}
