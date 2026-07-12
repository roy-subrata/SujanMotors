using AutoPartShop.Application.DTOs.CustomerDtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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
    string CurrencySymbol = "৳");

public class CustomerAccountStatementDocument : IDocument
{
    // ── Palette ────────────────────────────────────────────────────────────────
    private const string NavyPrimary = "#1e3a8a";
    private const string GreenText = "#15803d";
    private const string Gray50 = "#f9fafb";
    private const string Gray100 = "#f3f4f6";
    private const string Gray200 = "#e5e7eb";
    private const string Gray300 = "#d1d5db";
    private const string Gray400 = "#9ca3af";
    private const string Gray500 = "#6b7280";
    private const string Gray700 = "#374151";
    private const string Gray900 = "#111827";
    private const string White = "#FFFFFF";

    private readonly CustomerAccountSummaryDto _data;
    private readonly ShopProfile _shop;

    public CustomerAccountStatementDocument(CustomerAccountSummaryDto data, ShopProfile shop)
    {
        _data = data;
        _shop = shop;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Statement of Account – {_data.CustomerName}",
        Author = _shop.Name,
        CreationDate = DateTime.UtcNow
    };

    // ── Page setup ─────────────────────────────────────────────────────────────
    // A4: 595pt wide. Margins 40pt each side → 515pt usable content width.
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(40);
            page.MarginVertical(32);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Gray900));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── HEADER — repeated on every page ────────────────────────────────────────
    // Left col: company identity (RelativeItem, ~330pt)
    // Right col: document title block (185pt)
    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Company identity
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(_shop.Name)
                        .Bold().FontSize(16).FontColor(NavyPrimary);

                    if (!string.IsNullOrWhiteSpace(_shop.Tagline))
                        left.Item().PaddingTop(2).Text(_shop.Tagline)
                            .FontSize(8).Italic().FontColor(Gray500);

                    if (!string.IsNullOrWhiteSpace(_shop.Address))
                        left.Item().PaddingTop(5).Text(_shop.Address)
                            .FontSize(8).FontColor(Gray500);

                    var contacts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(_shop.Phone)) contacts.Add($"Tel: {_shop.Phone}");
                    if (!string.IsNullOrWhiteSpace(_shop.Email)) contacts.Add($"Email: {_shop.Email}");
                    if (!string.IsNullOrWhiteSpace(_shop.TaxNo)) contacts.Add($"Tax No: {_shop.TaxNo}");
                    if (contacts.Count > 0)
                        left.Item().PaddingTop(2).Text(string.Join("   ·   ", contacts))
                            .FontSize(8).FontColor(Gray500);
                });

                // Document title block (right column, 185pt)
                // "STATEMENT OF ACCOUNT" at 13pt bold ≈ 150pt — safe in 185pt.
                row.ConstantItem(185).Column(right =>
                {
                    right.Item().AlignRight().Text("STATEMENT OF ACCOUNT")
                        .Bold().FontSize(13).FontColor(NavyPrimary);

                    right.Item().PaddingTop(10).AlignRight().Text(txt =>
                    {
                        txt.Span("Date:   ").FontSize(8).FontColor(Gray500);
                        txt.Span(_data.ReportDate.ToString("dd MMM yyyy"))
                            .FontSize(8).Bold().FontColor(Gray900);
                    });

                    right.Item().PaddingTop(3).AlignRight().Text(txt =>
                    {
                        txt.Span("Ref:   ").FontSize(8).FontColor(Gray500);
                        txt.Span($"SOA-{_data.CustomerCode}-{_data.ReportDate:yyyyMMdd}")
                            .FontSize(8).FontColor(Gray700);
                    });
                });
            });

            // Navy rule separating header from content
            col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor(NavyPrimary);
        });
    }

    // ── CONTENT ────────────────────────────────────────────────────────────────
    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(16).Column(col =>
        {
            // Bill To / Statement Details side-by-side
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeBillToBlock);
                row.ConstantItem(16);
                row.RelativeItem().Element(ComposeStatementDetailsBlock);
            });

            col.Item().PaddingTop(22).Element(ComposeTransactionsTable);
        });
    }

    // ── BILL TO — plain, no box ────────────────────────────────────────────────
    private void ComposeBillToBlock(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("BILL TO").Bold().FontSize(7).FontColor(NavyPrimary);
            col.Item().PaddingTop(5).Text(_data.CustomerName)
                .Bold().FontSize(13).FontColor(Gray900);
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Gray200);

            if (!string.IsNullOrWhiteSpace(_data.CustomerCode))
                col.Item().PaddingTop(5).Element(c => InfoRow(c, "Account No", _data.CustomerCode));
            if (!string.IsNullOrWhiteSpace(_data.CustomerPhone))
                col.Item().PaddingTop(3).Element(c => InfoRow(c, "Phone", _data.CustomerPhone));
            if (!string.IsNullOrWhiteSpace(_data.CustomerType))
                col.Item().PaddingTop(3).Element(c => InfoRow(c, "Account Type", _data.CustomerType));
        });
    }

    // ── STATEMENT DETAILS — plain, period only ─────────────────────────────────
    private void ComposeStatementDetailsBlock(IContainer container)
    {
        var from = _data.FromDate?.ToString("dd MMM yyyy") ?? "All time";
        var to = _data.ToDate?.ToString("dd MMM yyyy") ?? "All time";

        var vehicleLabels = _data.PurchaseItems
            .Select(x => x.VehicleLabel)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToList();

        container.Column(col =>
        {
            col.Item().Text("STATEMENT DETAILS").Bold().FontSize(7).FontColor(Gray500);
            col.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Gray200);
            col.Item().PaddingTop(8).Text("Statement Period").FontSize(8).Bold().FontColor(Gray700);
            col.Item().PaddingTop(3).Text($"{from}  –  {to}").FontSize(9).FontColor(Gray900);

            if (vehicleLabels.Count == 1)
            {
                col.Item().PaddingTop(8).Text("Vehicle").FontSize(8).Bold().FontColor(Gray700);
                col.Item().PaddingTop(3).Text(vehicleLabels[0]).FontSize(9).FontColor(Gray900);
            }
        });
    }

    // ── TRANSACTION TABLE + SUMMARY ────────────────────────────────────────────
    // Column widths (relative, total = 80, content = 515pt):
    //   Date (9) = 57.9pt  · Invoice (10) = 64.4pt · Item (26) = 167.4pt
    //   Vehicle (10) = 64.4pt · Qty (5) = 32.2pt · UnitPrice (10) = 64.4pt · Amount (10) = 64.4pt
    // PaddingHorizontal(5) → inner width = col_pt − 10
    private void ComposeTransactionsTable(IContainer container)
    {
        var grandTotal = _data.PurchaseItems.Sum(x => x.LineTotal);

        container.Column(col =>
        {
            // Section heading
            col.Item().BorderBottom(1.5f).BorderColor(NavyPrimary).PaddingBottom(6)
                .Text("TRANSACTION HISTORY").Bold().FontSize(10).FontColor(NavyPrimary);

            if (_data.PurchaseItems.Count == 0)
            {
                col.Item().PaddingTop(20).AlignCenter()
                    .Text("No transactions found for this period.")
                    .FontSize(9).Italic().FontColor(Gray400);
                return;
            }

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(9);   // Date
                    c.RelativeColumn(10);  // Invoice #
                    c.RelativeColumn(26);  // Item + SKU
                    c.RelativeColumn(10);  // Vehicle
                    c.RelativeColumn(5);   // Qty
                    c.RelativeColumn(10);  // Unit Price
                    c.RelativeColumn(10);  // Amount
                });

                // Header row
                table.Header(header =>
                {
                    void H(IContainer c, string text, bool right = false)
                    {
                        var cell = c.Background(NavyPrimary)
                            .PaddingHorizontal(5).PaddingVertical(7);
                        (right ? cell.AlignRight().Text(text) : cell.Text(text))
                            .Bold().FontSize(8f).FontColor(White);
                    }

                    H(header.Cell(), "Date");
                    H(header.Cell(), "Invoice #");
                    H(header.Cell(), "Item Description");
                    H(header.Cell(), "Vehicle");
                    H(header.Cell(), "Qty", right: true);
                    H(header.Cell(), "Unit Price", right: true);
                    H(header.Cell(), "Amount", right: true);
                });

                // Data rows — alternating white / gray50
                bool alt = false;
                foreach (var item in _data.PurchaseItems)
                {
                    string bg = alt ? Gray50 : White;
                    alt = !alt;
                    const float hp = 5f, vp = 5f, bs = 0.5f;
                    string sku = !string.IsNullOrWhiteSpace(item.SKU) ? item.SKU : item.PartNumber;

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(item.InvoiceDate.ToString("dd MMM yy"))
                        .FontSize(8).FontColor(Gray700);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(item.InvoiceNumber)
                        .FontSize(8).FontColor(NavyPrimary);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Column(c =>
                        {
                            c.Item().Text(item.ItemName).FontSize(8).FontColor(Gray900);
                            if (!string.IsNullOrWhiteSpace(item.ItemLocalName))
                                c.Item().PaddingTop(1).Text(item.ItemLocalName).FontSize(7f).FontColor(Gray500);
                            if (!string.IsNullOrWhiteSpace(sku))
                                c.Item().PaddingTop(1).Text(sku).FontSize(7.5f).FontColor(Gray400);
                        });

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(string.IsNullOrWhiteSpace(item.VehicleLabel) ? "—" : item.VehicleLabel)
                        .FontSize(8f).FontColor(Gray500);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .AlignRight()
                        .Text(item.Quantity.ToString()).FontSize(8).FontColor(Gray700);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .AlignRight()
                        .Text($"{_shop.CurrencySymbol} {item.UnitPrice:N2}").FontSize(8).FontColor(Gray700);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .AlignRight()
                        .Text($"{_shop.CurrencySymbol} {item.LineTotal:N2}").FontSize(8).Bold().FontColor(Gray900);
                }

                // Grand Total footer row
                table.Cell().ColumnSpan(6)
                    .Background(Gray100).PaddingHorizontal(5).PaddingVertical(8)
                    .AlignRight()
                    .Text("GRAND TOTAL").Bold().FontSize(8.5f).FontColor(NavyPrimary);

                table.Cell()
                    .Background(NavyPrimary).PaddingHorizontal(5).PaddingVertical(8)
                    .AlignRight()
                    .Text($"{_shop.CurrencySymbol} {grandTotal:N2}").Bold().FontSize(8.5f).FontColor(White);
            });

            // Financial summary — plain text, right-aligned, no box
            // ConstantItem(200): label col ~105pt, value col 90pt.
            // "BALANCE DUE" at 10pt ≈ 66pt — fits in 110pt.
            // "100,000.00" at 10pt ≈ 55pt — fits in 90pt.
            col.Item().PaddingTop(4).Row(summaryRow =>
            {
                summaryRow.RelativeItem(); // spacer pushes summary to right

                summaryRow.ConstantItem(200).Column(s =>
                {
                    s.Item().PaddingTop(10).Row(r =>
                    {
                        r.RelativeItem().Text("Total Billed").FontSize(8.5f).FontColor(Gray500);
                        r.ConstantItem(90).AlignRight()
                            .Text($"{_shop.CurrencySymbol} {_data.TotalPurchaseAmount:N2}").FontSize(8.5f).FontColor(Gray700);
                    });

                    s.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem().Text("Total Paid").FontSize(8.5f).FontColor(GreenText);
                        r.ConstantItem(90).AlignRight()
                            .Text($"({_shop.CurrencySymbol} {_data.TotalPaidAmount:N2})").FontSize(8.5f).FontColor(GreenText);
                    });

                    s.Item().PaddingTop(8).LineHorizontal(1f).LineColor(Gray300);

                    s.Item().PaddingTop(8).Row(r =>
                    {
                        r.RelativeItem().Text("BALANCE DUE").Bold().FontSize(10).FontColor(Gray900);
                        r.ConstantItem(90).AlignRight()
                            .Text($"{_shop.CurrencySymbol} {_data.CurrentDue:N2}").Bold().FontSize(10).FontColor(Gray900);
                    });
                });
            });
        });
    }

    // ── FOOTER ─────────────────────────────────────────────────────────────────
    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Gray200);

            col.Item().PaddingTop(6).Row(row =>
            {
                // Footer text + generated timestamp
                row.RelativeItem().Column(left =>
                {
                    var footerText = string.IsNullOrWhiteSpace(_shop.FooterText)
                        ? "Thank you for your continued business. Please contact us for any queries."
                        : _shop.FooterText;
                    left.Item().Text(footerText).FontSize(8f).Italic().FontColor(Gray400);
                    left.Item().PaddingTop(2)
                        .Text($"Generated {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC")
                        .FontSize(7f).FontColor(Gray300);
                });

                // Page number — 70pt fits "Page 99 / 99" at 8pt
                row.ConstantItem(70).AlignRight().Text(txt =>
                {
                    txt.Span("Page ").FontSize(8).FontColor(Gray400);
                    txt.CurrentPageNumber()
                        .Style(TextStyle.Default.FontSize(8).Bold().FontColor(NavyPrimary));
                    txt.Span(" / ").FontSize(8).FontColor(Gray400);
                    txt.TotalPages()
                        .Style(TextStyle.Default.FontSize(8).Bold().FontColor(NavyPrimary));
                });
            });
        });
    }

    // ── Helper ─────────────────────────────────────────────────────────────────
    private static void InfoRow(IContainer c, string label, string value)
    {
        c.Text(txt =>
        {
            txt.Span($"{label}:  ").FontSize(8).FontColor(Gray500);
            txt.Span(value).FontSize(8).Bold().FontColor(Gray700);
        });
    }
}
