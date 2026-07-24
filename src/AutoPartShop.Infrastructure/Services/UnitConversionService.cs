using System;
using System.Linq;
using System.Threading.Tasks;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Infrastructure.Services;

/// <summary>
/// Implementation of unit conversion service using UnitConversion entity table
/// </summary>
public class UnitConversionService : IUnitConversionService
{
    private readonly IUnitConversionRepository _unitConversionRepository;
    private readonly IUnitRepository _unitRepository;

    public UnitConversionService(
        IUnitConversionRepository unitConversionRepository,
        IUnitRepository unitRepository)
    {
        _unitConversionRepository = unitConversionRepository;
        _unitRepository = unitRepository;
    }

    public async Task<decimal> ConvertQuantityAsync(decimal quantity, Guid fromUnitId, Guid toUnitId)
    {
        // If same unit, no conversion needed
        if (fromUnitId == toUnitId)
            return quantity;

        var conversionFactor = await GetConversionFactorAsync(fromUnitId, toUnitId);
        return quantity * conversionFactor;
    }

    public async Task<int> ConvertToBaseUnitAsync(decimal quantity, Guid fromUnitId, Guid baseUnitId)
    {
        // If already in base unit or fromUnit is null, return quantity as-is
        if (fromUnitId == baseUnitId || fromUnitId == Guid.Empty)
            return (int)quantity;

        var conversionFactor = await GetConversionFactorAsync(fromUnitId, baseUnitId);
        return (int)(quantity * conversionFactor);
    }

    public async Task<decimal> ConvertFromBaseUnitAsync(decimal quantity, Guid fromBaseUnitId, Guid toUnitId)
    {
        // If converting to same unit or toUnit is null, return quantity as-is
        if (fromBaseUnitId == toUnitId || toUnitId == Guid.Empty)
            return quantity;

        // Get reverse conversion factor (from base to target)
        var conversionFactor = await GetConversionFactorAsync(fromBaseUnitId, toUnitId);
        return quantity / conversionFactor;
    }

    public async Task<decimal> GetConversionFactorAsync(Guid fromUnitId, Guid toUnitId)
    {
        if (fromUnitId == toUnitId)
            return 1m;

        // Try to find direct conversion from UnitConversion table
        var conversions = await _unitConversionRepository.GetAllAsync();
        var conversion = conversions.FirstOrDefault(uc =>
            uc.FromUnitId == fromUnitId &&
            uc.ToUnitId == toUnitId &&
            uc.IsActive);

        if (conversion != null)
        {
            if (conversion.ConversionFactor <= 0)
                throw new InvalidOperationException(
                    $"Conversion factor for unit conversion '{conversion.Id}' must be greater than zero.");
            return conversion.ConversionFactor;
        }

        // Try reverse conversion
        var reverseConversion = conversions.FirstOrDefault(uc =>
            uc.FromUnitId == toUnitId &&
            uc.ToUnitId == fromUnitId &&
            uc.IsActive);

        if (reverseConversion != null)
        {
            if (reverseConversion.ConversionFactor <= 0)
                throw new InvalidOperationException(
                    $"Conversion factor for unit conversion '{reverseConversion.Id}' must be greater than zero.");
            return 1m / reverseConversion.ConversionFactor;
        }

        // If no conversion found, throw exception
        var fromUnit = await _unitRepository.GetByIdAsync(fromUnitId);
        var toUnit = await _unitRepository.GetByIdAsync(toUnitId);

        throw new InvalidOperationException(
            $"No active conversion found between units: '{fromUnit?.Name}' and '{toUnit?.Name}'");
    }

    public async Task<bool> ConversionExistsAsync(Guid fromUnitId, Guid toUnitId)
    {
        if (fromUnitId == toUnitId)
            return true;

        var conversions = await _unitConversionRepository.GetAllAsync();
        return conversions.Any(uc =>
            ((uc.FromUnitId == fromUnitId && uc.ToUnitId == toUnitId) ||
            (uc.FromUnitId == toUnitId && uc.ToUnitId == fromUnitId)) &&
            uc.IsActive);
    }

    public async Task<(bool isValid, string? errorMessage)> ValidateConversionAsync(Guid fromUnitId, Guid toUnitId)
    {
        // Same unit is always valid
        if (fromUnitId == toUnitId)
        {
            return (true, null);
        }

        // Check if units exist
        var fromUnit = await _unitRepository.GetByIdAsync(fromUnitId);
        var toUnit = await _unitRepository.GetByIdAsync(toUnitId);

        if (fromUnit == null)
        {
            return (false, $"Source unit not found: {fromUnitId}");
        }

        if (toUnit == null)
        {
            return (false, $"Target unit not found: {toUnitId}");
        }

        // Check if conversion exists
        var conversionExists = await ConversionExistsAsync(fromUnitId, toUnitId);
        if (!conversionExists)
        {
            return (false, $"No conversion found between '{fromUnit.Name}' and '{toUnit.Name}'");
        }

        return (true, null);
    }
}
