namespace AutoPartShop.Application.Interfaces;

/// <summary>
/// Generates embedding vectors for text via a configured (OpenAI-compatible) model.
/// Degrades gracefully: when not configured, <see cref="IsEnabled"/> is false and
/// <see cref="EmbedAsync"/> returns null so callers can fall back to keyword search.
/// </summary>
public interface IEmbeddingService
{
    bool IsEnabled { get; }
    string Model { get; }

    /// <summary>Returns the embedding vector for <paramref name="text"/>, or null when disabled / on failure.</summary>
    Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
