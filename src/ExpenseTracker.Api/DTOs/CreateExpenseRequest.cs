namespace ExpenseTracker.Api.DTOs;

/// <summary>
/// Input DTO for creating a new expense. All fields are validated by CreateExpenseRequestValidator.
/// </summary>
public class CreateExpenseRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
