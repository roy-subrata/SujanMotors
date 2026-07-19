using System.Text;

namespace AutoPartShop.Api.Pdf.Design;

/// <summary>
/// Spells an amount using the Bangladeshi/South Asian numbering system (Crore / Lakh / Thousand),
/// as required by the "In words" line on the POS documents.
/// </summary>
public static class AmountInWords
{
    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    /// <summary>
    /// e.g. 11500.00 → "Eleven Thousand Five Hundred Taka Only";
    ///      1234567.89 → "Twelve Lakh Thirty Four Thousand Five Hundred Sixty Seven Taka And Eighty Nine Poisha Only".
    /// </summary>
    /// <param name="amount">The amount to spell. Negative values are prefixed with "Minus".</param>
    /// <param name="currencyName">Major unit name. Defaults to Taka.</param>
    /// <param name="fractionName">Minor unit name. Defaults to Poisha.</param>
    public static string Convert(decimal amount, string currencyName = "Taka", string fractionName = "Poisha")
    {
        if (amount < 0) return "Minus " + Convert(-amount, currencyName, fractionName);

        var whole = decimal.Truncate(amount);
        // Round the minor unit rather than truncate, so 0.999 reads as 1.00 not 0.99.
        var fraction = (int)Math.Round((amount - whole) * 100, MidpointRounding.AwayFromZero);

        if (fraction == 100)
        {
            whole += 1;
            fraction = 0;
        }

        var sb = new StringBuilder();
        sb.Append(whole == 0 ? "Zero" : SpellIndian(whole));
        sb.Append(' ').Append(currencyName);

        if (fraction > 0)
            sb.Append(" And ").Append(SpellBelowThousand(fraction)).Append(' ').Append(fractionName);

        sb.Append(" Only");
        return sb.ToString();
    }

    /// <summary>
    /// Indian/Bangladeshi grouping: the lowest three digits are hundreds, then every group of two
    /// is Thousand, Lakh, Crore. Above 99 crore the count is expressed in crores.
    /// </summary>
    private static string SpellIndian(decimal value)
    {
        var parts = new List<string>();

        var crore = (long)(value / 10_000_000);
        value %= 10_000_000;

        var lakh = (int)(value / 100_000);
        value %= 100_000;

        var thousand = (int)(value / 1_000);
        value %= 1_000;

        var below = (int)value;

        if (crore > 0)
        {
            // Crores can exceed 99, so recurse to spell the count itself.
            parts.Add(SpellIndian(crore) + " Crore");
        }

        if (lakh > 0) parts.Add(SpellBelowThousand(lakh) + " Lakh");
        if (thousand > 0) parts.Add(SpellBelowThousand(thousand) + " Thousand");
        if (below > 0) parts.Add(SpellBelowThousand(below));

        return string.Join(" ", parts);
    }

    private static string SpellBelowThousand(int n)
    {
        var parts = new List<string>();

        if (n >= 100)
        {
            parts.Add(Ones[n / 100] + " Hundred");
            n %= 100;
        }

        if (n >= 20)
        {
            var t = Tens[n / 10];
            var o = n % 10;
            parts.Add(o > 0 ? $"{t} {Ones[o]}" : t);
        }
        else if (n > 0)
        {
            parts.Add(Ones[n]);
        }

        return string.Join(" ", parts);
    }
}
