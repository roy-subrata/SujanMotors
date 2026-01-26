using AutoPartsShop.Domain.Entities;


public interface ICodeGenerateService
{
    Task<string> GenerateAsync(string prefix, CancellationToken cancellationToken = default, int minDigits = 3);
    Task SaveGenerateCodeAsync(string prefix, CancellationToken cancellationToken = default);

}

