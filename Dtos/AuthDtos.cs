namespace MercadinhoSaoGeraldo.Api.Dtos
{
    public record RegisterDto(string Email, string Password, string Nome, string? Cpf);
    public record LoginDto(string Email, string Password);
    public record TokenResponse(string AccessToken, string RefreshToken);
}