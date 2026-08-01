using FluentValidation;
using ExpenseTracker.Api.DTOs;

namespace ExpenseTracker.Api.Validation;

/// <summary>
/// FluentValidation rules for the CreateExpenseRequest DTO.
/// Rules are intentionally kept short and explicit; no magic numbers outside this file.
/// </summary>
public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    private const int TitleMaxLength = 200;

    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must not exceed {TitleMaxLength} characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeAValidDate).WithMessage("Date must be a valid date.");
    }

    private static bool BeAValidDate(DateTime date)
    {
        // Reject DateTime.MinValue (0001-01-01) which is the default when JSON deserialization
        // fails or when no date is provided. Also reject DateTime.MaxValue as obviously invalid.
        // A year range of 1900–2100 covers all realistic expense dates.
        return date.Year >= 1900 && date.Year <= 2100;
    }
}
