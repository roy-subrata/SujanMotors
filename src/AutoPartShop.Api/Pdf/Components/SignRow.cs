using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf.Components;

/// <summary>
/// Shared three-column signature block — port of SignRow.dc.html.
/// Sits 64pt below the last content block on every document.
/// </summary>
public sealed class SignRow
{
    private readonly string _a;
    private readonly string _b;
    private readonly string _c;

    public SignRow(string a, string b, string c)
    {
        _a = a;
        _b = b;
        _c = c;
    }

    public void Compose(IContainer container)
    {
        container.PaddingTop(DocTheme.SignRowTop).Row(row =>
        {
            Column(row, _a);
            row.ConstantItem(DocTheme.Px(48));
            Column(row, _b);
            row.ConstantItem(DocTheme.Px(48));
            Column(row, _c);
        });
    }

    private static void Column(RowDescriptor row, string caption)
    {
        row.RelativeItem()
            .BorderTop(DocTheme.RuleHairline).BorderColor(DocTheme.Ink)
            .PaddingTop(DocTheme.Px(7))
            .AlignCenter()
            .Text(caption.ToUpperInvariant())
            .FontSize(DocTheme.SignCaption).FontColor(DocTheme.Muted)
            .LetterSpacing(1.2f / DocTheme.SignCaption);
    }
}
