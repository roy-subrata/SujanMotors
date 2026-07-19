using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Application.DTOs.ReportDtos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record StockReportDocumentData(
    string ReportNumber,
    DateTime AsOf,
    string WarehouseLabel,
    IReadOnlyList<StockSummaryRowDto> Rows,
    decimal TotalStockValue);

/// <summary>
/// Stock Report — document 9 of design_handoff_pos_documents.
/// Inventory snapshot with a LOW/OK status pill per line and a total-value footer.
/// </summary>
public class StockReportDocument : IDocument
{
    private readonly StockReportDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public StockReportDocument(StockReportDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Stock Report {_data.ReportNumber}",
        Author = _shop.Name,
        Subject = "Inventory snapshot",
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
        new DocHeader(_theme, _shop, "Stock Report",
        [
            new MetaField("No.", _data.ReportNumber),
            new MetaField("Date", _data.AsOf.ToString("dd MMM yyyy")),
            new MetaField("As Of", _data.AsOf.ToString("dd MMM yyyy, h:mm tt")),
            new MetaField("Warehouse", _data.WarehouseLabel),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeTable);
            col.Item().PaddingTop(DocTheme.Px(18)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Store Keeper", "Checked By", "Manager").Compose(c));
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(DocTheme.Px(100));  // Part No
                c.RelativeColumn();                  // Description
                c.ConstantColumn(DocTheme.Px(90));   // Category
                // Wider than the handoff's 60px: the uppercase letter-spaced headers "ON HAND" and
                // "REORDER" wrap at that width. Description is relative and absorbs the difference.
                c.ConstantColumn(DocTheme.Px(78));   // On Hand
                c.ConstantColumn(DocTheme.Px(78));   // Reorder
                c.ConstantColumn(DocTheme.Px(90));   // Value
                c.ConstantColumn(DocTheme.Px(64));   // Status
            });

            table.Header(header =>
            {
                Head(header.Cell(), "Part No");
                Head(header.Cell(), "Description");
                Head(header.Cell(), "Category");
                Head(header.Cell(), "On Hand", align: Align.Right);
                Head(header.Cell(), "Reorder", align: Align.Right);
                Head(header.Cell(), $"Value ({_theme.CurrencySymbol})", align: Align.Right);
                Head(header.Cell(), "Status", align: Align.Center);
            });

            foreach (var r in _data.Rows)
            {
                Cell(table.Cell(), r.PartNumber, mono: true, size: DocTheme.Px(10));

                Body(table.Cell()).Text(ComposeName(r))
                    .FontSize(DocTheme.Px(10.5f)).FontColor(DocTheme.Ink);

                Cell(table.Cell(), r.CategoryName ?? "—", color: DocTheme.Muted);
                Cell(table.Cell(), r.QuantityOnHand.ToString(), mono: true, align: Align.Right);
                Cell(table.Cell(), r.ReorderLevel > 0 ? r.ReorderLevel.ToString() : "—",
                    mono: true, align: Align.Right, color: DocTheme.Label);
                Cell(table.Cell(), DocTheme.Amount(r.StockValue), mono: true, align: Align.Right);

                Body(table.Cell()).AlignCenter().Element(c => Pill(c, IsLow(r)));
            }

            // Total-value footer: 2px ink rule, label spanning the leading five columns.
            table.Cell().ColumnSpan(5)
                .BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                .PaddingVertical(DocTheme.Px(9)).PaddingHorizontal(DocTheme.Px(8))
                .Text("Total Stock Value").FontSize(DocTheme.Px(10.5f)).Bold().FontColor(DocTheme.Ink);

            table.Cell()
                .BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                .PaddingVertical(DocTheme.Px(9)).PaddingHorizontal(DocTheme.Px(8)).AlignRight()
                .Text(DocTheme.Amount(_data.TotalStockValue))
                .Style(DocTheme.MonoText).FontSize(DocTheme.Px(10.5f)).Bold().FontColor(DocTheme.Ink);

            table.Cell().BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink);
        });
    }

    /// <summary>Accent-filled LOW pill, neutral outlined OK pill.</summary>
    private void Pill(IContainer container, bool low)
    {
        var box = container
            .PaddingVertical(DocTheme.Px(2)).PaddingHorizontal(DocTheme.Px(6));

        if (low)
            box.Background(_theme.Accent)
                .AlignCenter()
                .Text("LOW")
                .FontSize(DocTheme.Px(8.5f)).Bold().FontColor(DocTheme.White)
                .LetterSpacing(0.8f / 8.5f);
        else
            box.Border(DocTheme.RuleHairline).BorderColor(DocTheme.Divider)
                .AlignCenter()
                .Text("OK")
                .FontSize(DocTheme.Px(8.5f)).SemiBold().FontColor(DocTheme.Muted)
                .LetterSpacing(0.8f / 8.5f);
    }

    /// <summary>
    /// A reorder level of 0 means the item is opted out of reorder tracking, so it is never LOW —
    /// otherwise every zero-threshold item with no stock would flag.
    /// </summary>
    private static bool IsLow(StockSummaryRowDto r) =>
        r.ReorderLevel > 0 && r.QuantityOnHand <= r.ReorderLevel;

    private static string ComposeName(StockSummaryRowDto r) =>
        string.IsNullOrWhiteSpace(r.VariantName) ? r.PartName : $"{r.PartName} - {r.VariantName}";

    private void ComposeNote(IContainer container) =>
        container.Text(txt =>
        {
            txt.DefaultTextStyle(x => x.FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f));
            txt.Span("Items marked ");
            txt.Span("LOW").Bold();
            txt.Span(" are at or below their reorder level — include them in the next Purchase Order. " +
                     "Values are at last purchase cost.");
        });

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

    // ── Table helpers ──────────────────────────────────────────────────────────
    private enum Align { Left, Right, Center }

    private static void Head(IContainer cell, string text, Align align = Align.Left)
    {
        var c = cell
            .BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8));

        c = align switch
        {
            Align.Right => c.AlignRight(),
            Align.Center => c.AlignCenter(),
            _ => c
        };

        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.TableHeader).SemiBold().FontColor(DocTheme.Secondary)
            .LetterSpacing(1.2f / DocTheme.TableHeader);
    }

    private static IContainer Body(IContainer cell) =>
        cell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8));

    private static void Cell(
        IContainer cell,
        string text,
        bool mono = false,
        Align align = Align.Left,
        float? size = null,
        string color = DocTheme.Ink)
    {
        var fontSize = size ?? DocTheme.Px(10.5f);
        var c = Body(cell);

        c = align switch
        {
            Align.Right => c.AlignRight(),
            Align.Center => c.AlignCenter(),
            _ => c
        };

        var span = c.Text(text).FontSize(fontSize).FontColor(color);
        if (mono) span.Style(DocTheme.MonoText).FontSize(fontSize).FontColor(color);
    }
}
