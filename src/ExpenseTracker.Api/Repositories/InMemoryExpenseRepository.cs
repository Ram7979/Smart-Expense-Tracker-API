using System.Collections.Concurrent;
using ExpenseTracker.Api.Interfaces;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Repositories;

/// <summary>
/// Thread-safe in-memory expense store backed by ConcurrentDictionary.
/// ConcurrentDictionary was chosen over List&lt;T&gt;+lock because it provides
/// atomic operations without requiring manual lock management.
/// </summary>
public class InMemoryExpenseRepository : IExpenseRepository
{
    // Key: expense ID. All reads and writes go through thread-safe ConcurrentDictionary APIs.
    private readonly ConcurrentDictionary<Guid, Expense> _store = new();

    /// <inheritdoc />
    public Expense Add(Expense expense)
    {
        _store[expense.Id] = expense;
        return expense;
    }

    /// <inheritdoc />
    public IEnumerable<Expense> GetAll(string? category = null)
    {
        IEnumerable<Expense> expenses = _store.Values;

        if (!string.IsNullOrWhiteSpace(category))
        {
            // Case-insensitive comparison so "food" == "Food" == "FOOD"
            expenses = expenses.Where(e =>
                string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        return expenses.OrderByDescending(e => e.CreatedAt).ToList();
    }

    /// <inheritdoc />
    public Expense? GetById(Guid id)
    {
        _store.TryGetValue(id, out var expense);
        return expense;
    }

    /// <inheritdoc />
    public bool Delete(Guid id)
    {
        return _store.TryRemove(id, out _);
    }

    /// <inheritdoc />
    public decimal GetTotal(string? category = null)
    {
        var expenses = GetAll(category);
        return expenses.Sum(e => e.Amount);
    }

    /// <inheritdoc />
    public Dictionary<string, decimal> GetTotalsByCategory()
    {
        return _store.Values
            .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.Amount),
                StringComparer.OrdinalIgnoreCase);
    }
}
