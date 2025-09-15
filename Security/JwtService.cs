using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MercadinhoSaoGeraldo.Api.Security
{
    public class JwtService
    {
        private readonly string _issuer, _audience, _key;
        public JwtService(string issuer, string audience, string key)
        { _issuer = issuer; _audience = audience; _key = key; }


        public string CreateAccessToken(Guid userId, string email, string role, int value = 1)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role)
            };


            var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            SecurityAlgorithms.HmacSha256);


            var token = new JwtSecurityToken(_issuer, _audience, claims,
            expires: DateTime.UtcNow.AddDays(value),
            signingCredentials: creds);


            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public string CreateRefreshToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}