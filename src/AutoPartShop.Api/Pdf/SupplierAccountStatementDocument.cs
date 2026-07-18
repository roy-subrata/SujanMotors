using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Application.DTOs.PaymentDtos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

/// <summary>
/// Supplier Account Statement. Like the customer statement, this is not one of the 13 handoff
/// documents, but it uses the same header, palette, and table conventions. Replaces the previous
/// iText7-rendered supplier payment summary.
/// </summary>
public class SupplierAccountStatementDocument : IDocument
{
    private readonly SupplierPaymentHistorySummary _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public SupplierAccountStatementDocument(SupplierPaymentHistorySummary data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Supplier Statement – {_data.SupplierName}",
        Author = _shop.Name,
        Subject = $"Supplier account statement for {_data.SupplierName}",
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

    private void ComposeHeader(IContainer container) =>
        new DocHeader(_theme, _shop, "Supplier Statement",
        [
            // SupplierCode already carries its own prefix (e.g. "SUP-0042") — don't double it.
            new MetaField("No.", _data.SupplierCode),
            new MetaField("Date", DateTime.Now.ToString("dd MMM yyyy")),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeSupplier);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeAccountDetails);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeHistory);
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

    private void ComposeAccountDetails(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Account Details"));
            if (_data.CreditLimit > 0)
                col.Item().PaddingTop(DocTheme.Px(6)).Element(c => InfoRow(c, "Credit Limit", _theme.Money(_data.CreditLimit)));
            col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, "Outstanding", $"{_data.OutstandingInvoiceCount} invoice(s)"));
            if (_data.LastPaymentDate is { } last)
                col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, "Last Payment", $"{last:dd MMM yyyy} · {_theme.Money(_data.LastPaymentAmount)}"));
        });
    }

    private void ComposeHistory(IContainer container)
    {
        var history = _data.PaymentHistory
            .OrderByDescending(p => p.PaymentDate)
            .ToList();

        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Payment History"));

            if (history.Count == 0)
            {
                col.Item().PaddingTop(DocTheme.Px(20)).AlignCenter()
                    .Text("No payments recorded for this supplier.")
                    .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            }
            else
            {
                col.Item().PaddingTop(DocTheme.Px(8)).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(DocTheme.Px(92));   // Date
                        c.ConstantColumn(DocTheme.Px(120));  // Transaction
                        c.ConstantColumn(DocTheme.Px(95));   // Method
                        c.RelativeColumn();                  // Reference (PO / Invoice)
                        c.ConstantColumn(DocTheme.Px(100));  // Status ("COMPLETED" needs the width)
                        c.ConstantColumn(DocTheme.Px(95));   // Amount
                    });

                    table.Header(header =>
                    {
                        Head(header.Cell(), "Date");
                        Head(header.Cell(), "Transaction");
                        Head(header.Cell(), "Method");
                        Head(header.Cell(), "Reference");
                        Head(header.Cell(), "Status");
                        Head(header.Cell(), $"Amount ({_theme.CurrencySymbol})", right: true);
                    });

                    foreach (var p in history)
                    {
                        var reference = !string.IsNullOrWhiteSpace(p.PurchaseOrderNumber)
                            ? p.PurchaseOrderNumber!
                            : !string.IsNullOrWhiteSpace(p.InvoiceNumber) ? p.InvoiceNumber : "—";

                        Cell(table.Cell(), p.PaymentDate.ToString("dd MMM yy"), mono: true);
                        Cell(table.Cell(), p.TransactionNumber, mono: true);
                        Cell(table.Cell(), FormatMethod(p.PaymentMethod));
                        Cell(table.Cell(), reference, mono: true, color: DocTheme.Muted);
                        Cell(table.Cell(), p.Status, color: DocTheme.Muted);
                        Cell(table.Cell(), DocTheme.Amount(p.Amount), mono: true, right: true, medium: true);
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
            SummaryRow(col, "Total Paid", DocTheme.Amount(_data.TotalPaid));
            if (_data.TotalAdvanceAmount > 0)
                SummaryRow(col, "Advances", DocTheme.Amount(_data.TotalAdvanceAmount));
            if (_data.TotalRefunds > 0)
                SummaryRow(col, "Refunds", $"({DocTheme.Amount(_data.TotalRefunds)})");
            SummaryRow(col, "Total Due", DocTheme.Amount(_data.TotalDue));

            col.Item().PaddingTop(DocTheme.Px(4))
                .BorderTop(DocTheme.RuleMedium).BorderBottom(DocTheme.RuleMedium)
                .BorderColor(DocTheme.Ink)
                .Padding(DocTheme.Px(8))
                .Row(row =>
                {
                    row.RelativeItem().Text(_data.PaymentBalance <= 0 ? "Settled" : "Balance Payable")
                        .FontSize(DocTheme.GrandTotal).Bold().FontColor(DocTheme.Ink);
                    row.AutoItem().Text(_theme.Money(Math.Abs(_data.PaymentBalance)))
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
    private static string FormatMethod(string method) => method switch
    {
        "CASH" => "Cash",
        "CARD" => "Card",
        "MOBILE_BANKING" => "Mobile",
        "BANK_TRANSFER" => "Bank Transfer",
        _ => string.IsNullOrWhiteSpace(method) ? "—" : method.Replace('_', ' ')
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
            row.ConstantItem(DocTheme.Px(90)).Text(label)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            row.RelativeItem().Text(value)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });
}
