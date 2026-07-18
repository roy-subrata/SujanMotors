namespace AutoPartShop.Application.DTOs.TillSessionDtos;

public class OpenTillSessionRequest
{
    public string TerminalLabel { get; set; } = string.Empty;
    public decimal OpeningFloat { get; set; }
    public string? ShiftLabel { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class RecordCashDropRequest
{
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloseTillSessionRequest
{
    public decimal CountedAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class TillSessionResponse
{
    public Guid Id { get; set; }
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string TerminalLabel { get; set; } = string.Empty;
    public string? ShiftLabel { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal? ClosingCountedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal CashSalesTotal { get; set; }
    public decimal CashRefundsTotal { get; set; }
    public decimal CashDropsTotal { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal OverShortAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<TillCashDropResponse> CashDrops { get; set; } = new();
}

public class TillCashDropResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime DroppedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}
