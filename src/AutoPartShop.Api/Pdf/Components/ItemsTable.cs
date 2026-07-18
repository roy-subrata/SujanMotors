using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf.Components;

/// <summary>One goods line. Rate/Amount are pre-formatted by the caller so documents keep control
/// of rounding and of the no-price variant (Delivery Challan).</summary>
public readonly record struct ItemRow(
    int Sn,
    string Code,
    string Name,
    string Qty,
    string Rate,
    string Amount);

/// <summary>A label/value row in the totals stack (subtotal, discount, VAT, ...).</summary>
public readonly record struct TotalRow(string Label, string Value);

/// <summary>
/// Shared line-items table + totals block — port of ItemsTable.dc.html.
/// Columns: # | Part No | Description | Qty | Rate | Amount.
/// </summary>
public sealed class ItemsTable
{
    private readonly DocTheme _theme;
    private readonly IReadOnlyList<ItemRow> _items;
    private readonly IReadOnlyList<TotalRow> _totals;
    private readonly string _grandLabel;
    private readonly string _grandValue;
    private readonly string? _words;

    public ItemsTable(
        DocTheme theme,
        IReadOnlyList<ItemRow> items,
        IReadOnlyList<TotalRow> totals,
        string grandLabel,
        string grandValue,
        string? words = null)
    {
        _theme = theme;
        _items = items;
        _totals = totals;
        _grandLabel = grandLabel;
        _grandValue = grandValue;
        _words = words;
    }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(ComposeTable);
            col.Item().PaddingTop(DocTheme.Px(10)).Element(ComposeTotals);

            if (!string.IsNullOrWhiteSpace(_words))
                col.Item().PaddingTop(DocTheme.Px(10)).Element(ComposeWords);
        });
    }

    // ── Goods table ────────────────────────────────────────────────────────────
    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            // Widths from the handoff: #=30, Part No=105, Qty=55, Rate=95, Amount=110,
            // Description takes the remainder.
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(DocTheme.Px(30));
                c.ConstantColumn(DocTheme.Px(105));
                c.RelativeColumn();
                // Wider than the handoff's 55px: it shows a bare number, whereas we append the
                // unit ("200 pcs"), which matters for parts sold by set/litre. Description is
                // relative, so it absorbs the difference.
                c.ConstantColumn(DocTheme.Px(78));
                c.ConstantColumn(DocTheme.Px(95));
                c.ConstantColumn(DocTheme.Px(110));
            });

            table.Header(header =>
            {
                Head(header.Cell(), "#");
                Head(header.Cell(), "Part No");
                Head(header.Cell(), "Description");
                Head(header.Cell(), "Qty", right: true);
                Head(header.Cell(), $"Rate ({_theme.CurrencySymbol})", right: true);
                Head(header.Cell(), $"Amount ({_theme.CurrencySymbol})", right: true);
            });

            foreach (var it in _items)
            {
                Body(table.Cell(), it.Sn.ToString(), mono: true, color: DocTheme.Label);
                Body(table.Cell(), it.Code, mono: true, size: DocTheme.TableCode);
                Body(table.Cell(), it.Name);
                Body(table.Cell(), it.Qty, mono: true, right: true);
                Body(table.Cell(), it.Rate, mono: true, right: true);
                Body(table.Cell(), it.Amount, mono: true, right: true, medium: true);
            }
        });
    }

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

    private static void Body(
        IContainer cell,
        string text,
        bool mono = false,
        bool right = false,
        bool medium = false,
        float? size = null,
        string color = DocTheme.Ink)
    {
        // DocTheme sizes are computed (px→pt), so they can't be compile-time default arguments.
        var fontSize = size ?? DocTheme.TableCell;

        var c = cell
            .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .Padding(DocTheme.Px(8));

        if (right) c = c.AlignRight();

        var span = c.Text(text).FontSize(fontSize).FontColor(color);
        if (mono) span = span.Style(DocTheme.MonoText).FontSize(fontSize).FontColor(color);
        if (medium) span.Medium();
    }

    // ── Totals stack (right-aligned, 300pt) ────────────────────────────────────
    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Width(DocTheme.TotalsWidth).Column(col =>
        {
            foreach (var t in _totals)
            {
                col.Item().PaddingVertical(DocTheme.Px(5)).PaddingHorizontal(DocTheme.Px(8)).Row(row =>
                {
                    row.RelativeItem().Text(t.Label)
                        .FontSize(DocTheme.TableCell).FontColor(DocTheme.Secondary);
                    row.AutoItem().Text(t.Value)
                        .Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Secondary);
                });
            }

            col.Item().PaddingTop(DocTheme.Px(4))
                .BorderTop(DocTheme.RuleMedium).BorderBottom(DocTheme.RuleMedium)
                .BorderColor(DocTheme.Ink)
                .Padding(DocTheme.Px(8))
                .Row(row =>
                {
                    row.RelativeItem().Text(_grandLabel)
                        .FontSize(DocTheme.GrandTotal).Bold().FontColor(DocTheme.Ink);
                    row.AutoItem().Text($"{_theme.CurrencySymbol} {_grandValue}")
                        .Style(DocTheme.MonoText).FontSize(DocTheme.GrandTotal).Bold().FontColor(DocTheme.Ink);
                });
        });
    }

    // ── "In words" line ────────────────────────────────────────────────────────
    private void ComposeWords(IContainer container)
    {
        container.Text(txt =>
        {
            txt.Span("IN WORDS  ")
                .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
                .LetterSpacing(1f / DocTheme.SectionLabel);
            txt.Span(_words)
                .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary);
        });
    }
}
