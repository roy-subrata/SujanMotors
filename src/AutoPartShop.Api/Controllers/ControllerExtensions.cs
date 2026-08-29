using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

public static class ControllerExtensions
{
    /// <summary>
    /// Resolves the document/report language from the request's Accept-Language header —
    /// "bn" (case-insensitive prefix match) or "en" otherwise, matching the two languages
    /// DocStrings ships translations for.
    /// </summary>
    public static string GetLanguage(this ControllerBase controller)
    {
        var header = controller.Request.Headers.AcceptLanguage.ToString();
        return header.StartsWith("bn", StringComparison.OrdinalIgnoreCase) ? "bn" : "en";
    }
}
