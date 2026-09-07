namespace AutoPartShop.Api.Services;

/// <summary>
/// Thrown by ICostFloorEnforcementService when a line breaches the cost floor or MRP ceiling and no
/// valid price-override approval was supplied. Distinct from a plain ArgumentException so
/// controllers can return a structured error the frontend uses to prompt for manager approval
/// instead of just showing a generic validation message.
/// </summary>
public class PriceOverrideRequiredException : Exception
{
    /// <summary>"FLOOR" (below cost) or "CEILING" (above MRP).</summary>
    public string LimitType { get; }
    public string PartName { get; }
    public decimal AttemptedPrice { get; }
    public decimal Limit { get; }

    public PriceOverrideRequiredException(string limitType, string partName, decimal attemptedPrice, decimal limit, string message)
        : base(message)
    {
        LimitType = limitType;
        PartName = partName;
        AttemptedPrice = attemptedPrice;
        Limit = limit;
    }
}
