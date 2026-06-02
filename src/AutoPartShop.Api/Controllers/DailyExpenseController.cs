using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.ExpenseDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/daily-expense")]
[Route("api/v1/daily-expense")]
[ApiController]
[Authorize]
public class DailyExpenseController : ControllerBase
{
    private readonly IDailyExpenseService _service;
    private readonly ILogger<DailyExpenseController> _logger;

    public DailyExpenseController(
        IDailyExpenseService service,
        ILogger<DailyExpenseController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var expenses = await _service.GetAllExpensesAsync(cancellationToken);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily expenses");
            return StatusCode(500, "An error occurred while retrieving expenses");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var expense = await _service.GetExpenseByIdAsync(id, cancellationToken);
            if (expense == null)
                return NotFound($"Expense with ID {id} not found");

            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", id);
            return StatusCode(500, "An error occurred while retrieving the expense");
        }
    }

    [HttpGet("by-date-range")]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var expenses = await _service.GetExpensesByDateRangeAsync(startDate, endDate, cancellationToken);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by date range");
            return StatusCode(500, "An error occurred while retrieving expenses");
        }
    }

    [HttpGet("by-category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken cancellationToken)
    {
        try
        {
            var expenses = await _service.GetExpensesByCategoryAsync(category, cancellationToken);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by category {Category}", category);
            return StatusCode(500, "An error occurred while retrieving expenses");
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _service.GetExpenseSummaryAsync(startDate, endDate, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary");
            return StatusCode(500, "An error occurred while generating the summary");
        }
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _service.GetExpenseCategoriesAsync(cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDailyExpenseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var expense = await _service.CreateExpenseAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request to create expense");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, "An error occurred while creating the expense");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDailyExpenseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var expense = await _service.UpdateExpenseAsync(id, request, cancellationToken);
            return Ok(expense);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Expense {ExpenseId} not found", id);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request to update expense {ExpenseId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", id);
            return StatusCode(500, "An error occurred while updating the expense");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _service.DeleteExpenseAsync(id, cancellationToken);
            if (!success)
                return NotFound($"Expense with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", id);
            return StatusCode(500, "An error occurred while deleting the expense");
        }
    }
}
