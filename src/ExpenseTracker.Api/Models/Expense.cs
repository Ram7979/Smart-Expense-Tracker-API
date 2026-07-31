namespace ExpenseTracker.Api.Models;

/// <summary>
/// Core domain entity representing a single expense record.
/// Using a record for immutability at the model level; updates create new instances.
/// </summary>
public class Expense
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public DateTime CreatedAt { get; init; }
}
