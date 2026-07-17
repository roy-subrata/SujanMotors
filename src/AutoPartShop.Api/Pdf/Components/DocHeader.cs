using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf.Components;

/// <summary>A label/value pair in the header's right-hand meta grid.</summary>
public readonly record struct MetaField(string Label, string Value);

/// <summary>
/// Shared document header — port of DocHeader.dc.html.
/// Left: accent logo mark + company block. Right: document title + meta grid.
/// Closes with a 3px ink rule.
/// </summary>
public sealed class DocHeader
{
    private readonly DocTheme _theme;
    private readonly ShopProfile _shop;
    private readonly string _title;
    private readonly IReadOnlyList<MetaField> _meta;

    /// <summary>
    /// Meta fields render in order. The handoff shows No. and Date first, then up to two extra
    /// fields — pass them in that order. Fields with a blank value are skipped, matching the sc-if
    /// in the original markup.
    /// </summary>
    public DocHeader(DocTheme theme, ShopProfile shop, string title, IReadOnlyList<MetaField> meta)
    {
        _theme = theme;
        _shop = shop;
        _title = title;
        _meta = meta;
    }

    public void Compose(IContainer container)
    {
        container
            .BorderBottom(DocTheme.RuleHeavy).BorderColor(DocTheme.Ink)
            .PaddingBottom(DocTheme.Px(14))
            .Row(row =>
            {
                row.RelativeItem().Element(ComposeIdentity);
                row.ConstantItem(DocTheme.Px(24));
                row.AutoItem().Element(ComposeTitleAndMeta);
            });
    }

    // ── Left: logo mark + company block ────────────────────────────────────────
    private void ComposeIdentity(IContainer container)
    {
        container.Row(row =>
        {
            // 46x46 accent square with initials. The handoff notes this is a placeholder for the
            // real logo mark.
            row.ConstantItem(DocTheme.Px(46)).Height(DocTheme.Px(46))
                .Background(_theme.Accent)
                .AlignCenter().AlignMiddle()
                .Text(Initials(_shop.Name))
                .FontSize(DocTheme.Px(19)).Bold().FontColor(DocTheme.White).LetterSpacing(0.5f / 19f);

            row.ConstantItem(DocTheme.Px(12));

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_shop.Name.ToUpperInvariant())
                    .FontSize(DocTheme.CompanySize).Bold().FontColor(DocTheme.Ink)
                    .LetterSpacing(2f / DocTheme.CompanySize).LineHeight(1.1f);

                if (!string.IsNullOrWhiteSpace(_shop.Tagline))
                    col.Item().PaddingTop(DocTheme.Px(2)).Text(_shop.Tagline.ToUpperInvariant())
                        .FontSize(DocTheme.TaglineSize).SemiBold().FontColor(_theme.Accent)
                        .LetterSpacing(1.5f / DocTheme.TaglineSize);

                var address = BuildAddressBlock();
                if (address.Count > 0)
                    col.Item().PaddingTop(DocTheme.Px(5)).Column(a =>
                    {
                        foreach (var line in address)
                            a.Item().Text(line)
                                .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Muted)
                                .LineHeight(1.55f);
                    });
            });
        });
    }

    /// <summary>
    /// Address / contact / registration lines. The handoff shows three lines; blanks collapse
    /// rather than leaving gaps.
    /// </summary>
    private List<string> BuildAddressBlock()
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(_shop.Address))
            lines.Add(_shop.Address);

        var contact = new List<string>();
        if (!string.IsNullOrWhiteSpace(_shop.Phone)) contact.Add(_shop.Phone);
        if (!string.IsNullOrWhiteSpace(_shop.Email)) contact.Add(_shop.Email);
        if (contact.Count > 0) lines.Add(string.Join("  ·  ", contact));

        if (!string.IsNullOrWhiteSpace(_shop.TaxNo))
            lines.Add(_shop.TaxNo);

        return lines;
    }

    // ── Right: title + meta grid ───────────────────────────────────────────────
    private void ComposeTitleAndMeta(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignRight().Text(_title.ToUpperInvariant())
                .FontSize(DocTheme.TitleSize).Bold().FontColor(_theme.Accent)
                .LetterSpacing(3f / DocTheme.TitleSize).LineHeight(1.1f);

            var fields = _meta.Where(m => !string.IsNullOrWhiteSpace(m.Value)).ToList();
            if (fields.Count == 0) return;

            col.Item().PaddingTop(DocTheme.Px(10)).AlignRight().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(DocTheme.Px(72));   // label
                    // Wider than the handoff's 110px: real values like a vehicle registration
                    // ("DHK-METRO-TA-1877") wrap at that width. The title block is wider than the
                    // meta grid anyway, so this doesn't squeeze the company block.
                    c.ConstantColumn(DocTheme.Px(140));  // value
                });

                foreach (var f in fields)
                {
                    table.Cell().PaddingBottom(DocTheme.Px(3)).AlignRight()
                        .Text(f.Label)
                        .FontSize(DocTheme.MetaSize).FontColor(DocTheme.Label);

                    table.Cell().PaddingBottom(DocTheme.Px(3)).PaddingLeft(DocTheme.Px(14)).AlignRight()
                        .Text(f.Value)
                        .Style(DocTheme.MonoText).FontSize(DocTheme.MetaSize).FontColor(DocTheme.Ink);
                }
            });
        });
    }

    /// <summary>First letter of each of the first two words — "Sujan Motors" gives "SM".</summary>
    private static string Initials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "SM";
        var chars = words.Take(2).Select(w => char.ToUpperInvariant(w[0]));
        return string.Concat(chars);
    }
}
