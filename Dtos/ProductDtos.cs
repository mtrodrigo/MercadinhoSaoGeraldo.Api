namespace MercadinhoSaoGeraldo.Api.Dtos
{
    public record ProductCreateDto(string Nome, string? Descricao, decimal Preco, int Estoque);
    public record ProductUpdateDto(string Nome, string? Descricao, decimal Preco, int Estoque);
}