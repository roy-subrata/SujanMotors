using Microsoft.Data.SqlTypes;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Stored embedding vector for a product, used by semantic product search.
/// One row per product (unique ProductId). The vector is the embedding-model output
/// for a text built from the product's key fields. PartNumber/OemNumber are denormalised
/// here for cheap display without a join.
/// Persisted to a native SQL Server 2025 <c>vector(n)</c> column via <see cref="Embedding"/>.
/// </summary>
public class ProductEmbedding : AuditableEntity
{
    public Guid ProductId { get; private set; }
    public string PartNumber { get; private set; } = string.Empty;
    public string? OemNumber { get; private set; }
    /// <summary>Native SQL Server vector column (see EF config: <c>vector(1536)</c>).</summary>
    public SqlVector<float> Embedding { get; private set; }
    public string Model { get; private set; } = string.Empty;
    public int Dimensions { get; private set; }
    public string SourceText { get; private set; } = string.Empty;

    public Product? Product { get; set; }

    private ProductEmbedding() { }

    public static ProductEmbedding Create(Guid productId, float[] vector, string model,
        string sourceText, string partNumber, string? oemNumber)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));

        var entity = new ProductEmbedding { ProductId = productId };
        entity.Apply(vector, model, sourceText, partNumber, oemNumber);
        return entity;
    }

    public void Update(float[] vector, string model, string sourceText, string partNumber, string? oemNumber)
        => Apply(vector, model, sourceText, partNumber, oemNumber);

    private void Apply(float[] vector, string model, string sourceText, string partNumber, string? oemNumber)
    {
        if (vector is null || vector.Length == 0)
            throw new ArgumentException("Vector cannot be empty", nameof(vector));

        Embedding = new SqlVector<float>(vector);
        Dimensions = vector.Length;
        Model = model?.Trim() ?? string.Empty;
        SourceText = sourceText ?? string.Empty;
        PartNumber = partNumber?.Trim() ?? string.Empty;
        OemNumber = oemNumber?.Trim();
    }
}
