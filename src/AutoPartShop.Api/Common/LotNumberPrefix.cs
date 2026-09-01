namespace AutoPartShop.Api.Common;

/// <summary>
/// Date-scoped prefix (e.g. "LOT260901-", "ADJ260901-") passed to ICodeGenerateService.GenerateAsync
/// for stock lot numbers. Two-digit year keeps the resulting code short (e.g. "LOT260901-001",
/// 13 chars) while still reading as a day-level date at a glance. The per-prefix counter in
/// CodeSequences naturally resets each day because the prefix itself changes daily —
/// CodeGenerateService itself is unchanged.
/// </summary>
internal static class LotNumberPrefix
{
    public static string Today(string prefix = "LOT") => $"{prefix}{DateTime.UtcNow:yyMMdd}-";
}
