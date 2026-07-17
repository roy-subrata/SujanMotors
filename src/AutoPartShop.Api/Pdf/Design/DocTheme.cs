using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf.Design;

/// <summary>
/// Design tokens for the POS document set, transcribed from design_handoff_pos_documents.
///
/// The handoff is expressed in CSS pixels at 96dpi; PDF works in points at 72dpi. Every px value
/// is therefore scaled by 0.75 via <see cref="Px"/> — an A4 sheet is 794px wide in the browser but
/// 595pt in the PDF, so treating px as pt would render everything ~33% oversized relative to the
/// page and wrap the headers.
/// </summary>
public sealed record DocTheme
{
    /// <summary>CSS pixels (96dpi) to PDF points (72dpi).</summary>
    public static float Px(float px) => px * 0.75f;

    // ── Colors (handoff "Global Design System") ────────────────────────────────
    /// <summary>Brand accent. Handoff default #B0392E; also offered as teal/blue/slate.</summary>
    public string Accent { get; init; } = "#B0392E";

    public const string Ink = "#1c1917";        // primary text + heavy rules
    public const string Secondary = "#44403c";  // secondary text, table header text
    public const string Muted = "#57534e";      // address block, sign-row captions
    public const string Label = "#78716c";      // meta labels, "in words" caption
    public const string Hairline = "#e7e5e4";   // table row dividers
    public const string Divider = "#d6d3d1";    // medium divider
    public const string White = "#ffffff";

    // ── Type scale (handoff px → pt) ───────────────────────────────────────────
    public static readonly float TitleSize = Px(23);      // document title, weight 700, +3 letter-spacing
    public static readonly float CompanySize = Px(19);    // "SUJAN MOTORS", weight 700, +2 letter-spacing
    public static readonly float TaglineSize = Px(10);    // uppercase, accent
    public static readonly float AddressSize = Px(9.5f);  // address block
    public static readonly float MetaSize = Px(10.5f);    // header meta grid
    public static readonly float SectionLabel = Px(9);    // "BILL TO" etc, weight 600, +1.2 letter-spacing
    public static readonly float TableHeader = Px(9);     // uppercase, +1.2 letter-spacing
    public static readonly float TableCell = Px(11);
    public static readonly float TableCode = Px(10.5f);   // Part No column (mono)
    public static readonly float Body = Px(10.5f);
    public static readonly float GrandTotal = Px(13);
    public static readonly float SignCaption = Px(9.5f);

    // ── Borders (handoff "Design Tokens") ──────────────────────────────────────
    public static readonly float RuleHairline = Px(1);    // row dividers, sign-row rule
    public static readonly float RuleMedium = Px(2);      // table header rule, grand-total rule
    public static readonly float RuleHeavy = Px(3);       // document header rule

    // ── Page setup ─────────────────────────────────────────────────────────────
    /// <summary>0.55in at 72dpi.</summary>
    public const float PageMargin = 39.6f;

    /// <summary>Right-aligned totals stack width from ItemsTable.dc.html (300px).</summary>
    public static readonly float TotalsWidth = Px(300);

    /// <summary>Gap above the signature row (64px).</summary>
    public static readonly float SignRowTop = Px(64);

    public string CurrencySymbol { get; init; } = "৳";

    public static DocTheme Default => new();

    /// <summary>
    /// Applies the base text style. Noto Sans Bengali trails as a fallback family so the taka sign
    /// (U+09F3) and Bengali product names resolve — no IBM Plex face covers the Bengali block.
    /// </summary>
    public static TextStyle BaseText => TextStyle.Default
        .FontFamily(DocFonts.Sans, DocFonts.Bengali)
        .FontSize(Body)
        .FontColor(Ink);

    /// <summary>Monospace style for all figures, dates, document numbers, part codes, amounts.</summary>
    public static TextStyle MonoText => TextStyle.Default
        .FontFamily(DocFonts.Mono, DocFonts.Bengali);

    /// <summary>Standard A4 page setup shared by every full-page document.</summary>
    public void ApplyPage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(PageMargin);
        page.DefaultTextStyle(BaseText);
    }

    public string Money(decimal amount) => $"{CurrencySymbol} {amount:N2}";
}
