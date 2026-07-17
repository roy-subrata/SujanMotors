using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Application.DTOs.CustomerDtos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record ShopProfile(
    string Name,
    string Address,
    string Phone,
    string Email,
    string TaxNo,
    string Tagline,
    string FooterText,
    string CurrencySymbol = "৳",
    // BankDetails: free-text bank block for the invoice footer, from the SHOP_BANK_DETAILS setting
    // — e.g. "City Bank PLC — Kawran Bazar Branch\nA/C Name: …\nA/C No: … · Routing: …".
    // Blank until configured; the invoice omits the block rather than printing a placeholder
    // account number that someone might actually pay into.
    string BankDetails = "");

/// <summary>
/// Statement of Account. Not one of the 13 documents in design_handoff_pos_documents, so the body
/// is bespoke, but it uses the same header, palette, and table conventions so it reads as part of
/// the same product.
/// </summary>
public class CustomerAccountStatementDocument : IDocument
{
    private readonly CustomerAccountSummaryDto _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public CustomerAccountStatementDocument(CustomerAccountSummaryDto data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Statement of Account – {_data.CustomerName}",
        Author = _shop.Name,
        Subject = $"Statement of account for {_data.CustomerName}",
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

    // "Statement" rather than "Statement of Account": the longer title is wider than any in the
    // handoff and squeezes the company block until the tagline wraps. The meta grid says what it is.
    private void ComposeHeader(IContainer container) =>
        new DocHeader(_theme, _shop, "Statement",
        [
            // Period is too wide for the meta column and already appears under Statement Details.
            new MetaField("No.", $"SOA-{_data.CustomerCode}"),
            new MetaField("Date", _data.ReportDate.ToString("dd MMM yyyy")),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeBillTo);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeStatementDetails);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeTransactions);
        });
    }

    // ── Bill To ────────────────────────────────────────────────────────────────
    private void ComposeBillTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Bill To"));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.CustomerName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            if (!string.IsNullOrWhiteSpace(_data.CustomerCode))
                col.Item().PaddingTop(DocTheme.Px(4)).Element(c => InfoRow(c, "Account No", _data.CustomerCode));
            if (!string.IsNullOrWhiteSpace(_data.CustomerPhone))
                col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, "Phone", _data.CustomerPhone));
            if (!string.IsNullOrWhiteSpace(_data.CustomerType))
                col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, "Account Type", _data.CustomerType));
        });
    }

    // ── Statement details ──────────────────────────────────────────────────────
    private void ComposeStatementDetails(IContainer container)
    {
        var vehicles = _data.PurchaseItems
            .Select(x => x.VehicleLabel)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToList();

        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Statement Details"));
            col.Item().PaddingTop(DocTheme.Px(6)).Element(c => InfoRow(c, "Period", PeriodLabel()));
            col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, "Transactions", _data.PurchaseItems.Count.ToString()));

            // Only meaningful when the whole statement concerns one vehicle; otherwise the column
            // in the table carries it per line.
            if (vehicles.Count == 1)
                col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, "Vehicle", vehicles[0]));
        });
    }

    // ── Transaction history ────────────────────────────────────────────────────
    private void ComposeTransactions(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Transaction History"));

            if (_data.PurchaseItems.Count == 0)
            {
                col.Item().PaddingTop(DocTheme.Px(20)).AlignCenter()
                    .Text("No transactions found for this period.")
                    .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
                return;
            }

            col.Item().PaddingTop(DocTheme.Px(8)).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    // Sized so the mono values fit on one line: "17 Jul 26" and "INV-2026-0847"
                    // both wrapped at the handoff's narrower widths once px→pt scaling applied.
                    c.ConstantColumn(DocTheme.Px(92));   // Date
                    c.ConstantColumn(DocTheme.Px(125));  // Invoice #
                    c.RelativeColumn();                  // Item
                    c.ConstantColumn(DocTheme.Px(115));  // Vehicle
                    c.ConstantColumn(DocTheme.Px(45));   // Qty
                    c.ConstantColumn(DocTheme.Px(85));   // Unit price
                    c.ConstantColumn(DocTheme.Px(95));   // Amount
                });

                table.Header(header =>
                {
                    Head(header.Cell(), "Date");
                    Head(header.Cell(), "Invoice");
                    Head(header.Cell(), "Item Description");
                    Head(header.Cell(), "Vehicle");
                    Head(header.Cell(), "Qty", right: true);
                    Head(header.Cell(), $"Rate ({_theme.CurrencySymbol})", right: true);
                    Head(header.Cell(), $"Amount ({_theme.CurrencySymbol})", right: true);
                });

                foreach (var item in _data.PurchaseItems)
                {
                    var sku = !string.IsNullOrWhiteSpace(item.SKU) ? item.SKU : item.PartNumber;

                    Cell(table.Cell(), item.InvoiceDate.ToString("dd MMM yy"), mono: true);
                    Cell(table.Cell(), item.InvoiceNumber, mono: true);

                    // Description carries the local name and code beneath, as on the invoice.
                    Body(table.Cell()).Column(c =>
                    {
                        c.Item().Text(item.ItemName).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
                        if (!string.IsNullOrWhiteSpace(item.ItemLocalName))
                            c.Item().Text(item.ItemLocalName)
                                .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Muted);
                        if (!string.IsNullOrWhiteSpace(sku))
                            c.Item().Text(sku)
                                .Style(DocTheme.MonoText).FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label);
                    });

                    Cell(table.Cell(), string.IsNullOrWhiteSpace(item.VehicleLabel) ? "—" : item.VehicleLabel,
                        color: DocTheme.Muted);
                    Cell(table.Cell(), item.Quantity.ToString(), mono: true, right: true);
                    Cell(table.Cell(), item.UnitPrice.ToString("N2"), mono: true, right: true);
                    Cell(table.Cell(), item.LineTotal.ToString("N2"), mono: true, right: true, medium: true);
                }
            });

            col.Item().PaddingTop(DocTheme.Px(10)).Element(ComposeSummary);
        });
    }

    // ── Summary stack (mirrors the shared ItemsTable totals block) ─────────────
    private void ComposeSummary(IContainer container)
    {
        container.AlignRight().Width(DocTheme.TotalsWidth).Column(col =>
        {
            SummaryRow(col, "Total Billed", _data.TotalPurchaseAmount.ToString("N2"));
            SummaryRow(col, "Total Paid", $"({_data.TotalPaidAmount:N2})");

            col.Item().PaddingTop(DocTheme.Px(4))
                .BorderTop(DocTheme.RuleMedium).BorderBottom(DocTheme.RuleMedium)
                .BorderColor(DocTheme.Ink)
                .Padding(DocTheme.Px(8))
                .Row(row =>
                {
                    row.RelativeItem().Text(_data.CurrentDue <= 0 ? "Settled" : "Balance Due")
                        .FontSize(DocTheme.GrandTotal).Bold().FontColor(DocTheme.Ink);
                    row.AutoItem().Text(_theme.Money(Math.Abs(_data.CurrentDue)))
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
            row.RelativeItem().Text(
                    string.IsNullOrWhiteSpace(_shop.FooterText)
                        ? "Thank you for your continued business. Please contact us for any queries."
                        : _shop.FooterText)
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
    private string PeriodLabel()
    {
        var from = _data.FromDate?.ToString("dd MMM yyyy");
        var to = _data.ToDate?.ToString("dd MMM yyyy");

        if (from is null && to is null) return "All time";
        return $"{from ?? "All time"} – {to ?? _data.ReportDate.ToString("dd MMM yyyy")}";
    }

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

    private static IContainer Body(IContainer cell) =>
        cell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8));

    private static void Cell(
        IContainer cell,
        string text,
        bool mono = false,
        bool right = false,
        bool medium = false,
        string color = DocTheme.Ink)
    {
        var c = Body(cell);
        if (right) c = c.AlignRight();

        var span = c.Text(text).FontSize(DocTheme.TableCell).FontColor(color);
        if (mono) span = span.Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(color);
        if (medium) span.Medium();
    }

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);

    private static void InfoRow(IContainer c, string label, string value) =>
        c.Row(row =>
        {
            row.ConstantItem(DocTheme.Px(80)).Text(label)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            row.RelativeItem().Text(value)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });
}
