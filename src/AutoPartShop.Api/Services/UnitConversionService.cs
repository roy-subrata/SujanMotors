using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for handling unit conversions
/// </summary>
public interface IUnitConversionService
{
    /// <summary>
    /// Convert quantity from one unit to another
    /// </summary>
    Task<int> ConvertQuantityAsync(int quantity, Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversion factor between two units
    /// </summary>
    Task<decimal> GetConversionFactorAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default);
}

public class UnitConversionService : IUnitConversionService
{
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<UnitConversionService> _logger;

    public UnitConversionService(AutoPartDbContext dbContext, ILogger<UnitConversionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Convert quantity from one unit to another
    /// Returns the converted quantity as an integer (rounded)
    /// </summary>
    public async Task<int> ConvertQuantityAsync(int quantity, Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default)
    {
        // If same unit, no conversion needed
        if (fromUnitId == toUnitId)
            return quantity;

        var conversionFactor = await GetConversionFactorAsync(fromUnitId, toUnitId, cancellationToken);

        // Convert and round to nearest integer
        var convertedQuantity = quantity * conversionFactor;
        return (int)Math.Round(convertedQuantity, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Get conversion factor between two units
    /// Checks for direct conversion first, then tries reverse conversion
    /// </summary>
    public async Task<decimal> GetConversionFactorAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default)
    {
        // If same unit, factor is 1
        if (fromUnitId == toUnitId)
            return 1.0m;

        // Try direct conversion (fromUnit -> toUnit)
        var directConversion = await _dbContext.Set<UnitConversion>()
            .FirstOrDefaultAsync(uc =>
                uc.FromUnitId == fromUnitId &&
                uc.ToUnitId == toUnitId &&
                uc.IsActive &&
                !uc.Isdeleted,
                cancellationToken);

        if (directConversion != null)
        {
            _logger.LogDebug("Found direct conversion from {FromUnitId} to {ToUnitId}: factor = {Factor}",
                fromUnitId, toUnitId, directConversion.ConversionFactor);
            return directConversion.ConversionFactor;
        }

        // Try reverse conversion (toUnit -> fromUnit) and invert the factor
        var reverseConversion = await _dbContext.Set<UnitConversion>()
            .FirstOrDefaultAsync(uc =>
                uc.FromUnitId == toUnitId &&
                uc.ToUnitId == fromUnitId &&
                uc.IsActive &&
                !uc.Isdeleted,
                cancellationToken);

        if (reverseConversion != null)
        {
            var factor = 1.0m / reverseConversion.ConversionFactor;
            _logger.LogDebug("Found reverse conversion from {ToUnitId} to {FromUnitId}, inverted factor = {Factor}",
                toUnitId, fromUnitId, factor);
            return factor;
        }

        // No conversion found
        _logger.LogWarning("No conversion found between {FromUnitId} and {ToUnitId}", fromUnitId, toUnitId);
        throw new InvalidOperationException($"No conversion factor found between units {fromUnitId} and {toUnitId}. Please configure the unit conversion.");
    }
}
