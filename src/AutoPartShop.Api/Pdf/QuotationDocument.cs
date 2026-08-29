using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record QuotationDocumentLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    decimal Quantity,
    string UnitSymbol,
    decimal UnitPrice,
    decimal LineTotal);

public record QuotationDocumentData(
    string QuotationNumber,
    DateTime QuoteDate,
    DateTime ValidUntil,
    string CustomerName,
    string CustomerAddress,
    string CustomerPhone,
    List<QuotationDocumentLine> Lines,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string Notes);

/// <summary>
/// Quotation — document 1 of design_handoff_pos_documents.
/// Header + Quotation To (single block, not a two-column split) + items + numbered Terms &amp;
/// Conditions + signature row.
/// </summary>
public class QuotationDocument : IDocument
{
    private readonly QuotationDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public QuotationDocument(QuotationDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Quotation {_data.QuotationNumber}",
        Author = _shop.Name,
        Subject = $"Quotation for {_data.CustomerName}",
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
        new DocHeader(_theme, _shop, _theme.T("quotation.title"),
        [
            new MetaField(_theme.T("common.no"), _data.QuotationNumber),
            new MetaField(_theme.T("common.date"), _data.QuoteDate.ToString("dd MMM yyyy")),
            new MetaField(_theme.T("common.validUntil"), _data.ValidUntil.ToString("dd MMM yyyy")),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeQuotationTo);
            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeTerms);

            col.Item().ShowEntire().Element(c =>
                new SignRow(_theme.T("common.preparedBy"), _theme.T("common.checkedBy"), _theme.T("common.authorizedSignatory")).Compose(c));
        });
    }

    private void ComposeQuotationTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("quotation.quotationTo")));

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

        var totals = new List<TotalRow> { new(_theme.T("common.subtotal"), DocTheme.Amount(_data.SubTotal)) };

        if (_data.DiscountAmount > 0)
            totals.Add(new TotalRow(_theme.T("common.discount"), $"({DocTheme.Amount(_data.DiscountAmount)})"));

        if (_data.TaxAmount > 0)
            totals.Add(new TotalRow(_theme.T("common.vat"), DocTheme.Amount(_data.TaxAmount)));

        new ItemsTable(
            _theme, items, totals,
            grandLabel: _theme.T("common.total"),
            grandValue: DocTheme.Amount(_data.GrandTotal),
            words: AmountInWords.Convert(_data.GrandTotal)).Compose(container);
    }

    private void ComposeTerms(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("common.termsConditions")));

            var terms = !string.IsNullOrWhiteSpace(_data.Notes)
                ? [_data.Notes]
                : _theme.T("quotation.standingTerms").Split('\n');

            col.Item().PaddingTop(DocTheme.Px(5)).Column(c =>
            {
                foreach (var line in terms)
                    c.Item().Text(line)
                        .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
            });
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
