using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Application.CustomerPayment.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

/// <summary>
/// Extra detail the handoff's receipt shows but <see cref="CustomerPaymentResponse"/> does not
/// carry. Supplied by the controller when the payment is linked to an invoice; rows are omitted
/// when the value is null so the receipt still renders for standalone payments.
/// </summary>
public record PaymentReceiptContext(
    decimal? InvoiceTotal = null,
    decimal? BalanceDue = null,
    string CustomerAddress = "",
    string CustomerPhone = "");

/// <summary>
/// Payment Receipt — document 6 of design_handoff_pos_documents.
/// Header + Received From + amount highlight box + in-words + detail table + note + signature row.
/// </summary>
public class PaymentReceiptDocument : IDocument
{
    private readonly CustomerPaymentResponse _payment;
    private readonly ShopProfile _shop;
    private readonly PaymentReceiptContext _context;
    private readonly DocTheme _theme;

    public PaymentReceiptDocument(
        CustomerPaymentResponse payment,
        ShopProfile shop,
        PaymentReceiptContext? context = null,
        DocTheme? theme = null)
    {
        _payment = payment;
        _shop = shop;
        _context = context ?? new PaymentReceiptContext();
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Payment Receipt – {_payment.TransactionNumber}",
        Author = _shop.Name,
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
        new DocHeader(_theme, _shop, _theme.T("receipt.title"),
        [
            new MetaField(_theme.T("common.no"), _payment.TransactionNumber),
            new MetaField(_theme.T("common.date"), _payment.PaymentDate.ToString("dd MMM yyyy")),
            // The handoff shows just the brand here ("bKash"); the long form goes in the detail table.
            new MetaField(_theme.T("receipt.mode"), ShortMode()),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeReceivedFrom);
            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeAmountBox);
            col.Item().PaddingTop(DocTheme.Px(12)).Element(ComposeWords);
            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeDetails);
            col.Item().PaddingTop(DocTheme.Px(20)).Element(ComposeNote);
            col.Item().ShowEntire().Element(c =>
                new SignRow(_theme.T("common.receivedBy"), _theme.T("common.customerSignature"), _theme.T("common.authorizedSignatory")).Compose(c));
        });
    }

    // ── Received From ──────────────────────────────────────────────────────────
    private void ComposeReceivedFrom(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text(_theme.T("receipt.receivedFrom").ToUpperInvariant())
                .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
                .LetterSpacing(1.2f / DocTheme.SectionLabel);

            col.Item().PaddingTop(DocTheme.Px(5)).Text(_payment.CustomerName)
                .FontSize(DocTheme.TableCell).Bold().FontColor(DocTheme.Ink).LineHeight(1.6f);

            foreach (var line in new[] { _context.CustomerAddress, _context.CustomerPhone })
            {
                if (!string.IsNullOrWhiteSpace(line))
                    col.Item().Text(line)
                        .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink).LineHeight(1.6f);
            }
        });
    }

    // ── Amount highlight box (2px ink border, 26pt accent mono figure) ─────────
    private void ComposeAmountBox(IContainer container)
    {
        container
            .Border(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
            .PaddingVertical(DocTheme.Px(18)).PaddingHorizontal(DocTheme.Px(22))
            .Row(row =>
            {
                row.RelativeItem().AlignMiddle()
                    .Text(_theme.T("receipt.amountReceived").ToUpperInvariant())
                    .FontSize(DocTheme.Px(10)).SemiBold().FontColor(DocTheme.Muted)
                    .LetterSpacing(1.5f / 10f);

                row.AutoItem().AlignRight().AlignMiddle()
                    .Text(_theme.Money(_payment.Amount))
                    .Style(DocTheme.MonoText).FontSize(DocTheme.Px(26)).Bold().FontColor(_theme.Accent);
            });
    }

    private void ComposeWords(IContainer container) =>
        container.Text(txt =>
        {
            txt.Span($"{_theme.T("common.inWords").ToUpperInvariant()}  ")
                .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
                .LetterSpacing(1f / DocTheme.SectionLabel);
            txt.Span(AmountInWords.Convert(_payment.Amount))
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });

    // ── Detail table ───────────────────────────────────────────────────────────
    private void ComposeDetails(IContainer container)
    {
        var rows = new List<(string Label, string Value, bool Mono, bool Bold)>
        {
            (_theme.T("receipt.paymentMode"), PaymentMode(), false, false),
        };

        if (!string.IsNullOrWhiteSpace(_payment.ReferenceNumber))
            rows.Add((_theme.T("receipt.transactionRef"), _payment.ReferenceNumber, true, false));
        else if (!string.IsNullOrWhiteSpace(_payment.TransactionNumber))
            rows.Add((_theme.T("receipt.transactionRef"), _payment.TransactionNumber, true, false));

        if (!string.IsNullOrWhiteSpace(_payment.AuthorizationCode))
            rows.Add((_theme.T("receipt.authCode"), _payment.AuthorizationCode, true, false));

        if (!string.IsNullOrWhiteSpace(_payment.InvoiceNumber))
            rows.Add((_theme.T("receipt.againstInvoice"), _payment.InvoiceNumber, true, false));

        if (_payment.PaymentFee > 0)
        {
            rows.Add((_theme.T("receipt.transactionFee"), _theme.Money(_payment.PaymentFee), true, false));
            rows.Add((_theme.T("receipt.netAmount"), _theme.Money(_payment.NetAmount), true, false));
        }

        if (_context.InvoiceTotal is { } total)
            rows.Add((_theme.T("receipt.invoiceTotal"), _theme.Money(total), true, false));

        if (_context.BalanceDue is { } balance)
            rows.Add((_theme.T("common.balanceDue"), _theme.Money(balance), true, true));

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(DocTheme.Px(200));
                c.RelativeColumn();
            });

            foreach (var (label, value, mono, bold) in rows)
            {
                table.Cell()
                    .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8))
                    .Text(label).FontSize(DocTheme.TableCell).FontColor(DocTheme.Label);

                var cell = table.Cell()
                    .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8))
                    .Text(value).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);

                if (mono) cell = cell.Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
                if (bold) cell.SemiBold();
            }
        });
    }

    // ── Closing note ───────────────────────────────────────────────────────────
    private void ComposeNote(IContainer container)
    {
        string note;

        if (!string.IsNullOrWhiteSpace(_payment.Notes))
        {
            note = _payment.Notes;
        }
        else if (!string.IsNullOrWhiteSpace(_payment.InvoiceNumber))
        {
            note = _context.BalanceDue is { } b && b <= 0
                ? _theme.T("receipt.standingNoteSettled").Replace("{{invoiceNumber}}", _payment.InvoiceNumber)
                : _theme.T("receipt.standingNoteUnsettled").Replace("{{invoiceNumber}}", _payment.InvoiceNumber);
        }
        else
        {
            note = _theme.T("receipt.standingNoteDefault");
        }

        container.Text(note)
            .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(10)).Text(
                string.IsNullOrWhiteSpace(_shop.FooterText)
                    ? _theme.T("receipt.thankYouPayment")
                    : _shop.FooterText)
            .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label);
    }

    /// <summary>
    /// The handoff shows the provider brand ("bKash") where available, since that is what a
    /// customer recognises; the raw method enum is the fallback.
    /// </summary>
    private string ShortMode() =>
        string.IsNullOrWhiteSpace(_payment.ProviderName) ? MethodName() : _payment.ProviderName;

    private string PaymentMode()
    {
        var method = MethodName();
        return string.IsNullOrWhiteSpace(_payment.ProviderName)
            ? method
            : $"{_payment.ProviderName} ({method})";
    }

    private string MethodName()
    {
        return _payment.PaymentMethod switch
        {
            "CASH" => _theme.T("common.paymentMethod.cash"),
            "CARD" => _theme.T("common.paymentMethod.card"),
            "MOBILE_BANKING" => _theme.T("common.paymentMethod.mobileBanking"),
            "BANK_TRANSFER" => _theme.T("common.paymentMethod.bankTransfer"),
            "ADVANCE_CREDIT" => _theme.T("common.paymentMethod.creditApplied"),
            _ => _payment.PaymentMethod.Replace('_', ' ')
        };
    }
}
