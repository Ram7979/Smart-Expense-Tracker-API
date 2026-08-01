using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

/// <summary>
/// Handles all /api/expenses routes.
/// Controller is deliberately thin: input validation, delegate to service, return result.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;
    private readonly IValidator<CreateExpenseRequest> _validator;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        IExpenseService service,
        IValidator<CreateExpenseRequest> validator,
        ILogger<ExpensesController> logger)
    {
        _service = service;
        _validator = validator;
        _logger = logger;
    }

    // ─── POST /api/expenses ────────────────────────────────────────────────────

    /// <summary>Create a new expense.</summary>
    /// <response code="201">Expense created successfully.</response>
    /// <response code="400">Validation failed — see errors array for field-level detail.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validation failed for CreateExpenseRequest: {Errors}",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

            var problemDetails = new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest
            };
            return BadRequest(problemDetails);
        }

        var created = _service.Add(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // ─── GET /api/expenses ─────────────────────────────────────────────────────

    /// <summary>
    /// Retrieve all expenses. Optionally filter by category (case-insensitive).
    /// Returns an empty array if no expenses match — never 404 on a collection.
    /// </summary>
    /// <param name="category">Optional category filter (case-insensitive).</param>
    /// <response code="200">List of expenses (may be empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] string? category = null)
    {
        var expenses = _service.GetAll(category);
        return Ok(expenses);
    }

    // ─── GET /api/expenses/{id} ────────────────────────────────────────────────

    /// <summary>Retrieve a single expense by ID.</summary>
    /// <response code="200">The expense.</response>
    /// <response code="404">No expense with the given ID exists.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var expense = _service.GetById(id);
        if (expense is null)
        {
            return NotFound(new { error = $"Expense with id '{id}' was not found." });
        }

        return Ok(expense);
    }

    // ─── GET /api/expenses/total ───────────────────────────────────────────────

    /// <summary>
    /// Returns the sum of all expense amounts.
    /// Optionally scoped to a single category.
    /// </summary>
    /// <param name="category">Optional category to scope the total.</param>
    /// <response code="200">Total amount.</response>
    [HttpGet("total")]
    [ProducesResponseType(typeof(TotalResponse), StatusCodes.Status200OK)]
    public IActionResult GetTotal([FromQuery] string? category = null)
    {
        var total = _service.GetTotal(category);
        return Ok(total);
    }

    // ─── GET /api/expenses/totals-by-category ─────────────────────────────────

    /// <summary>Returns a dictionary of category → total amount for all categories.</summary>
    /// <response code="200">Map of category names to their totals.</response>
    [HttpGet("totals-by-category")]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), StatusCodes.Status200OK)]
    public IActionResult GetTotalsByCategory()
    {
        var totals = _service.GetTotalsByCategory();
        return Ok(totals);
    }

    // ─── DELETE /api/expenses/{id} ─────────────────────────────────────────────

    /// <summary>Delete an expense by ID.</summary>
    /// <response code="204">Deleted successfully.</response>
    /// <response code="404">No expense with the given ID exists.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        var deleted = _service.Delete(id);
        if (!deleted)
        {
            return NotFound(new { error = $"Expense with id '{id}' was not found." });
        }

        return NoContent();
    }
}
