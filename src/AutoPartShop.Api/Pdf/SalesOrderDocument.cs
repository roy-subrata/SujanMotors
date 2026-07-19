using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record SalesOrderDocumentLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    decimal Quantity,
    string UnitSymbol,
    decimal UnitPrice,
    decimal DiscountPerUnit,
    decimal LineTotal);

public record SalesOrderDocumentData(
    string SONumber,
    DateTime SODate,
    DateTime? DeliveryBy,
    string CustomerPO,
    string CustomerName,
    string BillToAddress,
    string BillToPhone,
    string ShipToName,
    string ShipToAddress,
    string ShipToContact,
    List<SalesOrderDocumentLine> Lines,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxPercentage,
    decimal TaxAmount,
    decimal TotalAmount,
    string Notes);

/// <summary>
/// Sales Order — document 2 of design_handoff_pos_documents.
/// Internal order confirmation. Header + Bill To / Ship To + items + note + signature row.
/// </summary>
public class SalesOrderDocument : IDocument
{
    private readonly SalesOrderDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public SalesOrderDocument(SalesOrderDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Sales Order {_data.SONumber}",
        Author = _shop.Name,
        Subject = $"Sales order for {_data.CustomerName}",
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
        new DocHeader(_theme, _shop, "Sales Order",
        [
            new MetaField("No.", _data.SONumber),
            new MetaField("Date", _data.SODate.ToString("dd MMM yyyy")),
            // Customer PO has no home in the domain yet — MetaField skips blank values, so this
            // simply doesn't render rather than showing a placeholder.
            new MetaField("Customer PO", _data.CustomerPO),
            new MetaField("Delivery By", _data.DeliveryBy?.ToString("dd MMM yyyy") ?? ""),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeBillTo);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeShipTo);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Prepared By", "Customer Confirmation", "Authorized Signatory").Compose(c));
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
            if (!string.IsNullOrWhiteSpace(_data.BillToAddress)) lines.Add(_data.BillToAddress);
            if (!string.IsNullOrWhiteSpace(_data.BillToPhone)) lines.Add(_data.BillToPhone);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
                {
                    foreach (var line in lines)
                        c.Item().Text(line)
                            .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
                });
        });
    }

    private void ComposeShipTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Ship To"));

            // Falls back to the customer's own name/address when the order carries no separate
            // delivery address — an order shipping to the billing address is the common case.
            var name = string.IsNullOrWhiteSpace(_data.ShipToName) ? _data.CustomerName : _data.ShipToName;
            var address = string.IsNullOrWhiteSpace(_data.ShipToAddress) ? _data.BillToAddress : _data.ShipToAddress;

            col.Item().PaddingTop(DocTheme.Px(6)).Text(name)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(address)) lines.Add(address);
            if (!string.IsNullOrWhiteSpace(_data.ShipToContact)) lines.Add(_data.ShipToContact);

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
            totals.Add(new TotalRow(
                _data.TaxPercentage > 0 ? $"VAT ({_data.TaxPercentage:N0}%)" : "VAT",
                DocTheme.Amount(_data.TaxAmount)));

        new ItemsTable(
            _theme, items, totals,
            grandLabel: "Order Total",
            grandValue: DocTheme.Amount(_data.TotalAmount),
            words: AmountInWords.Convert(_data.TotalAmount)).Compose(container);
    }

    private void ComposeNote(IContainer container)
    {
        var note = !string.IsNullOrWhiteSpace(_data.Notes)
            ? _data.Notes
            : "Order confirmed against customer PO. Goods will be delivered with Delivery Challan; Tax Invoice to follow on delivery.";

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
