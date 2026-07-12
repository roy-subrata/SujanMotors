using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record InvoiceLine(
    int SlNo,
    string DisplayName,
    string? LocalName,
    string PartNumber,
    string SKU,
    string UnitSymbol,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPerUnit,  // absolute per-unit discount amount (not percentage)
    decimal LineTotal);

public record InvoicePaymentEntry(
    DateTime PaymentDate,
    string Method,
    string Reference,
    decimal Amount);

public record InvoiceDocumentData(
    string InvoiceNumber,
    string SalesOrderNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    string Status,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string CustomerAddress,
    string VehicleLabel,
    string TechnicianName,
    List<InvoiceLine> Lines,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxPercentage,   // informational; shown as "Tax (X%)" label
    decimal TaxAmount,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    List<InvoicePaymentEntry> Payments,
    string Notes);

public class InvoiceDocument : IDocument
{
    // ── Palette (matches CustomerAccountStatementDocument) ──────────────────────
    private const string NavyPrimary = "#1e3a8a";
    private const string GreenText = "#15803d";
    private const string AmberText = "#92400e";
    private const string RedText = "#dc2626";
    private const string Gray50 = "#f9fafb";
    private const string Gray100 = "#f3f4f6";
    private const string Gray200 = "#e5e7eb";
    private const string Gray300 = "#d1d5db";
    private const string Gray400 = "#9ca3af";
    private const string Gray500 = "#6b7280";
    private const string Gray700 = "#374151";
    private const string Gray900 = "#111827";
    private const string White = "#FFFFFF";

    private readonly InvoiceDocumentData _data;
    private readonly ShopProfile _shop;

    public InvoiceDocument(InvoiceDocumentData data, ShopProfile shop)
    {
        _data = data;
        _shop = shop;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice {_data.InvoiceNumber}",
        Author = _shop.Name,
        Subject = $"Invoice for {_data.CustomerName}",
        CreationDate = DateTime.UtcNow
    };

