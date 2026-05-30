using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.TechnicianDtos;
using AutoPartShop.Application.Technecians;
using AutoPartShop.Application.Technecians.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TechnicianController : ControllerBase
{
    private readonly ITechnicianRepository _technicianRepository;
    private readonly ITechnecianReadRepository _technecianReadRepository;
    private readonly ILogger<TechnicianController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public TechnicianController(
        ITechnicianRepository technicianRepository,
        ITechnecianReadRepository technecianReadRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<TechnicianController> logger)
    {
        _technicianRepository = technicianRepository;
        _technecianReadRepository = technecianReadRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var technicians = await _technicianRepository.GetAllAsync(cancellationToken);
            var response = technicians.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all technicians");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(TechnecianQuery query, CancellationToken cancellationToken = default)
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

            var (technicians, totalCount) = await _technecianReadRepository.FindAllQuery(query, cancellationToken);

            return Ok(PagedResult<TechnicianResponse>.Create(technicians, totalCount, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting technicians list");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianRepository.GetByIdAsync(id, cancellationToken);
            if (technician is null) return NotFound();

            return Ok(MapToResponse(technician));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting technician");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("code/{technicianCode}")]
    public async Task<IActionResult> GetByCode(string technicianCode, CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianRepository.GetByCodeAsync(technicianCode, cancellationToken);
            if (technician is null) return NotFound();

            return Ok(MapToResponse(technician));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting technician by code");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var technicians = await _technicianRepository.GetByStatusAsync(status, cancellationToken);
            var response = technicians.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting technicians by status");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTechnicianRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { message = "Name and Phone are required" });

            var technicianCode = await _codeGenerateService.GenerateAsync("TECH", cancellationToken);

            var technician = Technician.Create(
                technicianCode,
                request.Name,
                request.Phone,
                request.Email,
                request.ShopName,
                request.Address,
                request.City,
                request.Notes
            );

            var currentUser = _currentUserService.GetCurrentUsername();
            technician.CreatedBy = currentUser;
            technician.ModifiedBy = currentUser;

            await _technicianRepository.AddAsync(technician, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = technician.Id }, MapToResponse(technician));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating technician");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTechnicianRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianRepository.GetByIdAsync(id, cancellationToken);
            if (technician is null) return NotFound();

            technician.UpdateInfo(
                request.Name,
                request.Phone,
                request.Email,
                request.ShopName,
                request.Address,
                request.City,
                request.Notes
            );

            technician.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _technicianRepository.UpdateAsync(technician, cancellationToken);

            return Ok(MapToResponse(technician));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating technician");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianRepository.GetByIdAsync(id, cancellationToken);
            if (technician is null) return NotFound();

            technician.Activate();
            technician.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _technicianRepository.UpdateAsync(technician, cancellationToken);

            return Ok(MapToResponse(technician));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating technician");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianRepository.GetByIdAsync(id, cancellationToken);
            if (technician is null) return NotFound();

            technician.Deactivate();
            technician.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _technicianRepository.UpdateAsync(technician, cancellationToken);

            return Ok(MapToResponse(technician));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating technician");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianRepository.GetByIdAsync(id, cancellationToken);
            if (technician is null) return NotFound();

            await _technicianRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting technician");
            return StatusCode(500, "An error occurred");
        }
    }

    private TechnicianResponse MapToResponse(Technician t) => new()
    {
        Id = t.Id,
        TechnicianCode = t.TechnicianCode,
        Name = t.Name,
        Phone = t.Phone,
        Email = t.Email,
        ShopName = t.ShopName,
        Address = t.Address,
        City = t.City,
        Status = t.Status,
        Notes = t.Notes,
        CreatedAt = t.CreatedDate
    };
}
