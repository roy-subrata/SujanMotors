using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutoPartShop.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Services.Embedding;

/// <summary>
/// Calls an OpenAI-compatible <c>/v1/embeddings</c> endpoint (OpenAI cloud, Azure OpenAI gateways,
/// local Ollama, etc.). Configured via the "Embedding" appsettings section. When BaseUrl is blank
/// the service is disabled and <see cref="EmbedAsync"/> returns null so callers fall back to keyword search.
/// </summary>
public class OpenAiCompatibleEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiCompatibleEmbeddingService> _logger;
    private readonly EmbeddingOptions _options;

    public OpenAiCompatibleEmbeddingService(HttpClient http, IConfiguration config, ILogger<OpenAiCompatibleEmbeddingService> logger)
    {
        _http = http;
        _logger = logger;
        _options = config.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>() ?? new EmbeddingOptions();
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.BaseUrl);
    public string Model => _options.Model;

    public async Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var url = BuildEmbeddingsUrl(_options.BaseUrl);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new EmbeddingRequest { Model = _options.Model, Input = text })
            };
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Embedding request failed: {Status} {Reason}", (int)response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
            var vector = payload?.Data?.FirstOrDefault()?.Embedding;
            if (vector is null || vector.Length == 0)
            {
                _logger.LogWarning("Embedding response contained no vector");
                return null;
            }

            if (vector.Length != _options.Dimensions)
                _logger.LogWarning("Embedding dimension {Actual} differs from configured {Expected}; storing/searching may fail",
                    vector.Length, _options.Dimensions);

            return vector;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding generation failed");
            return null;
        }
    }

    /// <summary>Normalises the configured host to a single "{root}/v1/embeddings" URL (handles a trailing /v1).</summary>
    private static string BuildEmbeddingsUrl(string baseUrl)
    {
        var root = baseUrl.TrimEnd('/');
        if (root.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            root = root[..^3].TrimEnd('/');
        return $"{root}/v1/embeddings";
    }

    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("input")] public string Input { get; set; } = string.Empty;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")] public List<EmbeddingDatum>? Data { get; set; }
    }

    private sealed class EmbeddingDatum
    {
        [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
    }
}
