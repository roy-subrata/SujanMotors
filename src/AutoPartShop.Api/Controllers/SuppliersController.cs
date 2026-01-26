using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerDtos;
using AutoPartShop.Application.DTOs.SupplierDtos;
using AutoPartShop.Domain.Common;
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
    private readonly ICurrentUserService _currentUserService;

    public SuppliersController(ISupplierRepository supplierRepository, ICodeGenerateService codeGenerateService, ICurrentUserService currentUserService, ILogger<SuppliersController> logger)
    {
        _supplierRepository = supplierRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _logger = logger;
    }
    [HttpPost("list")]
    public async Task<IActionResult> FindAll(SupplierQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (query is null)
            {
                return BadRequest("Request can not be empty");
            }
            if (query.PageNumber < 0)
            {
                return BadRequest($"Page number can not be {query.PageNumber}");
            }
            if (query.PageSize < 0)
            {
                return BadRequest($"Page size can not be {query.PageSize}");
            }

            var response = await _supplierRepository.SearchPagedAsync(query, cancellationToken);
            return Ok(new PaginatedResponse<SupplierResponse>
            {
                Data = response.Suppliers.Select(MapToResponse).ToList(),
                Pagination = new PaginationMeta
                {
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = response.TotalCount,
                    TotalPages = (int)Math.Ceiling(response.TotalCount / (double)query.PageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }

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

            var currentUser = _currentUserService.GetCurrentUsername();
            supplier.CreatedBy = currentUser;
            supplier.ModifiedBy = currentUser;

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
              request.IsActive);
            supplier.ModifiedBy = _currentUserService.GetCurrentUsername();

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
            supplier.ModifiedBy = _currentUserService.GetCurrentUsername();
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
            supplier.ModifiedBy = _currentUserService.GetCurrentUsername();
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
            supplier.ModifiedBy = _currentUserService.GetCurrentUsername();
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
            supplier.ModifiedBy = _currentUserService.GetCurrentUsername();
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
            CurrentBalance = supplier.CurrentBalance,
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
