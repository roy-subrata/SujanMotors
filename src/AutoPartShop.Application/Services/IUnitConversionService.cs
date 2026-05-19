using System;
using System.Threading.Tasks;

namespace AutoPartShop.Application.Services;

/// <summary>
/// Service for handling unit conversions between different measurement units
/// </summary>
public interface IUnitConversionService
{
    /// <summary>
    /// Converts a quantity from one unit to another
    /// </summary>
    /// <param name="quantity">The quantity to convert</param>
    /// <param name="fromUnitId">Source unit ID</param>
    /// <param name="toUnitId">Target unit ID</param>
    /// <returns>Converted quantity</returns>
    Task<decimal> ConvertQuantityAsync(decimal quantity, Guid fromUnitId, Guid toUnitId);

    /// <summary>
    /// Converts a quantity to base unit using the Part's base unit
    /// </summary>
    /// <param name="quantity">The quantity to convert</param>
    /// <param name="fromUnitId">Source unit ID</param>
    /// <param name="baseUnitId">Part's base unit ID</param>
    /// <returns>Quantity in base unit</returns>
    Task<int> ConvertToBaseUnitAsync(decimal quantity, Guid fromUnitId, Guid baseUnitId);

    /// <summary>
    /// Converts a quantity from base unit to another unit
    /// </summary>
    /// <param name="quantity">The quantity in base unit</param>
    /// <param name="fromBaseUnitId">Part's base unit ID</param>
    /// <param name="toUnitId">Target unit ID</param>
    /// <returns>Quantity in target unit</returns>
    Task<decimal> ConvertFromBaseUnitAsync(decimal quantity, Guid fromBaseUnitId, Guid toUnitId);

    /// <summary>
    /// Gets the conversion factor between two units
    /// </summary>
    /// <param name="fromUnitId">Source unit ID</param>
    /// <param name="toUnitId">Target unit ID</param>
    /// <returns>Conversion factor (multiply source by this to get target)</returns>
    Task<decimal> GetConversionFactorAsync(Guid fromUnitId, Guid toUnitId);

    /// <summary>
    /// Checks if a conversion exists between two units
    /// </summary>
    /// <param name="fromUnitId">Source unit ID</param>
    /// <param name="toUnitId">Target unit ID</param>
    /// <returns>True if conversion exists</returns>
    Task<bool> ConversionExistsAsync(Guid fromUnitId, Guid toUnitId);

    /// <summary>
    /// Validates that a unit conversion is possible and returns an error message if not
    /// </summary>
    /// <param name="fromUnitId">Source unit ID</param>
    /// <param name="toUnitId">Target unit ID</param>
    /// <returns>Tuple of (isValid, errorMessage)</returns>
    Task<(bool isValid, string? errorMessage)> ValidateConversionAsync(Guid fromUnitId, Guid toUnitId);
}
