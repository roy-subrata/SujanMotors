using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.Customers;
using AutoPartShop.Application.Customers.Dtos;
using AutoPartShop.Application.DTOs.CustomerDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerReadRepository _customerReadRepository;
    private readonly ILogger<CustomerController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public CustomerController(
        ICustomerRepository customerRepository, 
        ICustomerReadRepository customerReadRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<CustomerController> logger)
    {
        _customerRepository = customerRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _customerReadRepository = customerReadRepository;
        _logger = logger;
    }



    [HttpPost("list")]
    public async Task<IActionResult> FindAll([FromBody] CustomerQuery query, CancellationToken cancellationToken)
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


            var (response,total) = await _customerReadRepository.FindAllyAsync(query, cancellationToken);
            return Ok(PagedResult<CustomerResponse>.Create(response,total,query));
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
            var customers = await _customerRepository.GetAllAsync(cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }



    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the customer");
        }
    }

    [HttpGet("code/{customerCode}")]
    public async Task<IActionResult> GetByCode(string customerCode, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByCodeAsync(customerCode, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by code: {CustomerCode}", customerCode);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the customer");
        }
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetByEmail(string email, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByEmailAsync(email, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by email: {Email}", email);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the customer");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _customerRepository.GetByStatusAsync(status, cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers by status: {Status}", status);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }

    [HttpGet("type/{customerType}")]
    public async Task<IActionResult> GetByType(string customerType, CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _customerRepository.GetByTypeAsync(customerType, cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers by type: {CustomerType}", customerType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }

    [HttpGet("city/{city}")]
    public async Task<IActionResult> GetByCity(string city, CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _customerRepository.GetByCityAsync(city, cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers by city: {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }

    [HttpGet("country/{country}")]
    public async Task<IActionResult> GetByCountry(string country, CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _customerRepository.GetByCountryAsync(country, cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers by country: {Country}", country);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _customerRepository.GetActiveAsync(cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active customers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active customers");
        }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            if (limit < 1) limit = 50;
            if (limit > 100) limit = 100;

            var customers = await _customerRepository.GetRecentAsync(limit, cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent customers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving recent customers");
        }
    }

    [HttpGet("search-by-phone")]
    public async Task<IActionResult> GetByPhone([FromQuery] string phone, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { message = "Phone number is required" });

            var customer = await _customerRepository.GetByPhoneAsync(phone, cancellationToken);
            if (customer is null)
                return NotFound(new { message = "Customer not found" });

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by phone: {Phone}", phone);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the customer");
        }
    }

    [HttpGet("credit-limit-exceeded")]
    public async Task<IActionResult> GetCreditLimitExceeded(CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _customerRepository.GetWithCreditLimitExceededAsync(cancellationToken);
            var response = customers.Select(MapToCustomerResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers with exceeded credit limit");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }

    [HttpGet("{id:guid}/credit")]
    public async Task<IActionResult> GetCustomerCredit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null)
                return NotFound(new { message = "Customer not found" });

            var creditInfo = new
            {
                currentBalance = customer.CurrentBalance,
                advanceAmount = customer.AdvanceAmount,
                dueBalance = customer.CurrentBalance
            };

            return Ok(creditInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer credit: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customer credit");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CustomerCode) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest(new { message = "CustomerCode, FirstName, and LastName are required" });

            // Check if customer code already exists
            var existing = await _customerRepository.GetByCodeAsync(request.CustomerCode, cancellationToken);
            if (existing != null)
                return BadRequest(new { message = "Customer with this code already exists" });

            var customer = Customer.Create(
                request.CustomerCode,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Phone,
                request.CompanyName,
                request.BillingAddress,
                request.ShippingAddress,
                request.City,
                request.State,
                request.PostalCode,
                request.Country,
                request.CustomerType,
                request.Notes
            );

            customer.SetPrimaryContactPerson(request.PrimaryContactPerson);
            if (!string.IsNullOrWhiteSpace(request.AlternatePhone))
                customer.UpdateContactInfo(request.Email, request.Phone, request.AlternatePhone);

            var currentUser = _currentUserService.GetCurrentUsername();
            customer.CreatedBy = currentUser;
            customer.ModifiedBy = currentUser;
            await _codeGenerateService.SaveGenerateCodeAsync("CUST", cancellationToken);
            await _customerRepository.AddAsync(customer, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, MapToCustomerResponse(customer));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the customer");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            // Update contact info
            customer.UpdateContactInfo(request.Email, request.Phone, request.AlternatePhone, request.CustomerType);
            // Update address
            customer.UpdateAddress(request.BillingAddress, request.ShippingAddress, request.City, request.State, request.PostalCode, request.Country);

            customer.SetPrimaryContactPerson(request.PrimaryContactPerson);
            customer.UpdateNotes(request.Notes);

            customer.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return Ok(MapToCustomerResponse(customer));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the customer");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            customer.Activate();
            customer.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return Ok(MapToCustomerResponse(customer));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating customer: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the customer");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            customer.Deactivate();
            customer.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the customer");
        }
    }

    [HttpPatch("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            customer.Suspend();
            customer.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending customer: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while suspending the customer");
        }
    }

    [HttpPatch("{id:guid}/blacklist")]
    public async Task<IActionResult> Blacklist(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            customer.Blacklist();
            customer.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return Ok(MapToCustomerResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting customer: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while blacklisting the customer");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _customerRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Customer not found" });

            await _customerRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the customer");
        }
    }

    private CustomerResponse MapToCustomerResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            FullName = customer.GetFullName(),
            Email = customer.Email,
            Phone = customer.Phone,
            AlternatePhone = customer.AlternatePhone,
            CompanyName = customer.CompanyName,
            BillingAddress = customer.BillingAddress,
            ShippingAddress = customer.ShippingAddress,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode,
            Country = customer.Country,
            CustomerType = customer.CustomerType,
            Status = customer.Status,
            CurrentBalance = customer.CurrentBalance,
            AdvanceAmount = customer.AdvanceAmount,  // Available advance credit (sum of RemainingAmount)
            DueAmount = customer.CurrentBalance,      // Outstanding balance (due)
            CanPlaceOrder = customer.CanPlaceOrder(),
            PrimaryContactPerson = customer.PrimaryContactPerson,
            LastPurchaseDate = customer.LastPurchaseDate,
            TotalPurchaseAmount = customer.TotalPurchaseAmount,
            Notes = customer.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
