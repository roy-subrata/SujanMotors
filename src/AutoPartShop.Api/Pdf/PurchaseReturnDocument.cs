using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record PurchaseReturnDocumentLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    int Quantity,
    decimal UnitPrice,
    decimal RefundAmount);

public record PurchaseReturnDocumentData(
    string ReturnNumber,
    DateTime ReturnDate,
    string PONumber,
    string Reason,
    string Status,
    string SupplierName,
    string SupplierAddress,
    string SupplierPhone,
    List<PurchaseReturnDocumentLine> Lines,
    decimal RefundAmount,
    string Notes);

/// <summary>
/// Purchase Return — goods sent back to a supplier. Mirrors the Purchase Order layout (Supplier
/// block + items table priced at cost) since it's the reverse of the same transaction.
/// </summary>
public class PurchaseReturnDocument : IDocument
{
    private readonly PurchaseReturnDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public PurchaseReturnDocument(PurchaseReturnDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Purchase Return {_data.ReturnNumber}",
        Author = _shop.Name,
        Subject = $"Purchase return to {_data.SupplierName}",
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
        new DocHeader(_theme, _shop, _theme.T("purchaseReturn.title"),
        [
            new MetaField(_theme.T("common.no"), _data.ReturnNumber),
            new MetaField(_theme.T("common.date"), _data.ReturnDate.ToString("dd MMM yyyy")),
            new MetaField(_theme.T("common.refPO"), _data.PONumber),
            new MetaField(_theme.T("common.status"), _data.Status),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeSupplier);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeReason);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);

            if (!string.IsNullOrWhiteSpace(_data.Notes))
                col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNotes);

            col.Item().ShowEntire().Element(c =>
                new SignRow(_theme.T("common.preparedBy"), _theme.T("common.approvedBy"), _theme.T("common.supplierAcknowledgement")).Compose(c));
        });
    }

    private void ComposeSupplier(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("purchaseReturn.returnToSupplier")));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.SupplierName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data.SupplierAddress)) lines.Add(_data.SupplierAddress);
            if (!string.IsNullOrWhiteSpace(_data.SupplierPhone)) lines.Add(_data.SupplierPhone);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
                {
                    foreach (var line in lines)
                        c.Item().Text(line)
                            .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
                });
        });
    }

    private void ComposeReason(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("purchaseReturn.reasonForReturn")));
            col.Item().PaddingTop(DocTheme.Px(6)).Text(
                    string.IsNullOrWhiteSpace(_data.Reason) ? "—" : _data.Reason)
                .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink).LineHeight(1.6f);
        });
    }

    private void ComposeItems(IContainer container)
    {
        var items = _data.Lines.Select(l => new ItemRow(
            Sn: l.SlNo,
            Code: l.PartNumber,
            Name: string.IsNullOrWhiteSpace(l.LocalName) ? l.DisplayName : $"{l.DisplayName}\n{l.LocalName}",
            Qty: l.Quantity.ToString(),
            Rate: DocTheme.Amount(l.UnitPrice),
            Amount: DocTheme.Amount(l.RefundAmount))).ToList();

        new ItemsTable(
            _theme, items, totals: [],
            grandLabel: _theme.T("common.totalRefund"),
            grandValue: DocTheme.Amount(_data.RefundAmount),
            words: AmountInWords.Convert(_data.RefundAmount)).Compose(container);
    }

    private void ComposeNotes(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("common.notes")));
            col.Item().PaddingTop(DocTheme.Px(5)).Text(_data.Notes)
                .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
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

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);
}
