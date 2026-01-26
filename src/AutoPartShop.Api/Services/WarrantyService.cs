using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

public interface IWarrantyService
{
    Task<string> GenerateWarrantyNumberAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateClaimNumberAsync(CancellationToken cancellationToken = default);
    Task<WarrantyRegistration> CreateWarrantyForSalesOrderLineAsync(
        SalesOrderLine salesOrderLine,
        Guid salesOrderId,
        Guid customerId,
        DateTime saleDate,
        CancellationToken cancellationToken = default);
}

public class WarrantyService(
    ICodeGenerateService codeGenerateService,
    IPartRepository partRepository,
    IWarrantyRegistrationRepository warrantyRepository) : IWarrantyService
{
    public async Task<string> GenerateWarrantyNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"WR-{year}-";
        return await codeGenerateService.GenerateAsync(prefix, cancellationToken, minDigits: 5);
    }

    public async Task<string> GenerateClaimNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"WC-{year}-";
        return await codeGenerateService.GenerateAsync(prefix, cancellationToken, minDigits: 5);
    }

    public async Task<WarrantyRegistration> CreateWarrantyForSalesOrderLineAsync(
        SalesOrderLine salesOrderLine,
        Guid salesOrderId,
        Guid customerId,
        DateTime saleDate,
        CancellationToken cancellationToken = default)
    {
        // Get part details to check if it has warranty
        var part = await partRepository.GetByIdAsync(salesOrderLine.PartId, cancellationToken);
        if (part == null)
            throw new InvalidOperationException($"Part not found: {salesOrderLine.PartId}");

        // Check if part has warranty
        if (!part.HasWarranty || !part.WarrantyPeriodMonths.HasValue)
            throw new InvalidOperationException($"Part {part.Name} does not have warranty");

        // Generate warranty number
        var warrantyNumber = await GenerateWarrantyNumberAsync(cancellationToken);

        // Generate certificate number using the template or default format
        var certificateNumber = string.IsNullOrWhiteSpace(part.WarrantyCertificateTemplate)
            ? $"CERT-{warrantyNumber}"
            : $"{part.WarrantyCertificateTemplate}-{warrantyNumber}";

        // Create warranty registration
        var warranty = WarrantyRegistration.Create(
            warrantyNumber: warrantyNumber,
            partId: part.Id,
            salesOrderId: salesOrderId,
            salesOrderLineId: salesOrderLine.Id,
            customerId: customerId,
            saleDate: saleDate,
            warrantyStartDate: saleDate,
            warrantyPeriodMonths: part.WarrantyPeriodMonths.Value,
            warrantyType: part.WarrantyType ?? "SELLER",
            warrantyTerms: part.WarrantyTerms ?? "Standard warranty terms apply",
            certificateNumber: certificateNumber
        );

        // Save warranty registration
        await warrantyRepository.AddAsync(warranty, cancellationToken);

        return warranty;
    }
}
