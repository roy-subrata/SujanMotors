using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IPartRepository : IBaseRepository<Part>
{
    Task<IEnumerable<Part>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Part> Parts, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> SKUExistsAsync(string sku, Guid? excludePartId = null, CancellationToken cancellationToken = default);
    Task<Part?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default);
    Task<IEnumerable<Part>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
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

public interface IWarehouseRepository : IBaseRepository<Warehouse>
{
    Task<IEnumerable<Warehouse>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Warehouse> Warehouses, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeWarehouseId = null, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Warehouse>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
}

public interface IProductLocationRepository : IBaseRepository<ProductLocation>
{
    Task<IEnumerable<ProductLocation>> GetLocationsByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLocation>> GetLocationsByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<ProductLocation?> GetLocationByPartAndWarehouseAsync(Guid partId, Guid warehouseId, string section, string shelf, CancellationToken cancellationToken = default);
    Task<ProductLocation?> GetPrimaryLocationByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task SetPrimaryLocationAsync(Guid partId, Guid locationId, CancellationToken cancellationToken = default);
    Task<bool> LocationExistsAsync(Guid partId, Guid warehouseId, string section, string shelf, Guid? excludeLocationId = null, CancellationToken cancellationToken = default);
}
