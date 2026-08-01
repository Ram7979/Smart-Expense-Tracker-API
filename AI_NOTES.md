# AI Notes — Smart Expense Tracker API

## 1. AI-Generated vs. Hand-Reviewed / Changed

### Fully AI-generated (reviewed but not structurally changed)
- `src/ExpenseTracker.Api/Models/Expense.cs` — straightforward entity class; reviewed and kept as-is since `init`-only properties give the right immutability without needing a record type.
- `src/ExpenseTracker.Api/DTOs/CreateExpenseRequest.cs`, `ExpenseResponse.cs`, `TotalResponse.cs` — simple data bags; AI draft was correct.
- `src/ExpenseTracker.Api/Interfaces/IExpenseRepository.cs` and `IExpenseService.cs` — AI-drafted the method signatures; reviewed to confirm they matched the exact functional requirements from the spec.
- `src/ExpenseTracker.Api/Services/ExpenseService.cs` — AI generated the mapping logic; reviewed for correctness of `Date = request.Date.Date` (stripping the time component for consistency) and confirmed trim logic on Title/Category.
- `src/ExpenseTracker.Api/Validation/CreateExpenseRequestValidator.cs` — AI drafted the rules; manually adjusted the `TitleMaxLength = 200` constant and confirmed the `BeAValidDate` helper correctly rejects `DateTime.MinValue`.
- `tests/ExpenseTracker.Tests/Unit/ValidatorTests.cs` — AI-generated `[Theory]` / `[InlineData]` parameterized tests for amount boundaries; reviewed all `InlineData` values.

### Reviewed and materially changed by hand

- **`src/ExpenseTracker.Api/Repositories/InMemoryExpenseRepository.cs`** — The initial AI suggestion used a plain `List<Expense>` with a `lock` statement for thread safety. This was changed to `ConcurrentDictionary<Guid, Expense>` because `ConcurrentDictionary` provides atomic `TryAdd`, `TryRemove`, and `TryGetValue` operations without requiring manual lock management, making the code cleaner and less error-prone under concurrent load. The `GetAll` method's case-insensitive `StringComparison.OrdinalIgnoreCase` filter was also hand-tuned after noticing the initial draft used `.ToLower()` which is locale-sensitive.

- **`src/ExpenseTracker.Api/Controllers/ExpensesController.cs`** — The AI initially put all validation logic inline in the controller. This was refactored so the controller only calls `await _validator.ValidateAsync(request)` and delegates everything else to the service. The `[ProducesResponseType]` attributes and XML doc `<summary>` tags were added manually to ensure Swagger UI shows correct response schemas.

- **`src/ExpenseTracker.Api/Program.cs`** — `IExpenseRepository` was registered as `Singleton` (not `Scoped`) — the AI initially suggested `Scoped`, which would create a new, empty repository on each request, defeating the in-memory store entirely. Changed to `Singleton` so one instance persists for the process lifetime.

- **`tests/ExpenseTracker.Tests/Integration/ExpensesIntegrationTests.cs`** — The initial AI draft used `IClassFixture<WebApplicationFactory<Program>>` for every test and called the same shared `_client`, which caused state leakage between tests (e.g., totals accumulated across test runs). Fixed by creating `new WebApplicationFactory<Program>()` with `await using` disposal inside each test that needs a clean state, while keeping the `IClassFixture` client only for tests that do not require isolation (e.g., 404 responses).

---

## 2. What Was Validated, Tested, or Verified

- **`dotnet restore src/ExpenseTracker.sln`** — Ran locally; all NuGet packages resolved cleanly. Confirmed `FluentValidation.AspNetCore 11.3.0`, `Swashbuckle.AspNetCore 6.6.2`, `xunit 2.7.0`, `FluentAssertions 6.12.0`, and `Moq 4.20.70` are compatible with `net8.0`.

- **`dotnet build src/ExpenseTracker.sln`** — Confirmed zero warnings and zero errors on a clean build.

- **`dotnet test tests/ExpenseTracker.Tests`** — Ran the full test suite; all tests passed. Specific cases confirmed:
  - `GetAll_CategoryFilter_ReturnsCaseInsensitiveMatch` — verified "FOOD" query matches "Food" and "food" stored entries.
  - `GetTotal_WithCategory_CaseInsensitive` — verified sum uses `OrdinalIgnoreCase` grouping.
  - `Post_DefaultDate_Returns400WithValidationError` — verified the `BeAValidDate` predicate correctly rejects `DateTime.MinValue`.
  - `Delete_NonExistentId_Returns404` — verified the `{id:guid}` route constraint prevents non-GUID strings from even reaching the controller.

- **Case-sensitivity bug fix** — During testing, category filtering initially failed for mixed-case inputs (`"food"` stored, `"FOOD"` queried) because the first draft of the repository used `.Category.ToLower()` comparison. Replaced with `StringComparison.OrdinalIgnoreCase` throughout to be locale-safe.

- **Swagger UI** — Opened `http://localhost:5000/swagger` in browser after `dotnet run`; confirmed all 7 route entries appear with correct HTTP verbs, parameter descriptions, and example response schemas derived from `[ProducesResponseType]` attributes.

---

## 3. AI Suggestions That Were Rejected

**Rejected: JSON file persistence using `System.Text.Json` to serialize the expense list to a local `.json` file.**

The AI initially proposed persisting the in-memory store to a `data/expenses.json` file on every write as a low-friction durability option. This was rejected for the following reasons:

1. **Automated grading risk.** Automated test runners may execute in read-only directories, tmp directories, or with differing working directory assumptions. File I/O introduces failure modes that are hard to diagnose remotely.
2. **State contamination between test runs.** If the file exists from a previous run, the next `dotnet test` invocation starts with stale data, making tests non-deterministic unless explicit cleanup is added.
3. **Complexity without assignment benefit.** The assignment explicitly permits in-memory storage. Adding file persistence adds ~50 lines of serialization/deserialization code, error handling for I/O exceptions, and file locking logic — all for a feature the spec does not require and that could introduce bugs under time pressure.

The tradeoff (volatility for simplicity and test reliability) is explicitly documented in the README's "Known Limitations" section, and the `IExpenseRepository` interface makes a future storage migration straightforward.

**Rejected: EF Core + SQLite as the persistence layer.**

An early AI suggestion proposed adding `Microsoft.EntityFrameworkCore.Sqlite` to avoid implementing a custom repository. Rejected because: it requires the reviewer to have SQLite native libraries available (or the `Microsoft.Data.Sqlite.Core` native bundle), adds migration commands to the setup steps, and defeats the "zero-config, clone and run" goal. The interface-based repository pattern achieves the same testability benefit without any of the setup friction.
