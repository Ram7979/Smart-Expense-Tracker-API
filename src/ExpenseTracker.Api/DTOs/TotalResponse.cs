namespace ExpenseTracker.Api.DTOs;

/// <summary>
/// DTO wrapping the total amount for a /total query.
/// </summary>
public class TotalResponse
{
    public decimal Total { get; set; }
    public string? Category { get; set; }
}