    // ── Page setup ─────────────────────────────────────────────────────────────
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(40);
            page.MarginVertical(32);
            page.DefaultTextStyle(x => x.FontFamily("Bengali").FontSize(9).FontColor(Gray900));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── HEADER ─────────────────────────────────────────────────────────────────
    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Company identity (left)
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
                    if (!string.IsNullOrWhiteSpace(_shop.TaxNo)) contacts.Add($"Tax: {_shop.TaxNo}");
                    if (contacts.Count > 0)
                        left.Item().PaddingTop(2).Text(string.Join("   ·   ", contacts))
                            .FontSize(8).FontColor(Gray500);
                });

                // Document title block (right, 185pt)
                row.ConstantItem(185).Column(right =>
                {
                    right.Item().AlignRight().Text("INVOICE")
                        .Bold().FontSize(18).FontColor(NavyPrimary);

                    right.Item().PaddingTop(6).AlignRight().Text(txt =>
                    {
                        txt.Span("No:   ").FontSize(8).FontColor(Gray500);
                        txt.Span(_data.InvoiceNumber).FontSize(9).Bold().FontColor(NavyPrimary);
                    });

                    right.Item().PaddingTop(3).AlignRight().Text(txt =>
                    {
                        txt.Span("Date:   ").FontSize(8).FontColor(Gray500);
                        txt.Span(_data.InvoiceDate.ToString("dd MMM yyyy"))
                            .FontSize(8).Bold().FontColor(Gray900);
                    });

                    right.Item().PaddingTop(3).AlignRight().Text(txt =>
                    {
                        txt.Span("Due:   ").FontSize(8).FontColor(Gray500);
                        txt.Span(_data.DueDate.ToString("dd MMM yyyy"))
                            .FontSize(8).FontColor(Gray700);
                    });

                    // Status badge (colored text)
                    right.Item().PaddingTop(6).AlignRight().Text(_data.Status.Replace("_", " "))
                        .Bold().FontSize(8).FontColor(GetStatusColor());
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor(NavyPrimary);
        });
    }

    // ── CONTENT ────────────────────────────────────────────────────────────────
    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(16).Column(col =>
        {
            // Bill To + Invoice Details side by side
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeBillTo);
                row.ConstantItem(16);
                row.RelativeItem().Element(ComposeInvoiceDetails);
            });

            col.Item().PaddingTop(22).Element(ComposeItemsTable);

            if (_data.Payments.Count > 0)
                col.Item().PaddingTop(18).Element(ComposePaymentsTable);

            if (!string.IsNullOrWhiteSpace(_data.Notes))
                col.Item().PaddingTop(14).Element(ComposeNotes);
        });
    }

    // ── BILL TO ────────────────────────────────────────────────────────────────
    private void ComposeBillTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("BILL TO").Bold().FontSize(7).FontColor(NavyPrimary);
            col.Item().PaddingTop(5).Text(_data.CustomerName)
                .Bold().FontSize(13).FontColor(Gray900);
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Gray200);

            if (!string.IsNullOrWhiteSpace(_data.CustomerPhone))
                col.Item().PaddingTop(5).Element(c => InfoRow(c, "Phone", _data.CustomerPhone));
            if (!string.IsNullOrWhiteSpace(_data.CustomerEmail))
                col.Item().PaddingTop(3).Element(c => InfoRow(c, "Email", _data.CustomerEmail));
            if (!string.IsNullOrWhiteSpace(_data.CustomerAddress))
                col.Item().PaddingTop(3).Element(c => InfoRow(c, "Address", _data.CustomerAddress));
            if (!string.IsNullOrWhiteSpace(_data.VehicleLabel))
                col.Item().PaddingTop(3).Element(c => InfoRow(c, "Vehicle", _data.VehicleLabel));
            if (!string.IsNullOrWhiteSpace(_data.TechnicianName))
                col.Item().PaddingTop(3).Element(c => InfoRow(c, "Technician", _data.TechnicianName));
        });
    }

    // ── INVOICE DETAILS ────────────────────────────────────────────────────────
    private void ComposeInvoiceDetails(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("INVOICE DETAILS").Bold().FontSize(7).FontColor(Gray500);
            col.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Gray200);

            col.Item().PaddingTop(8).Element(c => InfoRow(c, "Invoice No", _data.InvoiceNumber));
            if (!string.IsNullOrWhiteSpace(_data.SalesOrderNumber))
                col.Item().PaddingTop(4).Element(c => InfoRow(c, "Sales Order", _data.SalesOrderNumber));
            col.Item().PaddingTop(4).Element(c => InfoRow(c, "Date", _data.InvoiceDate.ToString("dd MMM yyyy")));
            col.Item().PaddingTop(4).Element(c => InfoRow(c, "Due Date", _data.DueDate.ToString("dd MMM yyyy")));
        });
    }

    // ── ITEMS TABLE ────────────────────────────────────────────────────────────
    // Relative column widths: total = 52 units → 515pt content
    //   SL(3) · Description(22) · Qty(5) · UnitPrice(8) · Disc%(5) · Amount(9)
    private void ComposeItemsTable(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1.5f).BorderColor(NavyPrimary).PaddingBottom(6)
                .Text("ITEMS").Bold().FontSize(10).FontColor(NavyPrimary);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);   // SL#
                    c.RelativeColumn(22);  // Description + SKU
                    c.RelativeColumn(5);   // Qty
                    c.RelativeColumn(8);   // Unit Price
                    c.RelativeColumn(5);   // Disc%
                    c.RelativeColumn(9);   // Amount
                });

                table.Header(header =>
                {
                    void H(IContainer c, string text, bool right = false)
                    {
                        var cell = c.Background(NavyPrimary)
                            .PaddingHorizontal(5).PaddingVertical(7);
                        (right ? cell.AlignRight().Text(text) : cell.Text(text))
                            .Bold().FontSize(8).FontColor(White);
                    }

                    H(header.Cell(), "#");
                    H(header.Cell(), "Description");
                    H(header.Cell(), "Qty", right: true);
                    H(header.Cell(), "Unit Price", right: true);
                    H(header.Cell(), "Discount", right: true);
                    H(header.Cell(), "Amount", right: true);
                });

                bool alt = false;
                foreach (var line in _data.Lines)
                {
                    string bg = alt ? Gray50 : White;
                    alt = !alt;
                    const float hp = 5f, vp = 5f, bs = 0.5f;

                    // SL#
                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(line.SlNo.ToString()).FontSize(8).FontColor(Gray500);

                    // Description (name + part no / SKU below)
                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Column(c =>
                        {
                            c.Item().Text(line.DisplayName).FontSize(8).FontColor(Gray900);
                            if (!string.IsNullOrWhiteSpace(line.LocalName))
                                c.Item().PaddingTop(1).Text(line.LocalName).FontSize(7f).FontColor(Gray500);
                            var sub = !string.IsNullOrWhiteSpace(line.PartNumber)
                                ? line.PartNumber
                                : line.SKU;
                            if (!string.IsNullOrWhiteSpace(sub))
                                c.Item().PaddingTop(1).Text(sub).FontSize(7.5f).FontColor(Gray400);
                        });

                    // Qty
                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp).AlignRight()
                        .Text(line.Quantity % 1 == 0
                            ? ((int)line.Quantity).ToString()
                            : line.Quantity.ToString("N2"))
                        .FontSize(8).FontColor(Gray700);

                    // Unit Price
                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp).AlignRight()
                        .Text($"{_shop.CurrencySymbol} {line.UnitPrice:N2}").FontSize(8).FontColor(Gray700);

                    // Discount (per-unit absolute amount)
                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp).AlignRight()
                        .Text(line.DiscountPerUnit > 0 ? $"({_shop.CurrencySymbol} {line.DiscountPerUnit:N2})" : "—")
                        .FontSize(8).FontColor(line.DiscountPerUnit > 0 ? GreenText : Gray400);

                    // Amount
                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp).AlignRight()
                        .Text($"{_shop.CurrencySymbol} {line.LineTotal:N2}").FontSize(8).Bold().FontColor(Gray900);
                }

                // Totals footer spanning all columns
                AppendTotalsFooter(table);
            });
        });
    }

    // ── TOTALS FOOTER (inside items table) ────────────────────────────────────
    private void AppendTotalsFooter(TableDescriptor table)
    {
        void SummaryRow(string label, string value, string labelColor, string valueColor, bool topBorder = false)
        {
            var labelCell = table.Cell().ColumnSpan(5)
                .Background(Gray100)
                .PaddingHorizontal(5).PaddingVertical(5)
                .AlignRight();
            if (topBorder)
                labelCell = labelCell.BorderTop(1f).BorderColor(Gray300);
            labelCell.Text(label).FontSize(8).FontColor(labelColor);

            var valueCell = table.Cell()
                .Background(Gray100)
                .PaddingHorizontal(5).PaddingVertical(5)
                .AlignRight();
            if (topBorder)
                valueCell = valueCell.BorderTop(1f).BorderColor(Gray300);
            valueCell.Text(value).FontSize(8).FontColor(valueColor);
        }

        // Subtotal
        SummaryRow("Subtotal", $"{_shop.CurrencySymbol} {_data.SubTotal:N2}", Gray500, Gray700);

        // Discount (only if > 0)
        if (_data.DiscountAmount > 0)
            SummaryRow("Discount", $"({_shop.CurrencySymbol} {_data.DiscountAmount:N2})", Gray500, GreenText);

        // Tax (only if > 0)
        if (_data.TaxAmount > 0)
        {
            var taxLabel = _data.TaxPercentage > 0 ? $"Tax ({_data.TaxPercentage:N0}%)" : "Tax";
            SummaryRow(taxLabel, $"{_shop.CurrencySymbol} {_data.TaxAmount:N2}", Gray500, Gray700);
        }

        // Grand Total (navy background)
        table.Cell().ColumnSpan(5)
            .Background(NavyPrimary).PaddingHorizontal(5).PaddingVertical(8)
            .AlignRight()
            .Text("GRAND TOTAL").Bold().FontSize(8.5f).FontColor(White);
        table.Cell()
            .Background(NavyPrimary).PaddingHorizontal(5).PaddingVertical(8)
            .AlignRight()
            .Text($"{_shop.CurrencySymbol} {_data.GrandTotal:N2}").Bold().FontSize(8.5f).FontColor(White);

        // Paid (only if > 0)
        if (_data.PaidAmount > 0)
            SummaryRow("Paid", $"({_shop.CurrencySymbol} {_data.PaidAmount:N2})", GreenText, GreenText);

        // Balance Due (separator + bold)
        var balanceColor = _data.BalanceDue <= 0 ? GreenText : Gray900;
        var balanceLabel = _data.BalanceDue <= 0 ? "SETTLED" : "BALANCE DUE";
        table.Cell().ColumnSpan(5)
            .Background(Gray100).BorderTop(1f).BorderColor(Gray300)
            .PaddingHorizontal(5).PaddingVertical(8)
            .AlignRight()
            .Text(balanceLabel).Bold().FontSize(9).FontColor(balanceColor);
        table.Cell()
            .Background(Gray100).BorderTop(1f).BorderColor(Gray300)
            .PaddingHorizontal(5).PaddingVertical(8)
            .AlignRight()
            .Text($"{_shop.CurrencySymbol} {Math.Abs(_data.BalanceDue):N2}").Bold().FontSize(9).FontColor(balanceColor);
    }

    // ── PAYMENTS RECEIVED ──────────────────────────────────────────────────────
    private void ComposePaymentsTable(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1f).BorderColor(Gray200).PaddingBottom(5)
                .Text("PAYMENTS RECEIVED").Bold().FontSize(9).FontColor(Gray700);

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(10); // Date
                    c.RelativeColumn(10); // Method
                    c.RelativeColumn(22); // Reference
                    c.RelativeColumn(10); // Amount
                });

                table.Header(header =>
                {
                    void H(IContainer c, string text, bool right = false)
                    {
                        var cell = c.Background(Gray100)
                            .PaddingHorizontal(5).PaddingVertical(6);
                        (right ? cell.AlignRight().Text(text) : cell.Text(text))
                            .Bold().FontSize(7.5f).FontColor(Gray700);
                    }

                    H(header.Cell(), "Date");
                    H(header.Cell(), "Method");
                    H(header.Cell(), "Reference");
                    H(header.Cell(), "Amount", right: true);
                });

                bool alt = false;
                foreach (var p in _data.Payments)
                {
                    string bg = alt ? Gray50 : White;
                    alt = !alt;
                    const float hp = 5f, vp = 5f, bs = 0.5f;

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(p.PaymentDate.ToString("dd MMM yyyy")).FontSize(8).FontColor(Gray700);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(FormatPaymentMethod(p.Method)).FontSize(8).FontColor(Gray700);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp)
                        .Text(string.IsNullOrWhiteSpace(p.Reference) ? "—" : p.Reference)
                        .FontSize(8).FontColor(Gray500);

                    table.Cell().Background(bg).BorderBottom(bs).BorderColor(Gray200)
                        .PaddingHorizontal(hp).PaddingVertical(vp).AlignRight()
                        .Text($"{_shop.CurrencySymbol} {p.Amount:N2}").FontSize(8).Bold().FontColor(GreenText);
                }
            });
        });
    }

    // ── NOTES ──────────────────────────────────────────────────────────────────
    private void ComposeNotes(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(5).Text("NOTES").Bold().FontSize(7).FontColor(Gray500);
            col.Item().Background(Gray50).Border(0.5f).BorderColor(Gray200)
                .Padding(8)
                .Text(_data.Notes).FontSize(8).Italic().FontColor(Gray700);
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
                row.RelativeItem().Column(left =>
                {
                    var footerText = string.IsNullOrWhiteSpace(_shop.FooterText)
                        ? "Thank you for your business!"
                        : _shop.FooterText;
                    left.Item().Text(footerText).FontSize(8f).Italic().FontColor(Gray400);
                    left.Item().PaddingTop(2)
                        .Text($"Generated {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC")
                        .FontSize(7f).FontColor(Gray300);
                });

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

    // ── Helpers ────────────────────────────────────────────────────────────────
    private string GetStatusColor() => _data.Status switch
    {
        "PAID" => GreenText,
        "ISSUED" => NavyPrimary,
        "DUE" => AmberText,
        "PARTIALLY_PAID" => AmberText,
        "OVERDUE" => RedText,
        "CANCELLED" => Gray400,
        _ => Gray700
    };

    private static string FormatPaymentMethod(string method) => method switch
    {
        "CASH" => "Cash",
        "CARD" => "Card",
        "MOBILE_BANKING" => "Mobile",
        "BANK_TRANSFER" => "Bank Transfer",
        "ADVANCE_CREDIT" => "Credit Applied",
        _ => method
    };

    private static void InfoRow(IContainer c, string label, string value)
    {
        c.Text(txt =>
        {
            txt.Span($"{label}:  ").FontSize(8).FontColor(Gray500);
            txt.Span(value).FontSize(8).Bold().FontColor(Gray700);
        });
    }
}
