namespace AutoPartShop.Application.DTOs.PartDtos;

public class ProductMediaDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";   // image, video
    public string? AltText { get; set; }
    public string? FileName { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public Guid? VariantId { get; set; }
}

public class SaveProductMediaRequest
{
    /// <summary>Either an uploaded file URL (from POST /api/v1/files) or an external URL (e.g. YouTube).</summary>
    public string Url { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";   // image, video
    public string? AltText { get; set; }
    public string? FileName { get; set; }
    public bool IsPrimary { get; set; }
    public Guid? VariantId { get; set; }
}

public class ReorderProductMediaRequest
{
    /// <summary>Media ids in the desired display order; SortOrder is assigned by index.</summary>
    public List<Guid> OrderedIds { get; set; } = new();
}
