using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Validation;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace ExpenseTracker.Tests.Unit;

/// <summary>
/// Unit tests for CreateExpenseRequestValidator.
/// Uses FluentValidation's TestValidate helper so no DI or HTTP stack is needed.
/// </summary>
public class CreateExpenseRequestValidatorTests
{
    private readonly CreateExpenseRequestValidator _validator = new();

    private static CreateExpenseRequest ValidRequest() => new()
    {
        Title = "Lunch",
        Amount = 12.50m,
        Category = "Food",
        Date = DateTime.UtcNow
    };

    // ─── Title ────────────────────────────────────────────────────────────────

    [Fact]
    public void Title_Empty_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Title = string.Empty;

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_Whitespace_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Title = "   ";

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_ExceedsMaxLength_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Title = new string('A', 201); // max is 200

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_AtMaxLength_ShouldNotHaveValidationError()
    {
        var req = ValidRequest();
        req.Title = new string('A', 200);

        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_Valid_ShouldNotHaveValidationError()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    // ─── Amount ───────────────────────────────────────────────────────────────

    [Fact]
    public void Amount_Zero_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Amount = 0m;

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void Amount_Negative_ShouldHaveValidationError(decimal amount)
    {
        var req = ValidRequest();
        req.Amount = amount;

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(100)]
    [InlineData(9999.99)]
    public void Amount_Positive_ShouldNotHaveValidationError(decimal amount)
    {
        var req = ValidRequest();
        req.Amount = amount;

        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    // ─── Category ─────────────────────────────────────────────────────────────

    [Fact]
    public void Category_Empty_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Category = string.Empty;

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Category_Whitespace_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Category = "   ";

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Category_Valid_ShouldNotHaveValidationError()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    // ─── Date ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Date_DefaultValue_ShouldHaveValidationError()
    {
        var req = ValidRequest();
        req.Date = default; // DateTime.MinValue

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Date_Valid_ShouldNotHaveValidationError()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    // ─── Full valid request ───────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_ShouldPassAllRules()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
