using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record ChallanDocumentLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    int Quantity,
    string UnitName);

public record ChallanDocumentData(
    string ChallanNumber,
    DateTime ChallanDate,
    string SalesOrderNumber,
    string InvoiceNumber,
    string CustomerName,
    string DeliveryAddress,
    string ReceiverName,
    string ReceiverPhone,
    string TransportCompany,
    string VehicleNumber,
    string DriverName,
    string DriverPhone,
    DateTime? DispatchedAt,
    List<ChallanDocumentLine> Lines,
    string Notes);

/// <summary>
/// Delivery Challan — document 4 of design_handoff_pos_documents.
/// Deliberately shows no prices: quantities only, with the invoice referenced for pricing.
/// </summary>
public class DeliveryChallanDocument : IDocument
{
    private readonly ChallanDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public DeliveryChallanDocument(ChallanDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Delivery Challan {_data.ChallanNumber}",
        Author = _shop.Name,
        Subject = $"Delivery challan for {_data.CustomerName}",
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
        new DocHeader(_theme, _shop, "Delivery Challan",
        [
            new MetaField("No.", _data.ChallanNumber),
            new MetaField("Date", _data.ChallanDate.ToString("dd MMM yyyy")),
            new MetaField("Ref. Order", _data.SalesOrderNumber),
            new MetaField("Vehicle", _data.VehicleNumber),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeDeliverTo);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeDispatchDetails);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Prepared By", "Driver", "Received By (Customer)").Compose(c));
        });
    }

    // ── Deliver To ─────────────────────────────────────────────────────────────
    private void ComposeDeliverTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Deliver To"));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.CustomerName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data.DeliveryAddress)) lines.Add(_data.DeliveryAddress);
            if (!string.IsNullOrWhiteSpace(_data.ReceiverName)) lines.Add($"Attn: {_data.ReceiverName}");
            if (!string.IsNullOrWhiteSpace(_data.ReceiverPhone)) lines.Add(_data.ReceiverPhone);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
                {
                    foreach (var line in lines)
                        c.Item().Text(line)
                            .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
                });
        });
    }

    // ── Dispatch details ───────────────────────────────────────────────────────
    private void ComposeDispatchDetails(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Dispatch Details"));

            var rows = new List<(string, string)>();
            if (!string.IsNullOrWhiteSpace(_data.TransportCompany)) rows.Add(("Transport", _data.TransportCompany));
            if (!string.IsNullOrWhiteSpace(_data.VehicleNumber)) rows.Add(("Vehicle", _data.VehicleNumber));
            if (!string.IsNullOrWhiteSpace(_data.DriverName)) rows.Add(("Driver", _data.DriverName));
            if (!string.IsNullOrWhiteSpace(_data.DriverPhone)) rows.Add(("Driver Ph.", _data.DriverPhone));
            if (_data.DispatchedAt is { } at) rows.Add(("Dispatched", at.ToString("dd MMM yyyy, HH:mm")));

            if (rows.Count == 0)
            {
                col.Item().PaddingTop(DocTheme.Px(6)).Text("—")
                    .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
                return;
            }

            var first = true;
            foreach (var (label, value) in rows)
            {
                col.Item().PaddingTop(DocTheme.Px(first ? 6 : 2)).Element(c => InfoRow(c, label, value));
                first = false;
            }
        });
    }

    // ── Items (no prices — quantities and units only) ──────────────────────────
    private void ComposeItems(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(DocTheme.Px(30));   // #
                    c.ConstantColumn(DocTheme.Px(125));  // Part No
                    c.RelativeColumn();                  // Description
                    c.ConstantColumn(DocTheme.Px(70));   // Qty
                    c.ConstantColumn(DocTheme.Px(90));   // Unit
                });

                table.Header(header =>
                {
                    Head(header.Cell(), "#");
                    Head(header.Cell(), "Part No");
                    Head(header.Cell(), "Description");
                    Head(header.Cell(), "Qty", right: true);
                    Head(header.Cell(), "Unit");
                });

                foreach (var line in _data.Lines)
                {
                    Cell(table.Cell(), line.SlNo.ToString(), mono: true, color: DocTheme.Label);
                    Cell(table.Cell(), line.PartNumber, mono: true, size: DocTheme.TableCode);

                    Body(table.Cell()).Column(c =>
                    {
                        c.Item().Text(line.DisplayName).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
                        if (!string.IsNullOrWhiteSpace(line.LocalName))
                            c.Item().Text(line.LocalName)
                                .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Muted);
                    });

                    Cell(table.Cell(), line.Quantity.ToString(), mono: true, right: true);
                    Cell(table.Cell(), line.UnitName);
                }

                // Totalled quantity footer, per the handoff: 2px ink rule over the Qty and Unit
                // cells only, with the leading three columns left blank.
                table.Cell().ColumnSpan(3).Padding(DocTheme.Px(8));

                table.Cell().BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                    .Padding(DocTheme.Px(8)).AlignRight()
                    .Text(_data.Lines.Sum(l => l.Quantity).ToString())
                    .Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).SemiBold().FontColor(DocTheme.Ink);

                table.Cell().BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                    .Padding(DocTheme.Px(8))
                    .Text("Total").FontSize(DocTheme.TableCell).SemiBold().FontColor(DocTheme.Ink);
            });
        });
    }

    private void ComposeNote(IContainer container)
    {
        var note = !string.IsNullOrWhiteSpace(_data.Notes)
            ? _data.Notes
            : string.IsNullOrWhiteSpace(_data.InvoiceNumber)
                ? "Goods listed above are dispatched in good condition. Prices are not shown on this challan. Please verify quantities on receipt and sign below."
                : $"Goods listed above are dispatched in good condition. Prices are not shown on this challan — refer to Tax Invoice {_data.InvoiceNumber}. Please verify quantities on receipt and sign below.";

        container.Text(note)
            .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(10)).Row(row =>
        {
            row.RelativeItem().Text(_shop.FooterText).FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label);
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
    private static void Head(IContainer cell, string text, bool right = false)
    {
        var c = cell
            .BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8));

        if (right) c = c.AlignRight();

        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.TableHeader).SemiBold().FontColor(DocTheme.Secondary)
            .LetterSpacing(1.2f / DocTheme.TableHeader);
    }

    private static IContainer Body(IContainer cell) =>
        cell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8));

    private static void Cell(
        IContainer cell,
        string text,
        bool mono = false,
        bool right = false,
        float? size = null,
        string color = DocTheme.Ink)
    {
        var fontSize = size ?? DocTheme.TableCell;
        var c = Body(cell);
        if (right) c = c.AlignRight();

        var span = c.Text(text).FontSize(fontSize).FontColor(color);
        if (mono) span.Style(DocTheme.MonoText).FontSize(fontSize).FontColor(color);
    }

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);

    private static void InfoRow(IContainer c, string label, string value) =>
        c.Row(row =>
        {
            row.ConstantItem(DocTheme.Px(80)).Text(label)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Label);
            row.RelativeItem().Text(value)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });
}
