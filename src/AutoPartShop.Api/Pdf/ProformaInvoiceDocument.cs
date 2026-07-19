using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record ProformaInvoiceDocumentLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    decimal Quantity,
    string UnitSymbol,
    decimal UnitPrice,
    decimal LineTotal);

public record ProformaInvoiceDocumentData(
    string ProformaNumber,
    DateTime IssueDate,
    DateTime ValidUntil,
    string RefOrderNumber,
    string CustomerName,
    string CustomerAddress,
    string CustomerPhone,
    List<ProformaInvoiceDocumentLine> Lines,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string Notes);

/// <summary>
/// Proforma Invoice — document 3 of design_handoff_pos_documents. Pre-payment bill referencing a
/// confirmed SalesOrder. Header + Bill To + items (label "Total Payable") + two-column
/// Bank Details / Note footer + signature row.
///
/// Backed by ProformaInvoice, which carries no line items of its own — Lines/totals here are read
/// live from the linked SalesOrder by the caller, so a proforma can never drift from the order it
/// references.
/// </summary>
public class ProformaInvoiceDocument : IDocument
{
    private readonly ProformaInvoiceDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public ProformaInvoiceDocument(ProformaInvoiceDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Proforma Invoice {_data.ProformaNumber}",
        Author = _shop.Name,
        Subject = $"Proforma invoice for {_data.CustomerName}",
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
        new DocHeader(_theme, _shop, "Proforma Invoice",
        [
            new MetaField("No.", _data.ProformaNumber),
            new MetaField("Date", _data.IssueDate.ToString("dd MMM yyyy")),
            new MetaField("Valid Until", _data.ValidUntil.ToString("dd MMM yyyy")),
            new MetaField("Ref. Order", _data.RefOrderNumber),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeBillTo);
            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeFooterBlocks);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Prepared By", "Checked By", "Authorized Signatory").Compose(c));
        });
    }

    private void ComposeBillTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Bill To"));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.CustomerName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data.CustomerAddress)) lines.Add(_data.CustomerAddress);
            if (!string.IsNullOrWhiteSpace(_data.CustomerPhone)) lines.Add(_data.CustomerPhone);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
                {
                    foreach (var line in lines)
                        c.Item().Text(line)
                            .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
                });
        });
    }

    private void ComposeItems(IContainer container)
    {
        var items = _data.Lines.Select(l => new ItemRow(
            Sn: l.SlNo,
            Code: l.PartNumber,
            Name: string.IsNullOrWhiteSpace(l.LocalName) ? l.DisplayName : $"{l.DisplayName}\n{l.LocalName}",
            Qty: FormatQty(l.Quantity, l.UnitSymbol),
            Rate: DocTheme.Amount(l.UnitPrice),
            Amount: DocTheme.Amount(l.LineTotal))).ToList();

        var totals = new List<TotalRow> { new("Subtotal", DocTheme.Amount(_data.SubTotal)) };

        if (_data.DiscountAmount > 0)
            totals.Add(new TotalRow("Discount", $"({DocTheme.Amount(_data.DiscountAmount)})"));

        if (_data.TaxAmount > 0)
            totals.Add(new TotalRow("VAT", DocTheme.Amount(_data.TaxAmount)));

        new ItemsTable(
            _theme, items, totals,
            grandLabel: "Total Payable",
            grandValue: DocTheme.Amount(_data.GrandTotal),
            words: AmountInWords.Convert(_data.GrandTotal)).Compose(container);
    }

    // ── Bank Details + Note (two-column footer) ────────────────────────────────
    private void ComposeFooterBlocks(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => Block(c, "Bank Details", _shop.BankDetails));
            row.ConstantItem(DocTheme.Px(24));
            row.RelativeItem().Element(c => Block(c, "Note", NoteText()));
        });

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

    private string NoteText() => !string.IsNullOrWhiteSpace(_data.Notes)
        ? _data.Notes
        : "This is a proforma invoice — not a tax invoice.\n50% advance payable to confirm the order.";

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
    private static string FormatQty(decimal qty, string unitSymbol)
    {
        var n = qty % 1 == 0 ? ((int)qty).ToString() : qty.ToString("N2");
        return string.IsNullOrWhiteSpace(unitSymbol) ? n : $"{n} {unitSymbol}";
    }

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);
}
