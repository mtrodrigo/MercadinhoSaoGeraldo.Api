namespace MercadinhoSaoGeraldo.Api.Dtos
{
    public record ContactDto(
    string? Telefone,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf
    );
}