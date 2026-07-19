namespace AutoPartShop.Application.DTOs.ReportDtos;

/// <summary>
/// Shared aging bucket columns for receivables/payables aging reports.
/// Buckets are mutually exclusive and sum to Total; only positive outstanding balances are included.
/// </summary>
public abstract class AgingRowDtoBase
{
    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal Total { get; set; }
}

/// <summary>One customer row of the receivables aging report, bucketed by invoice DueDate vs. AsOfDate.</summary>
public class ReceivablesAgingRowDto : AgingRowDtoBase
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
}

/// <summary>One supplier row of the payables aging report, bucketed by purchase order age vs. AsOfDate.</summary>
public class PayablesAgingRowDto : AgingRowDtoBase
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
}

/// <summary>Grand totals across the whole filtered aging report, not just the current page.</summary>
public class AgingTotalsDto
{
    public int RowCount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal Total { get; set; }
}

/// <summary>One bucket of the expense report, grouped by day or category.</summary>
public class ExpenseReportRowDto
{
    public string GroupKey { get; set; } = "";
    public int ExpenseCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// One label/value line of the Profit &amp; Loss statement export. The P&amp;L report has no
/// stored procedure of its own — it renders IFinancialSummaryService's FinancialSummaryResponse
/// as a flat statement, keeping figures identical to the dashboard.
/// </summary>
public class StatementLineDto
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
}

/// <summary>
/// Output/input VAT reconciliation for a period, from dbo.usp_Report_Vat.
/// Sales and Purchase figures are the actual stored Invoice/PurchaseOrder TaxAmount totals.
/// Credit-note figures are derived (TotalAmount × the caller's VatRatePercent, default 15) since
/// CustomerCreditNote has no stored per-transaction tax breakdown — see ReportQuery.VatRatePercent.
/// </summary>
public class VatReportDto
{
    public decimal SalesTaxableValue { get; set; }
    public decimal SalesVatAmount { get; set; }
    public int SalesInvoiceCount { get; set; }

    public decimal CreditTaxableValue { get; set; }
    public decimal CreditVatAmount { get; set; }

    public decimal PurchaseTaxableValue { get; set; }
    public decimal PurchaseVatAmount { get; set; }
    public int PurchaseOrderCount { get; set; }

    /// <summary>SalesVatAmount − CreditVatAmount − PurchaseVatAmount.</summary>
    public decimal NetVatPayable { get; set; }
}
