using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.SupplierDtos;
using AutoPartShop.Application.Suppliers;
using AutoPartShop.Application.Suppliers.Dtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace AutoPartShop.Api.Controllers;

[Route("api/suppliers")]
[Route("api/v1/suppliers")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierReadRepository _supplierReadRepository;
    private readonly ISupplierPerformanceReadRepository _supplierPerformanceReadRepository;
    private readonly ISupplierPaymentRepository _supplierPaymentRepository;
    private readonly ILogger<SuppliersController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICodeGenerateService _codeGenerateService;

    public SuppliersController(ISupplierRepository supplierRepository,
        ISupplierReadRepository supplierReadRepository,
        ISupplierPerformanceReadRepository supplierPerformanceReadRepository,
        ISupplierPaymentRepository supplierPaymentRepository,
        ICurrentUserService currentUserService,
        ICodeGenerateService codeGenerateService,
        ILogger<SuppliersController> logger)
    {
        _supplierRepository = supplierRepository;
        _supplierPaymentRepository = supplierPaymentRepository;
        _currentUserService = currentUserService;
        _supplierReadRepository = supplierReadRepository;
        _supplierPerformanceReadRepository = supplierPerformanceReadRepository;
        _codeGenerateService = codeGenerateService;
        _logger = logger;
    }

    /// <summary>
    /// Supplier quality/performance metrics (damaged rate from accepted goods receipts + return counts).
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance([FromQuery] string? search, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _supplierPerformanceReadRepository.GetPerformanceAsync(search, cancellationToken);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier performance report");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving supplier performance");
        }
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

            var (response,total) = await _supplierReadRepository.FindAllAsynce(query, cancellationToken);

            return Ok(PagedResult<SupplierResponse>.Create(
                response.ToList(),
                total,
                query
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all suppliers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving suppliers");
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
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name is required" });

            var supplierCode = await _codeGenerateService.GenerateAsync("SUP", cancellationToken);

            var supplier = Supplier.Create(request.Name, supplierCode, request.ContactPerson, request.Email, request.Phone,
                request.Address, request.City, request.State, request.Country, request.PostalCode,
                request.PaymentTerms, request.CreditLimit);

            var currentUser = _currentUserService.GetCurrentUsername();
            supplier.CreatedBy = currentUser;
            supplier.ModifiedBy = currentUser;

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
                request.IsActive, request.PaymentTerms, request.CreditLimit);
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

            var totalPayments = await _supplierPaymentRepository.GetTotalBySupplierAsync(id, cancellationToken);
            if (totalPayments > 0)
                return Conflict(new { message = "Cannot delete supplier with existing payment history. Deactivate the supplier instead." });

            await _supplierRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the supplier");
        }
    }

    [HttpPatch("{id:guid}/rating")]
    public async Task<IActionResult> SetRating(Guid id, [FromBody] SupplierRatingRequest request, CancellationToken cancellationToken)
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
            PaymentTerms = supplier.PaymentTerms,
            CreditLimit = supplier.CreditLimit,
            CurrentBalance = supplier.CurrentBalance,
            IsActive = supplier.IsActive,
            Rating = supplier.Rating,
            CreatedBy = supplier.CreatedBy,
            ModifiedBy = supplier.ModifiedBy
        };
    }
}

