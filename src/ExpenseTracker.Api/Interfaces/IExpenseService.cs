using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Interfaces;

/// <summary>
/// Encapsulates business logic for expenses, keeping controllers thin.
/// The service owns mapping between domain models and DTOs.
/// </summary>
public interface IExpenseService
{
    ExpenseResponse Add(CreateExpenseRequest request);
    IEnumerable<ExpenseResponse> GetAll(string? category = null);
    ExpenseResponse? GetById(Guid id);
    bool Delete(Guid id);
    TotalResponse GetTotal(string? category = null);
    Dictionary<string, decimal> GetTotalsByCategory();
}
