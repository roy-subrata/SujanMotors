using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Employee master records (HR). Salary data is sensitive, so the whole
/// controller is restricted to Admin/Manager.
/// </summary>
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeReadRepository _employeeReadRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeRepository employeeRepository,
        IEmployeeReadRepository employeeReadRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IMemoryCache memoryCache,
        ILogger<EmployeesController> logger)
    {
        _employeeRepository = employeeRepository;
        _employeeReadRepository = employeeReadRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var employees = await _employeeRepository.GetAllAsync(cancellationToken);
            var response = employees.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all employees");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(EmployeeQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (query is null)
            {
                return BadRequest("Request can not be empty");
            }

            var (employees, totalCount) = await _employeeReadRepository.FindAllQuery(query, cancellationToken);

            return Ok(PagedResult<EmployeeResponse>.Create(employees, totalCount, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees list");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) return NotFound();

            return Ok(MapToResponse(employee));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Staff login accounts that can be linked to an employee record.
    /// Pass employeeId when editing so the employee's own linked account stays selectable.
    /// </summary>
    [HttpGet("linkable-users")]
    public async Task<IActionResult> GetLinkableUsers([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _employeeReadRepository.GetLinkableUsers(employeeId, cancellationToken);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting linkable users");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { message = "Name and Phone are required" });

            if (request.JoinDate == default)
                return BadRequest(new { message = "Join date is required" });

            var employeeCode = await _codeGenerateService.GenerateAsync("EMP", cancellationToken);

            var employee = Employee.Create(
                employeeCode,
                request.Name,
                request.Phone,
                request.JoinDate,
                request.Designation,
                request.Department,
                request.MonthlySalary,
                request.EmploymentType,
                request.Email,
                request.NidNumber,
                request.DateOfBirth,
                request.Gender,
                request.Address,
                request.City,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.Notes
            );

            employee.UpdateCompensation(request.ShiftId, request.MonthlyTaxDeduction, request.CommissionRate);

            if (request.UserId is Guid userId)
            {
                var alreadyLinked = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);
                if (alreadyLinked is not null)
                    return BadRequest(new { message = "This user account is already linked to another employee" });

                employee.LinkUserAccount(userId);
            }

            var currentUser = _currentUserService.GetCurrentUsername();
            employee.CreatedBy = currentUser;
            employee.ModifiedBy = currentUser;

            await _employeeRepository.AddAsync(employee, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, MapToResponse(employee));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) return NotFound();

            employee.UpdateInfo(
                request.Name,
                request.Phone,
                request.Email,
                request.NidNumber,
                request.DateOfBirth,
                request.Gender,
                request.Address,
                request.City,
                request.Designation,
                request.Department,
                request.JoinDate,
                request.EmploymentType,
                request.MonthlySalary,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.Notes
            );

            employee.UpdateCompensation(request.ShiftId, request.MonthlyTaxDeduction, request.CommissionRate);

            if (request.UserId is Guid userId)
            {
                var alreadyLinked = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);
                if (alreadyLinked is not null && alreadyLinked.Id != employee.Id)
                    return BadRequest(new { message = "This user account is already linked to another employee" });

                employee.LinkUserAccount(userId);
            }
            else
            {
                employee.UnlinkUserAccount();
            }

            employee.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _employeeRepository.UpdateAsync(employee, cancellationToken);

            return Ok(MapToResponse(employee));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, ActivateEmployeeRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            employee.Activate();
            employee.ModifiedBy = currentUser;

            await _employeeRepository.UpdateAsync(employee, cancellationToken);

            var loginToggled = request?.EnableLogin == true
                && await SetLinkedLoginActiveAsync(employee, active: true, currentUser);

            return Ok(new { employee = MapToResponse(employee), loginEnabled = loginToggled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employee");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, DeactivateEmployeeRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) return NotFound();

            var currentUser = _currentUserService.GetCurrentUsername();
            employee.Deactivate();
            employee.ModifiedBy = currentUser;

            await _employeeRepository.UpdateAsync(employee, cancellationToken);

            var loginToggled = request?.DisableLogin == true
                && await SetLinkedLoginActiveAsync(employee, active: false, currentUser);

            return Ok(new { employee = MapToResponse(employee), loginDisabled = loginToggled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>Flips the linked login account's IsActive flag; returns false when there is no link.</summary>
    private async Task<bool> SetLinkedLoginActiveAsync(Employee employee, bool active, string modifiedBy)
    {
        if (employee.UserId is not Guid userId) return false;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        user.IsActive = active;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = modifiedBy;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            _logger.LogWarning("Failed to set IsActive={Active} on login {UserId} for employee {EmployeeCode}", active, userId, employee.EmployeeCode);
        else
            // Evict the auth-time IsActive cache so revocation takes effect on the very next request
            _memoryCache.Remove($"user-active:{userId}");

        return result.Succeeded;
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) return NotFound();

            await _employeeRepository.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee");
            return StatusCode(500, "An error occurred");
        }
    }

    private EmployeeResponse MapToResponse(Employee e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        Name = e.Name,
        Phone = e.Phone,
        Email = e.Email,
        NidNumber = e.NidNumber,
        DateOfBirth = e.DateOfBirth,
        Gender = e.Gender,
        Address = e.Address,
        City = e.City,
        Designation = e.Designation,
        Department = e.Department,
        JoinDate = e.JoinDate,
        EndDate = e.EndDate,
        EmploymentType = e.EmploymentType,
        MonthlySalary = e.MonthlySalary,
        Currency = e.Currency,
        ShiftId = e.ShiftId,
        MonthlyTaxDeduction = e.MonthlyTaxDeduction,
        CommissionRate = e.CommissionRate,
        EmergencyContactName = e.EmergencyContactName,
        EmergencyContactPhone = e.EmergencyContactPhone,
        Status = e.Status,
        Notes = e.Notes,
        UserId = e.UserId,
        CreatedAt = e.CreatedDate
    };
}
