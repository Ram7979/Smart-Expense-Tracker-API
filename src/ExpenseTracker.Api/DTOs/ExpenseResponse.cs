namespace ExpenseTracker.Api.DTOs;

/// <summary>
/// Response DTO returned to callers after creating or retrieving an expense.
/// Mirrors the Expense model but is explicitly shaped for the API contract.
/// </summary>
public class ExpenseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}
