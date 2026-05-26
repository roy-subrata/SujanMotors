using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.SupplierDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/supplier-payment-accounts")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class SupplierPaymentAccountController : ControllerBase
{
    private readonly ISupplierPaymentAccountRepository _repository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierPaymentRepository _paymentRepository;
    private readonly ILogger<SupplierPaymentAccountController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public SupplierPaymentAccountController(
        ISupplierPaymentAccountRepository repository,
        ISupplierRepository supplierRepository,
        ISupplierPaymentRepository paymentRepository,
        ICurrentUserService currentUserService,
        ILogger<SupplierPaymentAccountController> logger)
    {
        _repository = repository;
        _supplierRepository = supplierRepository;
        _paymentRepository = paymentRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all supplier payment accounts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _repository.GetAllAsync(cancellationToken);
            return Ok(accounts.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier payment accounts");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get supplier payment account by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account is null) return NotFound(new { message = "Payment account not found" });
            return Ok(MapResponse(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment account");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get all payment accounts for a supplier
    /// </summary>
    [HttpGet("by-supplier/{supplierId:guid}")]
    public async Task<IActionResult> GetBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _repository.GetBySupplierAsync(supplierId, cancellationToken);
            return Ok(accounts.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment accounts for supplier");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get active payment accounts for a supplier (for dropdown selection)
    /// </summary>
    [HttpGet("by-supplier/{supplierId:guid}/active")]
    public async Task<IActionResult> GetActiveBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _repository.GetActiveBySupplierAsync(supplierId, cancellationToken);
            return Ok(accounts.Select(MapResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active payment accounts for supplier");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get default payment account for a supplier
    /// </summary>
    [HttpGet("by-supplier/{supplierId:guid}/default")]
    public async Task<IActionResult> GetDefaultBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _repository.GetDefaultBySupplierAsync(supplierId, cancellationToken);
            if (account is null) return NotFound(new { message = "No default payment account found" });
            return Ok(MapResponse(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default payment account for supplier");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Create a new supplier payment account
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierPaymentAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate supplier exists
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
            if (supplier is null)
                return BadRequest(new { message = "Supplier not found" });

            var account = SupplierPaymentAccount.Create(
                request.SupplierId,
                request.AccountType,
                request.AccountName,
                request.IsDefault
            );

            // Set type-specific details
            if (request.AccountType.ToUpper() == "BANK_TRANSFER")
            {
                account.SetBankDetails(
                    request.BankName,
                    request.BankAccountNumber,
                    request.BeneficiaryName,
                    request.BankBranchName,
                    request.BankBranchCode
                );
                account.SetInternationalDetails(request.BankIBAN, request.BankSWIFT);
            }
            else if (request.AccountType.ToUpper() == "MOBILE_BANKING")
            {
                account.SetMobileBankingDetails(
                    request.MobileNumber,
                    request.MobileAccountHolderName,
                    request.MobileProvider
                );
            }

            account.UpdateNotes(request.Notes);

            var currentUser = _currentUserService.GetCurrentUsername();
            account.CreatedBy = currentUser;
            account.ModifiedBy = currentUser;

            // If this is default, unset all existing defaults for this supplier first
            if (request.IsDefault)
            {
                var existingAccounts = await _repository.GetBySupplierAsync(request.SupplierId, cancellationToken);
                foreach (var existingAccount in existingAccounts.Where(a => a.IsDefault))
                {
                    existingAccount.SetAsDefault(false);
                    existingAccount.ModifiedBy = account.CreatedBy;
                    await _repository.UpdateAsync(existingAccount, cancellationToken);
                }
            }

            await _repository.AddAsync(account, cancellationToken);

            // Reload with supplier
            var created = await _repository.GetByIdAsync(account.Id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, MapResponse(created!));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment account");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update a supplier payment account
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSupplierPaymentAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account is null) return NotFound(new { message = "Payment account not found" });

            account.Update(request.AccountName, request.IsActive);

            // Update type-specific details
            if (account.AccountType == "BANK_TRANSFER")
            {
                account.SetBankDetails(
                    request.BankName,
                    request.BankAccountNumber,
                    request.BeneficiaryName,
                    request.BankBranchName,
                    request.BankBranchCode
                );
                account.SetInternationalDetails(request.BankIBAN, request.BankSWIFT);
            }
            else if (account.AccountType == "MOBILE_BANKING")
            {
                account.SetMobileBankingDetails(
                    request.MobileNumber,
                    request.MobileAccountHolderName,
                    request.MobileProvider
                );
            }

            account.UpdateNotes(request.Notes);
            account.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _repository.UpdateAsync(account, cancellationToken);
            return Ok(MapResponse(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment account");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Set a payment account as default for the supplier
    /// </summary>
    [HttpPatch("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account is null) return NotFound(new { message = "Payment account not found" });

            var currentUser = _currentUserService.GetCurrentUsername();

            // Set the target as default first — if subsequent unsets fail the intended default is at least in place
            account.SetAsDefault(true);
            account.ModifiedBy = currentUser;
            await _repository.UpdateAsync(account, cancellationToken);

            // Unset all other defaults for this supplier
            var existingAccounts = await _repository.GetBySupplierAsync(account.SupplierId, cancellationToken);
            foreach (var other in existingAccounts.Where(a => a.IsDefault && a.Id != id))
            {
                other.SetAsDefault(false);
                other.ModifiedBy = currentUser;
                await _repository.UpdateAsync(other, cancellationToken);
            }

            return Ok(MapResponse(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment account");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete a payment account (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account is null) return NotFound(new { message = "Payment account not found" });

            var payments = await _paymentRepository.GetBySupplierAsync(account.SupplierId, cancellationToken);
            if (payments.Any(p => p.SupplierPaymentAccountId == id))
                return Conflict(new { message = "Cannot delete a payment account that is linked to existing payments." });

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment account");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    private static SupplierPaymentAccountResponse MapResponse(SupplierPaymentAccount account) => new()
    {
        Id = account.Id,
        SupplierId = account.SupplierId,
        SupplierName = account.Supplier?.Name ?? string.Empty,
        AccountType = account.AccountType,
        AccountName = account.AccountName,
        IsDefault = account.IsDefault,
        IsActive = account.IsActive,
        BankName = account.BankName,
        BankAccountNumber = account.BankAccountNumber,
        BankBranchName = account.BankBranchName,
        BankBranchCode = account.BankBranchCode,
        BeneficiaryName = account.BeneficiaryName,
        BankIBAN = account.BankIBAN,
        BankSWIFT = account.BankSWIFT,
        MobileNumber = account.MobileNumber,
        MobileAccountHolderName = account.MobileAccountHolderName,
        MobileProvider = account.MobileProvider,
        Notes = account.Notes,
        DisplayText = account.GetDisplayText(),
        CreatedAt = account.CreatedDate
    };
}
