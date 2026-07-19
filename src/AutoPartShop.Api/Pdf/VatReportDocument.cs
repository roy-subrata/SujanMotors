using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record VatReportDocumentData(
    string ReportNumber,
    DateTime FromDate,
    DateTime ToDate,
    decimal VatRatePercent,
    decimal SalesTaxableValue,
    decimal SalesVatAmount,
    int SalesInvoiceCount,
    decimal CreditTaxableValue,
    decimal CreditVatAmount,
    decimal PurchaseTaxableValue,
    decimal PurchaseVatAmount,
    int PurchaseOrderCount,
    decimal NetVatPayable);

/// <summary>
/// VAT Report — document 11 of design_handoff_pos_documents.
/// Output VAT on sales, less output VAT reversed by credit notes, less input VAT on purchases.
/// </summary>
public class VatReportDocument : IDocument
{
    private readonly VatReportDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public VatReportDocument(VatReportDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"VAT Report {_data.ReportNumber}",
        Author = _shop.Name,
        Subject = $"VAT reconciliation for {_data.FromDate:dd MMM yyyy} - {_data.ToDate:dd MMM yyyy}",
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
        new DocHeader(_theme, _shop, "VAT Report",
        [
            new MetaField("No.", _data.ReportNumber),
            new MetaField("Date", DateTime.Now.ToString("dd MMM yyyy")),
            new MetaField("Period", FormatPeriod()),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeTable);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Accountant", "VAT Consultant", "Proprietor").Compose(c));
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(DocTheme.Px(130));  // Taxable Value
                c.ConstantColumn(DocTheme.Px(120));  // VAT
            });

            table.Header(header =>
            {
                Head(header.Cell(), "Description");
                Head(header.Cell(), $"Taxable Value ({_theme.CurrencySymbol})", right: true);
                Head(header.Cell(), $"VAT ({_theme.CurrencySymbol})", right: true);
            });

            Row(table, $"Output VAT — Sales (Mushak-6.3, {_data.SalesInvoiceCount} invoices)",
                DocTheme.Amount(_data.SalesTaxableValue), DocTheme.Amount(_data.SalesVatAmount));

            Row(table, "Less: Output VAT reversed — Credit Notes",
                $"- {DocTheme.Amount(_data.CreditTaxableValue)}", $"- {DocTheme.Amount(_data.CreditVatAmount)}");

            Row(table, $"Less: Input VAT — Purchases (Mushak-6.1, {_data.PurchaseOrderCount} orders)",
                DocTheme.Amount(_data.PurchaseTaxableValue), $"- {DocTheme.Amount(_data.PurchaseVatAmount)}");

            // Total row: 2px rule top+bottom, bold, spans Description+Taxable Value with the VAT
            // figure alone on the right — matches the handoff (no total shown under Taxable Value).
            table.Cell().ColumnSpan(2)
                .BorderTop(DocTheme.RuleMedium).BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                .PaddingVertical(DocTheme.Px(9)).PaddingHorizontal(DocTheme.Px(8))
                .Text("Net VAT Payable to NBR").FontSize(DocTheme.TableCell).Bold().FontColor(DocTheme.Ink);

            table.Cell()
                .BorderTop(DocTheme.RuleMedium).BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                .PaddingVertical(DocTheme.Px(9)).PaddingHorizontal(DocTheme.Px(8)).AlignRight()
                .Text(DocTheme.Amount(_data.NetVatPayable))
                .Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).Bold().FontColor(DocTheme.Ink);
        });
    }

    private void ComposeNote(IContainer container)
    {
        var note = $"Standard VAT rate {_data.VatRatePercent:N0}% applied on all taxable sales. " +
                   "Figures feed the monthly Mushak-9.1 return, due by the 15th of the following month. " +
                   "Supporting registers: sales register, purchase register, credit note file.";

        container.Text(note)
            .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
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
    private string FormatPeriod() =>
        _data.FromDate.Year == _data.ToDate.Year && _data.FromDate.Month == _data.ToDate.Month
            ? $"{_data.FromDate:dd} – {_data.ToDate:dd MMM yyyy}"
            : $"{_data.FromDate:dd MMM yyyy} – {_data.ToDate:dd MMM yyyy}";

    private static void Row(TableDescriptor table, string label, string taxable, string vat)
    {
        Body(table.Cell()).Text(label).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
        Body(table.Cell()).AlignRight().Text(taxable).Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
        Body(table.Cell()).AlignRight().Text(vat).Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
    }

    private static IContainer Body(IContainer cell) =>
        cell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .PaddingVertical(DocTheme.Px(8)).PaddingHorizontal(DocTheme.Px(8));

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
}
