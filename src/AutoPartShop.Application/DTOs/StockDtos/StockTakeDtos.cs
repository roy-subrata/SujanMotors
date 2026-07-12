namespace AutoPartShop.Application.DTOs.StockDtos;

public class CreateStockTakeRequest
{
    public Guid WarehouseId { get; set; }
    public Guid? CategoryId { get; set; }  // Optional cycle-count scope
    public string Notes { get; set; } = string.Empty;
}

public class RecordStockTakeCountsRequest
{
    public List<StockTakeCountEntry> Counts { get; set; } = new();
}

public class StockTakeCountEntry
{
    public Guid LineId { get; set; }
    /// <summary>Physically counted quantity in base units; null clears an earlier count.</summary>
    public int? CountedQuantity { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class StockTakeResponse
{
    public Guid Id { get; set; }
    public string StockTakeNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SnapshotDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string CompletedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public int CountedLines { get; set; }
    public int VarianceLines { get; set; }       // counted lines where variance != 0
    public decimal TotalVarianceValue { get; set; } // Σ variance × unit cost (negative = shrinkage)
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class StockTakeDetailResponse : StockTakeResponse
{
    public List<StockTakeLineResponse> Lines { get; set; } = new();
}

public class StockTakeLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public int? Variance { get; set; }
    public decimal UnitCost { get; set; }
    public decimal? VarianceValue { get; set; }
    public string CountedBy { get; set; } = string.Empty;
    public DateTime? CountedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class ApproveStockTakeResponse
{
    public Guid Id { get; set; }
    public string StockTakeNumber { get; set; } = string.Empty;
    public int AdjustmentsApplied { get; set; }
    public int LinesUnchanged { get; set; }      // counted, zero variance
    public int LinesSkippedUncounted { get; set; }
    public decimal TotalVarianceValue { get; set; }
    /// <summary>Lines whose lot records couldn't fully mirror the adjustment (level still correct).</summary>
    public List<string> LotSyncWarnings { get; set; } = new();
}
