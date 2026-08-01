using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Interfaces;

/// <summary>
/// Abstracts the expense data store so the service layer is independent of storage technology.
/// All implementations must be thread-safe.
/// </summary>
public interface IExpenseRepository
{
    /// <summary>Persist a new expense and return it.</summary>
    Expense Add(Expense expense);

    /// <summary>Return all stored expenses, optionally filtered by category (case-insensitive).</summary>
    IEnumerable<Expense> GetAll(string? category = null);

    /// <summary>Return a single expense by ID, or null if not found.</summary>
    Expense? GetById(Guid id);

    /// <summary>Remove an expense by ID. Returns true if deleted, false if not found.</summary>
    bool Delete(Guid id);

    /// <summary>Return the sum of amounts for all expenses, or for a single category.</summary>
    decimal GetTotal(string? category = null);

    /// <summary>Return a dictionary mapping each category name to its total amount.</summary>
    Dictionary<string, decimal> GetTotalsByCategory();
}
