namespace AutoPartShop.Infrastructure.Services.Embedding;

/// <summary>Bound from the "Embedding" appsettings section.</summary>
public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Provider { get; set; } = "openai";
    /// <summary>Host root of an OpenAI-compatible API, e.g. https://api.openai.com or http://localhost:11434. Empty = disabled.</summary>
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-3-small";
    /// <summary>Must match the vector(N) column dimension (default 1536).</summary>
    public int Dimensions { get; set; } = 1536;
}
