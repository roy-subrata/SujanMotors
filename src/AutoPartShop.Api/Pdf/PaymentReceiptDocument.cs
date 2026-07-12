using AutoPartShop.Application.CustomerPayment.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public class PaymentReceiptDocument : IDocument
{
    // ── Palette (matches CustomerAccountStatementDocument) ─────────────────────
    private const string NavyPrimary = "#1e3a8a";
    private const string GreenBg = "#f0fdf4";
    private const string GreenBorder = "#86efac";
    private const string GreenText = "#15803d";
    private const string Gray200 = "#e5e7eb";
    private const string Gray300 = "#d1d5db";
    private const string Gray400 = "#9ca3af";
    private const string Gray500 = "#6b7280";
    private const string Gray700 = "#374151";
    private const string Gray900 = "#111827";
    private const string White = "#FFFFFF";

    private readonly CustomerPaymentResponse _payment;
    private readonly ShopProfile _shop;

    public PaymentReceiptDocument(CustomerPaymentResponse payment, ShopProfile shop)
    {
        _payment = payment;
        _shop = shop;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Payment Receipt – {_payment.TransactionNumber}",
        Author = _shop.Name,
        CreationDate = DateTime.UtcNow
    };

    // ── Page setup ─────────────────────────────────────────────────────────────
    // A5: 420pt wide. Margins 32pt each side → 356pt usable content width.
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.MarginHorizontal(32);
            page.MarginVertical(28);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Gray900));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── HEADER ─────────────────────────────────────────────────────────────────
    // Left col: company identity (RelativeItem, ~221pt)
    // Right col: document title block (135pt)
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

                // Document title block (right column, 135pt)
                // "PAYMENT RECEIPT" at 13pt bold ≈ 113pt — safe in 135pt.
                row.ConstantItem(135).Column(right =>
                {
                    right.Item().AlignRight().Text("PAYMENT RECEIPT")
                        .Bold().FontSize(13).FontColor(NavyPrimary);

                    right.Item().PaddingTop(10).AlignRight().Text(txt =>
                    {
                        txt.Span("Date:   ").FontSize(8).FontColor(Gray500);
                        txt.Span(_payment.PaymentDate.ToString("dd MMM yyyy"))
                            .FontSize(8).Bold().FontColor(Gray900);
                    });

                    right.Item().PaddingTop(3).AlignRight().Text(txt =>
                    {
                        txt.Span("Ref:   ").FontSize(8).FontColor(Gray500);
                        txt.Span(_payment.TransactionNumber)
                            .FontSize(8).FontColor(Gray700);
                    });
                });
            });

            // Navy rule (matches SOA header separator)
            col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor(NavyPrimary);
        });
    }

    // ── CONTENT ────────────────────────────────────────────────────────────────
    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(16).Column(col =>
        {
            // RECEIVED FROM — plain heading + name + divider (matches SOA Bill To style)
            col.Item().Column(c =>
            {
                c.Item().Text("RECEIVED FROM").Bold().FontSize(7).FontColor(NavyPrimary);
                c.Item().PaddingTop(5).Text(_payment.CustomerName)
                    .Bold().FontSize(13).FontColor(Gray900);
                c.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Gray200);
            });

            // Payment detail rows
            // Label: RelativeItem (~226pt). Value: ConstantItem(120).
            // Longest value: "BANK_TRANSFER" → "BANK TRANSFER" = 13 chars ≈ 65pt at 8pt. Fits in 120pt.
            col.Item().PaddingTop(14).Column(details =>
            {
                void DetailRow(IContainer r, string label, string value)
                {
                    r.BorderBottom(0.5f).BorderColor(Gray200)
                        .PaddingVertical(6)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text(label).FontSize(8).FontColor(Gray500);
                            row.ConstantItem(120).AlignRight()
                                .Text(value).FontSize(8).FontColor(Gray700);
                        });
                }

                details.Item().Element(r => DetailRow(r, "Payment Method", FormatMethod(_payment.PaymentMethod)));
                details.Item().Element(r => DetailRow(r, "Status", _payment.Status));
                if (!string.IsNullOrWhiteSpace(_payment.Currency))
                    details.Item().Element(r => DetailRow(r, "Currency", _payment.Currency));

                if (!string.IsNullOrWhiteSpace(_payment.InvoiceNumber))
                    details.Item().Element(r => DetailRow(r, "Invoice Reference", _payment.InvoiceNumber));

                if (!string.IsNullOrWhiteSpace(_payment.ProviderName))
                    details.Item().Element(r => DetailRow(r, "Payment Provider", _payment.ProviderName));

                if (!string.IsNullOrWhiteSpace(_payment.ReferenceNumber))
                    details.Item().Element(r => DetailRow(r, "Reference No", _payment.ReferenceNumber));

                if (!string.IsNullOrWhiteSpace(_payment.AuthorizationCode))
                    details.Item().Element(r => DetailRow(r, "Auth Code", _payment.AuthorizationCode));

                if (_payment.PaymentFee > 0)
                    details.Item().Element(r => DetailRow(r, "Transaction Fee", $"{_payment.PaymentFee:N2}"));
            });

            // AMOUNT RECEIVED — prominent green box (full content width, 356pt)
            // Label: RelativeItem (~206pt). Amount: ConstantItem(140).
            // "100,000.00" at 16pt bold ≈ 10 chars × 9pt = 90pt — safe in 140pt.
            col.Item().PaddingTop(20)
                .Border(1f).BorderColor(GreenBorder)
                .Background(GreenBg)
                .PaddingHorizontal(16).PaddingVertical(14)
                .Row(row =>
                {
                    row.RelativeItem().AlignMiddle()
                        .Text("AMOUNT RECEIVED")
                        .Bold().FontSize(9).FontColor(GreenText);

                    row.ConstantItem(140).AlignRight().AlignMiddle()
                        .Text($"{_shop.CurrencySymbol} {_payment.Amount:N2}")
                        .Bold().FontSize(16).FontColor(GreenText);
                });

            // Net amount (shown only when a fee was charged)
            if (_payment.PaymentFee > 0)
            {
                col.Item().PaddingTop(5).AlignRight().Text(txt =>
                {
                    txt.Span("Net Amount:   ").FontSize(8).FontColor(Gray500);
                    txt.Span($"{_shop.CurrencySymbol} {_payment.NetAmount:N2}").FontSize(8).Bold().FontColor(Gray700);
                });
            }

            // Notes
            if (!string.IsNullOrWhiteSpace(_payment.Notes))
            {
                col.Item().PaddingTop(16).Column(n =>
                {
                    n.Item().Text("Notes").Bold().FontSize(7).FontColor(Gray500);
                    n.Item().PaddingTop(3).Text(_payment.Notes)
                        .FontSize(8).Italic().FontColor(Gray700);
                });
            }

            // Signature lines — two equal-width cols with a 20pt spacer
            // Each col: (356 − 20) / 2 = 168pt. Plenty for label text.
            col.Item().PaddingTop(32).Row(sig =>
            {
                sig.RelativeItem().Column(left =>
                {
                    left.Item().LineHorizontal(0.5f).LineColor(Gray300);
                    left.Item().PaddingTop(5)
                                .Text("Authorised Signature").FontSize(8f).FontColor(Gray400);
                });

                sig.ConstantItem(20);

                sig.RelativeItem().Column(right =>
                {
                    right.Item().LineHorizontal(0.5f).LineColor(Gray300);
                    right.Item().PaddingTop(5)
                        .Text("Received By").FontSize(8f).FontColor(Gray400);
                });
            });
        });
    }

    // ── FOOTER ─────────────────────────────────────────────────────────────────
    private void ComposeFooter(IContainer container)
    {
        var footerText = string.IsNullOrWhiteSpace(_shop.FooterText)
            ? "Thank you for your payment."
            : _shop.FooterText;

        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Gray200);

            col.Item().PaddingTop(6).AlignCenter()
                .Text(footerText).FontSize(8).Italic().FontColor(Gray400);

            col.Item().PaddingTop(2).AlignCenter()
                .Text($"Computer-generated receipt. Generated {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC")
                .FontSize(7f).FontColor(Gray300);
        });
    }

    private static string FormatMethod(string method) =>
        method.Replace("_", " ").ToUpperInvariant();
}
