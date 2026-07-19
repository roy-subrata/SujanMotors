using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record PurchaseOrderDocumentLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    decimal Quantity,
    string UnitSymbol,
    decimal UnitPrice,
    decimal LineTotal);

public record PurchaseOrderDocumentData(
    string PONumber,
    DateTime PODate,
    DateTime ExpectedDeliveryDate,
    string PaymentTerms,
    string SupplierName,
    string SupplierAddress,
    string SupplierPhone,
    string SupplierTaxNo,
    string DeliverToName,
    string DeliverToAddress,
    List<PurchaseOrderDocumentLine> Lines,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxPercentage,
    decimal TaxAmount,
    decimal TotalAmount,
    string Notes);

/// <summary>
/// Purchase Order — document 8 of design_handoff_pos_documents.
/// Priced at cost. Header + Supplier / Deliver To + items + numbered conditions + signature row.
/// </summary>
public class PurchaseOrderDocument : IDocument
{
    private readonly PurchaseOrderDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public PurchaseOrderDocument(PurchaseOrderDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Purchase Order {_data.PONumber}",
        Author = _shop.Name,
        Subject = $"Purchase order to {_data.SupplierName}",
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
        new DocHeader(_theme, _shop, "Purchase Order",
        [
            new MetaField("No.", _data.PONumber),
            new MetaField("Date", _data.PODate.ToString("dd MMM yyyy")),
            new MetaField("Delivery By", _data.ExpectedDeliveryDate.ToString("dd MMM yyyy")),
            new MetaField("Payment", _data.PaymentTerms),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeSupplier);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeDeliverTo);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeConditions);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Prepared By", "Approved By", "Supplier Acknowledgement").Compose(c));
        });
    }

    private void ComposeSupplier(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Supplier"));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.SupplierName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            Stack(col,
            [
                _data.SupplierAddress,
                _data.SupplierPhone,
                _data.SupplierTaxNo,
            ]);
        });
    }

    private void ComposeDeliverTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Deliver To"));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.DeliverToName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            Stack(col, [_data.DeliverToAddress]);
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
            grandLabel: "PO Total",
            grandValue: DocTheme.Amount(_data.TotalAmount),
            words: AmountInWords.Convert(_data.TotalAmount)).Compose(container);
    }

    private void ComposeConditions(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Conditions"));

            // PO notes replace the standing conditions when the buyer has written their own.
            var conditions = !string.IsNullOrWhiteSpace(_data.Notes)
                ? [_data.Notes]
                : new[]
                {
                    "1. Supply genuine parts only; counterfeit items will be rejected at supplier's cost.",
                    $"2. Deliver with challan and Mushak-6.3 invoice quoting this PO number ({_data.PONumber}).",
                    "3. Short or damaged supply must be replaced within 7 days.",
                };

            col.Item().PaddingTop(DocTheme.Px(5)).Column(c =>
            {
                foreach (var line in conditions)
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
    private static void Stack(ColumnDescriptor col, IReadOnlyList<string> lines)
    {
        var present = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (present.Count == 0) return;

        col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
        {
            foreach (var line in present)
                c.Item().Text(line)
                    .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
        });
    }

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
