using AutoPartShop.Application.DTOs.SupplierDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<SuppliersController> _logger;

    private readonly ICodeGenerateService _codeGenerateService;

    public SuppliersController(ISupplierRepository supplierRepository, ICodeGenerateService codeGenerateService, ILogger<SuppliersController> logger)
    {
        _supplierRepository = supplierRepository;
        _codeGenerateService = codeGenerateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
            var response = suppliers.Select(s => MapToResponse(s));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all suppliers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving suppliers");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var suppliers = await _supplierRepository.GetAllActiveAsync(cancellationToken);
            var response = suppliers.Select(s => MapToResponse(s));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active suppliers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active suppliers");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (suppliers, totalCount) = await _supplierRepository.SearchPagedAsync(searchTerm ?? string.Empty, pageNumber, pageSize, cancellationToken);
            var response = suppliers.Select(s => MapToResponse(s));
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suppliers list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving suppliers");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier is null) return NotFound(new { message = "Supplier not found" });

            return Ok(MapToResponse(supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier by ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the supplier");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { message = "Name and Code are required" });

            if (await _supplierRepository.CodeExistsAsync(request.Code, null, cancellationToken))
                return Conflict(new { message = "Supplier code already exists" });

            var supplier = Supplier.Create(request.Name, request.Code, request.ContactPerson, request.Email, request.Phone,
                request.Address, request.City, request.State, request.Country, request.PostalCode);
            supplier.PaymentTerms = request.PaymentTerms;
            supplier.CreditLimit = request.CreditLimit;
            supplier.CreatedBy = "System";
            supplier.ModifiedBy = "System";

            await _codeGenerateService.SaveGenerateCodeAsync("SUP", cancellationToken);        
            await _supplierRepository.AddAsync(supplier, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, MapToResponse(supplier));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the supplier");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier is null) return NotFound(new { message = "Supplier not found" });

            supplier.Update(request.Name, request.ContactPerson, request.Email, request.Phone,
                request.Address, request.City, request.State, request.Country, request.PostalCode,
                request.PaymentTerms, request.CreditLimit, request.IsActive);
            supplier.ModifiedBy = "System";

            await _supplierRepository.UpdateAsync(supplier, cancellationToken);

            return Ok(MapToResponse(supplier));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the supplier");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier is null) return NotFound(new { message = "Supplier not found" });

            supplier.Activate();
            supplier.ModifiedBy = "System";
            await _supplierRepository.UpdateAsync(supplier, cancellationToken);

            return Ok(MapToResponse(supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating supplier: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the supplier");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier is null) return NotFound(new { message = "Supplier not found" });

            supplier.Deactivate();
            supplier.ModifiedBy = "System";
            await _supplierRepository.UpdateAsync(supplier, cancellationToken);

            return Ok(MapToResponse(supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating supplier: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the supplier");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _supplierRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Supplier not found" });

            await _supplierRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the supplier");
        }
    }

    [HttpPatch("{id:guid}/bank-details")]
    public async Task<IActionResult> SetBankDetails(Guid id, [FromBody] BankDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier is null) return NotFound(new { message = "Supplier not found" });

            supplier.SetBankDetails(request.BankName, request.AccountNumber, request.TaxID);
            supplier.ModifiedBy = "System";
            await _supplierRepository.UpdateAsync(supplier, cancellationToken);

            return Ok(MapToResponse(supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting bank details");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while setting bank details");
        }
    }

    [HttpPatch("{id:guid}/rating")]
    public async Task<IActionResult> SetRating(Guid id, [FromBody] RatingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier is null) return NotFound(new { message = "Supplier not found" });

            supplier.SetRating(request.Rating);
            supplier.ModifiedBy = "System";
            await _supplierRepository.UpdateAsync(supplier, cancellationToken);

            return Ok(MapToResponse(supplier));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting rating");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while setting rating");
        }
    }

    private static SupplierResponse MapToResponse(Supplier supplier)
    {
        return new SupplierResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Code = supplier.Code,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            City = supplier.City,
            State = supplier.State,
            Country = supplier.Country,
            PostalCode = supplier.PostalCode,
            PaymentTerms = supplier.PaymentTerms,
            CreditLimit = supplier.CreditLimit,
            BankName = supplier.BankName,
            BankAccountNumber = supplier.BankAccountNumber,
            TaxID = supplier.TaxID,
            IsActive = supplier.IsActive,
            Rating = supplier.Rating,
            CreatedBy = supplier.CreatedBy,
            ModifiedBy = supplier.ModifiedBy
        };
    }
}

public class BankDetailsRequest
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IFSC { get; set; } = string.Empty;
    public string TaxID { get; set; } = string.Empty;
}

public class RatingRequest
{
    public int Rating { get; set; }
}
