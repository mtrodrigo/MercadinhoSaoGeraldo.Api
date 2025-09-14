using System;

namespace MercadinhoSaoGeraldo.Api.Domain
{
    public class AppUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = "Cliente";
        public string? CpfEnc { get; set; }
        public string? Nome { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }


        public AppUserDetail? Detail { get; set; }
    }
}