using System.Reflection;
using QuestPDF.Drawing;

namespace AutoPartShop.Api.Pdf.Design;

/// <summary>
/// Registers the embedded document fonts with QuestPDF. Call once at startup, before any
/// document is rendered.
/// </summary>
public static class DocFonts
{
    public const string Sans = "IBM Plex Sans";
    public const string Mono = "IBM Plex Mono";

    /// <summary>
    /// Fallback family. No IBM Plex face carries the Bengali block, so the taka sign (U+09F3)
    /// and any Bengali text (e.g. Product.LocalName) render as tofu without this.
    /// </summary>
    public const string Bengali = "Noto Sans Bengali";

    private static bool _registered;
    private static readonly Lock Gate = new();

    public static void Register()
    {
        lock (Gate)
        {
            if (_registered) return;

            var assembly = Assembly.GetExecutingAssembly();
            var fonts = assembly.GetManifestResourceNames()
                .Where(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (fonts.Count == 0)
                throw new InvalidOperationException(
                    "No embedded .ttf resources found. Expected Fonts/*.ttf to be embedded in AutoPartShop.Api.");

            foreach (var name in fonts)
            {
                using var stream = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Embedded font '{name}' could not be opened.");
                FontManager.RegisterFont(stream);
            }

            _registered = true;
        }
    }
}
