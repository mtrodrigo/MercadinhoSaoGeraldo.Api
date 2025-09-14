using FluentValidation;
using MercadinhoSaoGeraldo.Api.Dtos;


public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.Nome).NotEmpty();
    }
}


public class ProductCreateValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateValidator()
    {
        RuleFor(x => x.Nome).NotEmpty();
        RuleFor(x => x.Preco).GreaterThan(0);
        RuleFor(x => x.Estoque).GreaterThanOrEqualTo(0);
    }
}


public class ContactDtoValidator : AbstractValidator<ContactDto>
{
    public ContactDtoValidator()
    {
        RuleFor(x => x.Telefone)
        .Matches(@"^\+?\d{10,15}$")
        .When(x => !string.IsNullOrWhiteSpace(x.Telefone))
        .WithMessage("Telefone deve ter 10-15 dígitos, opcionalmente com DDI.");
        RuleFor(x => x.Cep)
        .Matches(@"^\d{5}-?\d{3}$")
        .When(x => !string.IsNullOrWhiteSpace(x.Cep))
        .WithMessage("CEP no formato 99999-999 ou 99999999.");
        RuleFor(x => x.Uf)
        .Length(2)
        .When(x => !string.IsNullOrWhiteSpace(x.Uf))
        .WithMessage("UF deve ter 2 letras.");
    }
}