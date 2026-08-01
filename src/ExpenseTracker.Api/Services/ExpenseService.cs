using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Interfaces;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Services;

/// <summary>
/// Contains business logic for expense operations.
/// Responsible for mapping between CreateExpenseRequest/ExpenseResponse and the Expense domain model.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IExpenseRepository repository, ILogger<ExpenseService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public ExpenseResponse Add(CreateExpenseRequest request)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Amount = request.Amount,
            Category = request.Category.Trim(),
            Date = request.Date.Date, // strip time component for consistency
            CreatedAt = DateTime.UtcNow
        };

        var created = _repository.Add(expense);
        _logger.LogInformation("Expense added: Id={Id}, Title={Title}, Amount={Amount}, Category={Category}",
            created.Id, created.Title, created.Amount, created.Category);

        return MapToResponse(created);
    }

    /// <inheritdoc />
    public IEnumerable<ExpenseResponse> GetAll(string? category = null)
    {
        return _repository.GetAll(category).Select(MapToResponse);
    }

    /// <inheritdoc />
    public ExpenseResponse? GetById(Guid id)
    {
        var expense = _repository.GetById(id);
        return expense is null ? null : MapToResponse(expense);
    }

    /// <inheritdoc />
    public bool Delete(Guid id)
    {
        var deleted = _repository.Delete(id);
        if (deleted)
        {
            _logger.LogInformation("Expense deleted: Id={Id}", id);
        }
        else
        {
            _logger.LogWarning("Delete attempted for non-existent expense: Id={Id}", id);
        }
        return deleted;
    }

    /// <inheritdoc />
    public TotalResponse GetTotal(string? category = null)
    {
        var total = _repository.GetTotal(category);
        return new TotalResponse
        {
            Total = total,
            Category = string.IsNullOrWhiteSpace(category) ? null : category
        };
    }

    /// <inheritdoc />
    public Dictionary<string, decimal> GetTotalsByCategory()
    {
        return _repository.GetTotalsByCategory();
    }

    // --- Private helpers ---

    private static ExpenseResponse MapToResponse(Expense expense) =>
        new()
        {
            Id = expense.Id,
            Title = expense.Title,
            Amount = expense.Amount,
            Category = expense.Category,
            Date = expense.Date,
            CreatedAt = expense.CreatedAt
        };
}
