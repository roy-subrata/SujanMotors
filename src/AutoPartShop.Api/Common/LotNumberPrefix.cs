namespace AutoPartShop.Api.Common;

/// <summary>
/// Date-scoped LOT prefix (e.g. "LOT-20260901-") passed to ICodeGenerateService.GenerateAsync.
/// The per-prefix counter in CodeSequences naturally resets because the prefix changes every
/// day — CodeGenerateService itself is unchanged.
/// </summary>
internal static class LotNumberPrefix
{
    public static string Today() => $"LOT-{DateTime.UtcNow:yyyyMMdd}-";
}
