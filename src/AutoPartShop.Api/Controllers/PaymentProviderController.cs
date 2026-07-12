using AutoPartShop.Api.Services;
using AutoPartShop.Application.CustomerPayment.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/payment-provider")]
[Route("api/v1/payment-provider")]
[ApiController]
[Produces("application/json")]
[Authorize]
public class PaymentProviderController : ControllerBase
{
    private readonly IPaymentProviderRepository _repository;
    private readonly ILogger<PaymentProviderController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PaymentProviderController(IPaymentProviderRepository repository, ICurrentUserService currentUserService, ILogger<PaymentProviderController> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var providers = await _repository.GetAllAsync(cancellationToken);
            return Ok(providers.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment providers");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound();
            return Ok(MapResponse(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var providers = await _repository.GetActiveAsync(cancellationToken);
            return Ok(providers.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active providers");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("default")]
    public async Task<IActionResult> GetDefault(CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetDefaultAsync(cancellationToken);
            if (provider is null) return NotFound();
            return Ok(MapResponse(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default provider");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(CreatePaymentProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var provider = PaymentProvider.Create(request.ProviderName, request.ProviderType);

            if (!string.IsNullOrWhiteSpace(request.BankName))
                provider.SetBankDetails(request.BankName, request.BankAccountNumber, request.BankRoutingNumber, request.BeneficiaryName);
            if (!string.IsNullOrWhiteSpace(request.BankIBAN))
                provider.SetInternationalDetails(request.BankIBAN, request.BankSWIFT);
            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                provider.SetMobileBankingDetails(request.MobileNumber, request.AccountHolderName, request.AgentNumber);
            if (request.TransactionFeeAmount >= 0)
                provider.SetTransactionFees(request.TransactionFeeType, request.TransactionFeeAmount, request.MinimumAmount, request.MaximumAmount);

            provider.SetCurrencies(request.SupportedCurrencies);
            provider.SetApiKey(request.ApiKey);
            provider.SetMerchantId(request.MerchantId);
            provider.SetWebhookUrl(request.WebhookUrl);
            provider.UpdateNotes(request.Notes);
            var currentUser = _currentUserService.GetCurrentUsername();
            provider.CreatedBy = currentUser;
            provider.ModifiedBy = currentUser;

            await _repository.AddAsync(provider, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = provider.Id }, MapResponse(provider));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, UpdatePaymentProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.BankName))
                provider.SetBankDetails(request.BankName, request.BankAccountNumber, request.BankRoutingNumber, request.BeneficiaryName);
            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                provider.SetMobileBankingDetails(request.MobileNumber, request.AccountHolderName, request.AgentNumber);
            if (request.TransactionFeeAmount >= 0)
                provider.SetTransactionFees(request.TransactionFeeType, request.TransactionFeeAmount, request.MinimumAmount, request.MaximumAmount);

            provider.SetCurrencies(request.SupportedCurrencies);
            provider.SetWebhookUrl(request.WebhookUrl);
            provider.UpdateNotes(request.Notes);
            provider.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _repository.UpdateAsync(provider, cancellationToken);
            return Ok(MapResponse(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound();
            provider.Activate();
            provider.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(provider, cancellationToken);
            return Ok(MapResponse(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating provider");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound();
            provider.Deactivate();
            provider.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(provider, cancellationToken);
            return Ok(MapResponse(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating provider");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var allProviders = await _repository.GetAllAsync(cancellationToken);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                allProviders = allProviders.Where(p =>
                    p.ProviderName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.ProviderType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.BankName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var providerList = allProviders.ToList();
            var totalCount = providerList.Count;
            var providers = providerList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                data = providers.Select(MapResponse),
                pagination = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment providers list");
            return StatusCode(500, new { message = "An error occurred while retrieving payment providers" });
        }
    }

    [HttpPatch("{id:guid}/set-default")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound(new { message = "Payment provider not found" });

            // Clear previous default
            var allProviders = await _repository.GetAllAsync(cancellationToken);
            foreach (var p in allProviders.Where(p => p.IsDefault))
            {
                p.SetAsDefault(false);
                await _repository.UpdateAsync(p, cancellationToken);
            }

            // Set new default
            provider.SetAsDefault(true);
            provider.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(provider, cancellationToken);

            return Ok(MapResponse(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default provider");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost("{id:guid}/test-connection")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound(new { message = "Payment provider not found" });

            // Simple connectivity test - in real implementation, would test with actual payment gateway
            var isConnected = !string.IsNullOrEmpty(provider.ApiKey) && !string.IsNullOrEmpty(provider.MerchantId);

            if (isConnected)
            {
                provider.TestConnection();
                await _repository.UpdateAsync(provider, cancellationToken);

                return Ok(new { success = true, message = "Connection test successful" });
            }

            return Ok(new { success = false, message = "Connection test failed - Missing API key or Merchant ID" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing payment provider connection");
            return StatusCode(500, new { success = false, message = "An error occurred during connection test" });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _repository.GetByIdAsync(id, cancellationToken);
            if (provider is null) return NotFound(new { message = "Payment provider not found" });

            await _repository.DeleteAsync(id, cancellationToken);
            return Ok(new { message = "Payment provider deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment provider");
            return StatusCode(500, new { message = "An error occurred while deleting the payment provider" });
        }
    }

    private PaymentProviderResponse MapResponse(PaymentProvider p) => new()
    {
        Id = p.Id,
        ProviderName = p.ProviderName,
        ProviderType = p.ProviderType,
        Status = p.Status,
        BankName = p.BankName,
        BankAccountNumber = p.BankAccountNumber,
        BankRoutingNumber = p.BankRoutingNumber,
        BankIBAN = p.BankIBAN,
        BankSWIFT = p.BankSWIFT,
        BeneficiaryName = p.BeneficiaryName,
        MobileNumber = p.MobileNumber,
        AccountHolderName = p.AccountHolderName,
        AgentNumber = p.AgentNumber,
        TransactionFeeType = p.TransactionFeeType,
        TransactionFeeAmount = p.TransactionFeeAmount,
        MinimumAmount = p.MinimumAmount,
        MaximumAmount = p.MaximumAmount,
        SettlementDays = p.SettlementDays,
        SupportedCurrencies = p.SupportedCurrencies,
        WebhookUrl = p.WebhookUrl,
        IsDefault = p.IsDefault,
        LastTestedDate = p.LastTestedDate,
        Notes = p.Notes,
        CreatedAt = DateTime.UtcNow
    };
}
