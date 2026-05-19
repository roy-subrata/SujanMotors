using AutoPartShop.Application.DTOs.ExpenseDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

public interface IDailyExpenseService
{
    Task<DailyExpenseResponse> CreateExpenseAsync(CreateDailyExpenseRequest request, CancellationToken cancellationToken = default);
    Task<DailyExpenseResponse> UpdateExpenseAsync(Guid id, UpdateDailyExpenseRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteExpenseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyExpenseResponse?> GetExpenseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DailyExpenseResponse>> GetAllExpensesAsync(CancellationToken cancellationToken = default);
    Task<List<DailyExpenseResponse>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<DailyExpenseResponse>> GetExpensesByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<ExpenseSummaryByPeriod> GetExpenseSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<ExpenseCategory>> GetExpenseCategoriesAsync(CancellationToken cancellationToken = default);
}

public class DailyExpenseService(IDailyExpenseRepository repository) : IDailyExpenseService
{
    public async Task<DailyExpenseResponse> CreateExpenseAsync(CreateDailyExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = DailyExpense.Create(
            request.ExpenseDate,
            request.Category,
            request.Amount,
            request.Description,
            request.PaymentMethod,
            request.VendorName
        );

        if (!string.IsNullOrWhiteSpace(request.ReferenceNumber))
            expense.SetReferenceNumber(request.ReferenceNumber);

        if (!string.IsNullOrWhiteSpace(request.Notes))
            expense.UpdateNotes(request.Notes);

        if (request.IsRecurring)
            expense.SetRecurring(true, request.RecurrencePattern);

        await repository.AddAsync(expense, cancellationToken);

        return MapToResponse(expense);
    }

    public async Task<DailyExpenseResponse> UpdateExpenseAsync(Guid id, UpdateDailyExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await repository.GetByIdAsync(id, cancellationToken);
        if (expense == null)
            throw new InvalidOperationException($"Expense with ID {id} not found");

        expense.Update(
            request.ExpenseDate,
            request.Category,
            request.Amount,
            request.Description,
            request.PaymentMethod,
            request.VendorName
        );

        expense.SetReferenceNumber(request.ReferenceNumber);
        expense.UpdateNotes(request.Notes);
        expense.SetRecurring(request.IsRecurring, request.RecurrencePattern);

        await repository.UpdateAsync(expense, cancellationToken);

        return MapToResponse(expense);
    }

    public async Task<bool> DeleteExpenseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await repository.ExistsAsync(id, cancellationToken);
        if (!exists)
            return false;

        await repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    public async Task<DailyExpenseResponse?> GetExpenseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await repository.GetByIdAsync(id, cancellationToken);
        return expense == null ? null : MapToResponse(expense);
    }

    public async Task<List<DailyExpenseResponse>> GetAllExpensesAsync(CancellationToken cancellationToken = default)
    {
        var expenses = await repository.GetAllAsync(cancellationToken);
        return expenses.Select(MapToResponse).ToList();
    }

    public async Task<List<DailyExpenseResponse>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var expenses = await repository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        return expenses.Select(MapToResponse).ToList();
    }

    public async Task<List<DailyExpenseResponse>> GetExpensesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var expenses = await repository.GetByCategoryAsync(category, cancellationToken);
        return expenses.Select(MapToResponse).ToList();
    }

    public async Task<ExpenseSummaryByPeriod> GetExpenseSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var expenses = await repository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        var expenseList = expenses.ToList();

        var totalExpenses = expenseList.Sum(e => e.Amount);
        var daysDiff = (endDate - startDate).Days + 1;
        var averageDailyExpense = daysDiff > 0 ? totalExpenses / daysDiff : 0;

        var byCategory = expenseList
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseSummaryByCategory
            {
                Category = g.Key,
                TotalAmount = g.Sum(e => e.Amount),
                ExpenseCount = g.Count(),
                AverageAmount = g.Average(e => e.Amount),
                MinAmount = g.Min(e => e.Amount),
                MaxAmount = g.Max(e => e.Amount)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var recentExpenses = expenseList
            .OrderByDescending(e => e.ExpenseDate)
            .Take(10)
            .Select(MapToResponse)
            .ToList();

        return new ExpenseSummaryByPeriod
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalExpenses = totalExpenses,
            ExpenseCount = expenseList.Count,
            AverageDailyExpense = averageDailyExpense,
            ByCategory = byCategory,
            RecentExpenses = recentExpenses
        };
    }

    public Task<List<ExpenseCategory>> GetExpenseCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // Return predefined expense categories
        var categories = new List<ExpenseCategory>
        {
            new() { Value = "RENT", Label = "Rent", Icon = "pi-home" },
            new() { Value = "UTILITIES", Label = "Utilities", Icon = "pi-bolt" },
            new() { Value = "SALARIES", Label = "Salaries", Icon = "pi-users" },
            new() { Value = "OFFICE_SUPPLIES", Label = "Office Supplies", Icon = "pi-box" },
            new() { Value = "MARKETING", Label = "Marketing", Icon = "pi-megaphone" },
            new() { Value = "MAINTENANCE", Label = "Maintenance", Icon = "pi-wrench" },
            new() { Value = "TRANSPORTATION", Label = "Transportation", Icon = "pi-car" },
            new() { Value = "INSURANCE", Label = "Insurance", Icon = "pi-shield" },
            new() { Value = "TAXES", Label = "Taxes", Icon = "pi-calculator" },
            new() { Value = "PROFESSIONAL_FEES", Label = "Professional Fees", Icon = "pi-briefcase" },
            new() { Value = "COMMUNICATION", Label = "Communication", Icon = "pi-phone" },
            new() { Value = "EQUIPMENT", Label = "Equipment", Icon = "pi-desktop" },
            new() { Value = "SOFTWARE", Label = "Software & Subscriptions", Icon = "pi-cloud" },
            new() { Value = "TRAINING", Label = "Training & Development", Icon = "pi-book" },
            new() { Value = "MISCELLANEOUS", Label = "Miscellaneous", Icon = "pi-ellipsis-h" }
        };

        return Task.FromResult(categories);
    }

    private static DailyExpenseResponse MapToResponse(DailyExpense expense)
    {
        return new DailyExpenseResponse
        {
            Id = expense.Id,
            ExpenseDate = expense.ExpenseDate,
            Category = expense.Category,
            Amount = expense.Amount,
            Description = expense.Description,
            PaymentMethod = expense.PaymentMethod,
            VendorName = expense.VendorName,
            ReferenceNumber = expense.ReferenceNumber,
            Notes = expense.Notes,
            IsRecurring = expense.IsRecurring,
            RecurrencePattern = expense.RecurrencePattern,
            CreatedDate = expense.CreatedDate,
            ModifiedDate = expense.ModifiedDate,
            CreatedBy = expense.CreatedBy,
            ModifiedBy = expense.ModifiedBy
        };
    }
}
