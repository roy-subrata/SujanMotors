using System.Reflection;
using System.Text.Json;

namespace AutoPartShop.Api.Pdf.Design;

/// <summary>
/// Label localization store for the document set and the Reports module. Mirrors the frontend
/// I18nService's dotted-path lookup (src/AutoPartShop.WebApp/.../i18n.service.ts): keys are
/// nested by document/section in the source JSON, flattened to dotted strings once at load, and
/// resolved with fallback to English, then to the raw key.
/// </summary>
public static class DocStrings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Languages = new();
    private static bool _loaded;
    private static readonly Lock Gate = new();

    public static void Load()
    {
        lock (Gate)
        {
            if (_loaded) return;

            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames()
                .Where(n => n.Contains(".Pdf.Localization.", StringComparison.OrdinalIgnoreCase)
                            && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (resources.Count == 0)
                throw new InvalidOperationException(
                    "No embedded .json resources found under Pdf/Localization. Expected Pdf/Localization/*.json to be embedded in AutoPartShop.Api.");

            foreach (var name in resources)
            {
                var lang = name.Split('.')[^2];

                using var stream = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Embedded resource '{name}' could not be opened.");
                using var document = JsonDocument.Parse(stream);

                var flat = new Dictionary<string, string>();
                Flatten(document.RootElement, "", flat);
                Languages[lang] = flat;
            }

            _loaded = true;
        }
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> into)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
                Flatten(property.Value, key, into);
            else
                into[key] = property.Value.GetString() ?? "";
        }
    }

    /// <summary>
    /// Resolves a dotted key for the given language, falling back to English, then to the raw key —
    /// matching I18nService.translate's fallback chain.
    /// </summary>
    public static string T(string key, string lang)
    {
        if (Languages.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
            return value;

        if (lang != "en" && Languages.TryGetValue("en", out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
            return fallbackValue;

        return key;
    }
}
