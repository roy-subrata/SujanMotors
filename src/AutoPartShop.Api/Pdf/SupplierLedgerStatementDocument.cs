using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record SupplierLedgerStatementLine(
    DateTime TransactionDate,
    string TransactionType,
    string ReferenceNumber,
    string Description,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal RunningBalance);

public record SupplierLedgerStatementData(
    string SupplierName,
    string SupplierCode,
    string PeriodLabel,
    decimal TotalPurchases,
    decimal TotalPayments,
    decimal TotalRefunds,
    decimal AvailableAdvanceCredit,
    decimal CurrentBalance,
    IReadOnlyList<SupplierLedgerStatementLine> Entries);

/// <summary>
/// Supplier Ledger — the running-balance statement behind the procurement "Supplier Account
/// Summary" page. Distinct from <see cref="SupplierAccountStatementDocument"/> (payment history
/// only, from SupplierPaymentController): this shows every transaction type (purchase, payment,
/// refund, advance, cancellation) with a debit/credit/balance column, matching what
/// SupplierLedgerController's ledger entries already compute.
///
/// Totals (Total Purchases/Payments/Refunds/Advance Credit/Current Balance) are always all-time,
/// per GetLedgerSummaryAsync — only the entry list respects the requested date range. This mirrors
/// the existing Angular component's behaviour exactly, including that the running balance is
/// recomputed within the filtered window rather than carrying an opening balance from before it.
/// </summary>
public class SupplierLedgerStatementDocument : IDocument
{
    private readonly SupplierLedgerStatementData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public SupplierLedgerStatementDocument(SupplierLedgerStatementData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Supplier Ledger – {_data.SupplierName}",
        Author = _shop.Name,
        Subject = $"Supplier ledger statement for {_data.SupplierName}",
        CreationDate = DateTime.UtcNow
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            _theme.ApplyPage(page);

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // A date-range Period doesn't fit the header's narrow meta column (same issue already fixed on
    // CustomerAccountStatementDocument) — it goes next to the "Ledger" section heading instead,
    // where there's a full row's width to work with.
    private void ComposeHeader(IContainer container) =>
        new DocHeader(_theme, _shop, "Supplier Ledger",
        [
            new MetaField("No.", $"LED-{_data.SupplierCode}"),
            new MetaField("Date", DateTime.Now.ToString("dd MMM yyyy")),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeSupplier);
            col.Item().PaddingTop(DocTheme.Px(20)).Element(ComposeEntries);
        });
    }

    private void ComposeSupplier(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Supplier"));
            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.SupplierName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);
            if (!string.IsNullOrWhiteSpace(_data.SupplierCode))
                col.Item().PaddingTop(DocTheme.Px(4)).Element(c => InfoRow(c, "Code", _data.SupplierCode));
        });
    }

    private void ComposeEntries(IContainer container)
    {
        var entries = _data.Entries.OrderBy(e => e.TransactionDate).ToList();

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => SectionLabel(c, "Ledger"));
                row.AutoItem().Text(_data.PeriodLabel)
                    .Style(DocTheme.MonoText).FontSize(DocTheme.Px(9)).FontColor(DocTheme.Label);
            });

            if (entries.Count == 0)
            {
                col.Item().PaddingTop(DocTheme.Px(20)).AlignCenter()
                    .Text("No transactions found for this period.")
                    .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            }
            else
            {
                col.Item().PaddingTop(DocTheme.Px(8)).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        // Every column but Description must never wrap — a wrapped code or amount
                        // is much worse than a wrapped free-text description. Sized to the longest
                        // realistic values: "Cancellation" (Type), "SPY-2026-0059" (Reference),
                        // Indian-grouped 6-7 digit amounts (Debit/Credit/Balance).
                        c.ConstantColumn(DocTheme.Px(85));   // Date
                        c.ConstantColumn(DocTheme.Px(85));   // Type
                        c.ConstantColumn(DocTheme.Px(105));  // Reference
                        c.RelativeColumn();                  // Description
                        c.ConstantColumn(DocTheme.Px(105));  // Debit
                        c.ConstantColumn(DocTheme.Px(105));  // Credit
                        c.ConstantColumn(DocTheme.Px(112));  // Balance (may carry a "-" sign)
                    });

                    table.Header(header =>
                    {
                        Head(header.Cell(), "Date");
                        Head(header.Cell(), "Type");
                        Head(header.Cell(), "Reference");
                        // "Description" (11 chars, letter-spaced uppercase) wraps mid-word in this
                        // column — six other columns compete for width, unlike the wider item tables.
                        Head(header.Cell(), "Details");
                        Head(header.Cell(), "Debit", right: true);
                        Head(header.Cell(), "Credit", right: true);
                        Head(header.Cell(), "Balance", right: true);
                    });

                    foreach (var e in entries)
                    {
                        Cell(table.Cell(), e.TransactionDate.ToString("dd MMM yy"), mono: true);
                        Cell(table.Cell(), FormatType(e.TransactionType));
                        Cell(table.Cell(), e.ReferenceNumber, mono: true, color: DocTheme.Muted);
                        Cell(table.Cell(), e.Description, color: DocTheme.Muted);
                        Cell(table.Cell(), e.DebitAmount > 0 ? DocTheme.Amount(e.DebitAmount) : "—", mono: true, right: true);
                        Cell(table.Cell(), e.CreditAmount > 0 ? DocTheme.Amount(e.CreditAmount) : "—", mono: true, right: true);
                        Cell(table.Cell(), DocTheme.Amount(e.RunningBalance), mono: true, right: true, medium: true);
                    }
                });
            }

            col.Item().PaddingTop(DocTheme.Px(10)).Element(ComposeSummary);
        });
    }

    private void ComposeSummary(IContainer container)
    {
        container.AlignRight().Width(DocTheme.TotalsWidth).Column(col =>
        {
            SummaryRow(col, "Total Purchases", DocTheme.Amount(_data.TotalPurchases));
            SummaryRow(col, "Total Payments", $"({DocTheme.Amount(_data.TotalPayments)})");
            if (_data.TotalRefunds > 0)
                SummaryRow(col, "Total Refunds", $"({DocTheme.Amount(_data.TotalRefunds)})");
            if (_data.AvailableAdvanceCredit > 0)
                SummaryRow(col, "Available Advance Credit", DocTheme.Amount(_data.AvailableAdvanceCredit));

            col.Item().PaddingTop(DocTheme.Px(4))
                .BorderTop(DocTheme.RuleMedium).BorderBottom(DocTheme.RuleMedium)
                .BorderColor(DocTheme.Ink)
                .Padding(DocTheme.Px(8))
                .Row(row =>
                {
                    row.RelativeItem().Text(_data.CurrentBalance <= 0 ? "Settled" : "Current Balance")
                        .FontSize(DocTheme.GrandTotal).Bold().FontColor(DocTheme.Ink);
                    row.AutoItem().Text(_theme.Money(Math.Abs(_data.CurrentBalance)))
                        .Style(DocTheme.MonoText).FontSize(DocTheme.GrandTotal).Bold().FontColor(DocTheme.Ink);
                });
        });

        static void SummaryRow(ColumnDescriptor col, string label, string value) =>
            col.Item().PaddingVertical(DocTheme.Px(5)).PaddingHorizontal(DocTheme.Px(8)).Row(row =>
            {
                row.RelativeItem().Text(label)
                    .FontSize(DocTheme.TableCell).FontColor(DocTheme.Secondary);
                row.AutoItem().Text(value)
                    .Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Secondary);
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(10)).Row(row =>
        {
            row.RelativeItem().Text(_shop.FooterText)
                .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label);
            row.AutoItem().Text(txt =>
            {
                txt.DefaultTextStyle(DocTheme.MonoText.FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label));
                txt.CurrentPageNumber();
                txt.Span(" / ");
                txt.TotalPages();
            });
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private static string FormatType(string type) => type switch
    {
        "PURCHASE" => "Purchase",
        "PAYMENT" => "Payment",
        "REFUND" => "Refund",
        "ADVANCE" => "Advance",
        "CANCELLATION" => "Cancellation",
        _ => string.IsNullOrWhiteSpace(type) ? "—" : type
    };

    private static void Head(IContainer cell, string text, bool right = false)
    {
        var c = cell
            .BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8));
        if (right) c = c.AlignRight();
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.TableHeader).SemiBold().FontColor(DocTheme.Secondary)
            .LetterSpacing(1.2f / DocTheme.TableHeader);
    }

    private static void Cell(
        IContainer cell, string text,
        bool mono = false, bool right = false, bool medium = false, string color = DocTheme.Ink)
    {
        var c = cell
            .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8));
        if (right) c = c.AlignRight();
        var span = c.Text(text).FontSize(DocTheme.Px(10.5f)).FontColor(color);
        if (mono) span = span.Style(DocTheme.MonoText).FontSize(DocTheme.Px(10.5f)).FontColor(color);
        if (medium) span.Medium();
    }

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);

    private static void InfoRow(IContainer c, string label, string value) =>
        c.Row(row =>
        {
            row.ConstantItem(DocTheme.Px(60)).Text(label)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            row.RelativeItem().Text(value)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });
}
