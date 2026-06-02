using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Infrastructure.Services.Embedding;

/// <summary>
/// Builds the text that gets embedded for a product. Kept to a small set of "basic" fields for now;
/// add more (Tags, RichDescription, vehicle compatibility, …) here later without touching callers.
/// </summary>
public static class EmbeddingTextBuilder
{
    public static string Build(Product p)
    {
        var parts = new[]
        {
            p.Name,
            p.PartNumber?.Value,
            p.OemNumber,
            p.Description,
            p.Category?.Name,
            p.Brand?.Name
        };

        return string.Join("\n", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
