using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
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
    decimal TaxPercentage,   // informational; shown as "VAT (X%)" label
    decimal TaxAmount,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    List<InvoicePaymentEntry> Payments,
    string Notes);

/// <summary>
/// Tax Invoice — document 5 of design_handoff_pos_documents.
/// Header + Bill To / Payment terms + items table + payments + terms footer + signature row.
/// </summary>
public class InvoiceDocument : IDocument
{
    private readonly InvoiceDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public InvoiceDocument(InvoiceDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice {_data.InvoiceNumber}",
        Author = _shop.Name,
        Subject = $"Invoice for {_data.CustomerName}",
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
        new DocHeader(_theme, _shop, _data.TaxAmount > 0 ? _theme.T("invoice.taxInvoiceTitle") : _theme.T("invoice.title"),
        [
            new MetaField(_theme.T("common.no"), _data.InvoiceNumber),
            new MetaField(_theme.T("common.date"), _data.InvoiceDate.ToString("dd MMM yyyy")),
            new MetaField(_theme.T("common.paymentDue"), _data.DueDate.ToString("dd MMM yyyy")),
            new MetaField(_theme.T("common.refOrder"), _data.SalesOrderNumber),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeBillTo);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposePaymentTerms);
            });

            col.Item().PaddingTop(DocTheme.Px(20)).Element(ComposeItems);

            // ShowEntire keeps each block whole, so a section label can never be orphaned at the
            // foot of a page with its content on the next one.
            if (_data.Payments.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(16)).ShowEntire().Element(ComposePayments);

            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeBankAndTerms);

            col.Item().ShowEntire().Element(c =>
                new SignRow(_theme.T("common.preparedBy"), _theme.T("common.customerSignature"), _theme.T("common.authorizedSignatory")).Compose(c));
        });
    }

    // ── Bill To ────────────────────────────────────────────────────────────────
    private void ComposeBillTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("common.billTo")));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.CustomerName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            // A stacked address block, as in the handoff — not a label/value grid. Contact details
            // run full width so long addresses don't wrap against a label column.
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data.CustomerAddress)) lines.Add(_data.CustomerAddress);

            // Phone and email each get their own line — this column is half-width, so joining them
            // with a separator wraps and strands the separator at the end of a line.
            if (!string.IsNullOrWhiteSpace(_data.CustomerPhone)) lines.Add(_data.CustomerPhone);
            if (!string.IsNullOrWhiteSpace(_data.CustomerEmail)) lines.Add(_data.CustomerEmail);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
                {
                    foreach (var line in lines)
                        c.Item().Text(line)
                            .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
                });

            // Vehicle and technician are job context rather than address, so they stay labelled.
            if (!string.IsNullOrWhiteSpace(_data.VehicleLabel))
                col.Item().PaddingTop(DocTheme.Px(4)).Element(c => InfoRow(c, _theme.T("common.vehicle"), _data.VehicleLabel));
            if (!string.IsNullOrWhiteSpace(_data.TechnicianName))
                col.Item().PaddingTop(DocTheme.Px(2)).Element(c => InfoRow(c, _theme.T("common.technician"), _data.TechnicianName));
        });
    }

    // ── Payment terms (right column) ───────────────────────────────────────────
    private void ComposePaymentTerms(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("common.terms")));

            var creditDays = Math.Max(0, (_data.DueDate.Date - _data.InvoiceDate.Date).Days);

            col.Item().PaddingTop(DocTheme.Px(6)).Element(c => InfoRow(c, _theme.T("common.status"), _data.Status.Replace('_', ' ')));
            col.Item().PaddingTop(DocTheme.Px(3)).Element(c => InfoRow(c, _theme.T("common.credit"), creditDays == 0 ? _theme.T("common.dueOnReceipt") : $"{creditDays} {_theme.T("common.days")}"));

            if (_data.PaidAmount > 0)
                col.Item().PaddingTop(DocTheme.Px(3)).Element(c => InfoRow(c, _theme.T("common.paid"), _theme.Money(_data.PaidAmount)));

            col.Item().PaddingTop(DocTheme.Px(3)).Element(c => InfoRow(
                c,
                _data.BalanceDue <= 0 ? _theme.T("common.settled") : _theme.T("common.balanceDue"),
                _theme.Money(Math.Abs(_data.BalanceDue))));

            if (_data.TaxAmount > 0)
                col.Item().PaddingTop(DocTheme.Px(6)).Text(_theme.T("invoice.vatNote"))
                    .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label);
        });
    }

    // ── Items ──────────────────────────────────────────────────────────────────
    private void ComposeItems(IContainer container)
    {
        var items = _data.Lines.Select(l => new ItemRow(
            Sn: l.SlNo,
            Code: !string.IsNullOrWhiteSpace(l.PartNumber) ? l.PartNumber : l.SKU,
            Name: string.IsNullOrWhiteSpace(l.LocalName) ? l.DisplayName : $"{l.DisplayName}\n{l.LocalName}",
            Qty: FormatQty(l.Quantity, l.UnitSymbol),
            Rate: DocTheme.Amount(l.UnitPrice),
            Amount: DocTheme.Amount(l.LineTotal))).ToList();

        var totals = new List<TotalRow> { new(_theme.T("common.subtotal"), DocTheme.Amount(_data.SubTotal)) };

        if (_data.DiscountAmount > 0)
            totals.Add(new TotalRow(_theme.T("common.discount"), $"({DocTheme.Amount(_data.DiscountAmount)})"));

        if (_data.TaxAmount > 0)
            totals.Add(new TotalRow(
                _data.TaxPercentage > 0 ? $"{_theme.T("common.vat")} ({_data.TaxPercentage:N0}%)" : _theme.T("common.vat"),
                DocTheme.Amount(_data.TaxAmount)));

        new ItemsTable(
            _theme, items, totals,
            grandLabel: _theme.T("common.grandTotal"),
            grandValue: DocTheme.Amount(_data.GrandTotal),
            words: AmountInWords.Convert(_data.GrandTotal)).Compose(container);
    }

    // ── Payments received ──────────────────────────────────────────────────────
    private void ComposePayments(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("invoice.paymentsReceived")));

            col.Item().PaddingTop(DocTheme.Px(8)).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(DocTheme.Px(90));
                    c.ConstantColumn(DocTheme.Px(110));
                    c.RelativeColumn();
                    c.ConstantColumn(DocTheme.Px(110));
                });

                table.Header(header =>
                {
                    Head(header.Cell(), _theme.T("common.date"));
                    Head(header.Cell(), _theme.T("common.method"));
                    Head(header.Cell(), _theme.T("common.reference"));
                    Head(header.Cell(), _theme.T("common.amount"), right: true);
                });

                foreach (var p in _data.Payments)
                {
                    Cell(table.Cell(), p.PaymentDate.ToString("dd MMM yyyy"), mono: true);
                    Cell(table.Cell(), FormatPaymentMethod(p.Method));
                    Cell(table.Cell(), string.IsNullOrWhiteSpace(p.Reference) ? "—" : p.Reference, mono: true);
                    Cell(table.Cell(), _theme.Money(p.Amount), mono: true, right: true);
                }
            });
        });

        static void Head(IContainer cell, string text, bool right = false)
        {
            var c = cell.BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8));
            if (right) c = c.AlignRight();
            c.Text(text.ToUpperInvariant())
                .FontSize(DocTheme.TableHeader).SemiBold().FontColor(DocTheme.Secondary)
                .LetterSpacing(1.2f / DocTheme.TableHeader);
        }

        static void Cell(IContainer cell, string text, bool mono = false, bool right = false)
        {
            var c = cell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8));
            if (right) c = c.AlignRight();
            var span = c.Text(text).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
            if (mono) span.Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
        }
    }

    // ── Bank Details + Terms (two-column footer, per the handoff) ──────────────
    private void ComposeBankAndTerms(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => Block(c, _theme.T("common.bankDetails"), _shop.BankDetails));
            row.ConstantItem(DocTheme.Px(24));
            row.RelativeItem().Element(c => Block(c, _theme.T("common.terms"), TermsText()));
        });

        // The handoff's Bank Details block is fixed text; ours comes from SHOP_BANK_DETAILS and is
        // skipped while unset, so the column simply stays empty rather than printing a placeholder
        // account number.
        static void Block(IContainer c, string label, string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return;

            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, label));
                col.Item().PaddingTop(DocTheme.Px(5)).Text(body)
                    .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
            });
        }
    }

    /// <summary>Invoice notes when present, else the handoff's standing terms.</summary>
    private string TermsText() =>
        !string.IsNullOrWhiteSpace(_data.Notes)
            ? _data.Notes
            : _theme.T("invoice.standingTerms");

    // ── Footer ─────────────────────────────────────────────────────────────────
    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(10)).Row(row =>
        {
            row.RelativeItem().Text(
                    string.IsNullOrWhiteSpace(_shop.FooterText)
                        ? _theme.T("common.thankYouBusiness")
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
    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);

    private static void InfoRow(IContainer c, string label, string value) =>
        c.Row(row =>
        {
            row.ConstantItem(DocTheme.Px(70)).Text(label)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            row.RelativeItem().Text(value)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });

    private static string FormatQty(decimal qty, string unitSymbol)
    {
        var n = qty % 1 == 0 ? ((int)qty).ToString() : qty.ToString("N2");
        return string.IsNullOrWhiteSpace(unitSymbol) ? n : $"{n} {unitSymbol}";
    }

    private string FormatPaymentMethod(string method) => method switch
    {
        "CASH" => _theme.T("common.paymentMethod.cash"),
        "CARD" => _theme.T("common.paymentMethod.card"),
        "MOBILE_BANKING" => _theme.T("common.paymentMethod.mobile"),
        "BANK_TRANSFER" => _theme.T("common.paymentMethod.bankTransfer"),
        "ADVANCE_CREDIT" => _theme.T("common.paymentMethod.creditApplied"),
        _ => method
    };
}
