using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Product> Parts, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> SKUExistsAsync(string sku, Guid? excludePartId = null, CancellationToken cancellationToken = default);
    Task<Product?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveVariantsAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeOrPartNumberAsync(string code, CancellationToken cancellationToken = default);
    Task<(Product Product, ProductVariant Variant)?> GetByVariantCodeAsync(string code, CancellationToken cancellationToken = default);
}




public interface IVehicleRepository : IBaseRepository<Vehicle>
{
    Task<IEnumerable<Vehicle>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetByMakeAsync(string make, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetByEngineTypeAsync(string engineType, CancellationToken cancellationToken = default);
}

public interface IPartVehicleCompatibilityRepository : IBaseRepository<PartVehicleCompatibility>
{
    Task<IEnumerable<PartVehicleCompatibility>> GetCompatibilitiesByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PartVehicleCompatibility>> GetCompatibilitiesByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<PartVehicleCompatibility?> GetCompatibilityAsync(Guid partId, Guid vehicleId, CancellationToken cancellationToken = default);
    Task<bool> IsCompatibleAsync(Guid partId, Guid vehicleId, CancellationToken cancellationToken = default);
}



public interface IProductLocationRepository : IBaseRepository<ProductLocation>
{
    /// <summary>Locations for a part; optionally narrowed to one warehouse (via the joined WarehouseLocation.WarehouseId).</summary>
    Task<IEnumerable<ProductLocation>> GetLocationsByPartAsync(Guid partId, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLocation>> GetLocationsByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<ProductLocation?> GetLocationByPartAndWarehouseLocationAsync(Guid partId, Guid warehouseLocationId, CancellationToken cancellationToken = default);
    Task<ProductLocation?> GetPrimaryLocationByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task SetPrimaryLocationAsync(Guid partId, Guid locationId, CancellationToken cancellationToken = default);
    Task<bool> LocationExistsAsync(Guid partId, Guid warehouseLocationId, Guid? excludeLocationId = null, CancellationToken cancellationToken = default);
}
