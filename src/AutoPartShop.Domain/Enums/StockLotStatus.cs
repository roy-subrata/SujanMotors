namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Inventory status of a <see cref="Entities.StockLot"/> (GRN spec): AVAILABLE = sellable;
/// DAMAGED / QUARANTINE = held, excluded from sale.
/// Member names are serialized as-is (global JsonStringEnumConverter, no naming policy) and match
/// the historical string values stored in the database (.HasConversion&lt;string&gt;() uses
/// enum.ToString() by default) — do not rename members without a data migration.
/// </summary>
public enum StockLotStatus
{
    AVAILABLE,
    DAMAGED,
    QUARANTINE
}
