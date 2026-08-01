using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Repositories;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Tests.Unit;

/// <summary>
/// Unit tests for InMemoryExpenseRepository in isolation — no HTTP stack.
/// Each test constructs a fresh repository instance to avoid shared mutable state.
/// </summary>
public class InMemoryExpenseRepositoryTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Expense MakeExpense(
        string title = "Lunch",
        decimal amount = 10m,
        string category = "Food",
        DateTime? date = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Amount = amount,
            Category = category,
            Date = date ?? DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow
        };

    private static InMemoryExpenseRepository EmptyRepo() => new();

    private static InMemoryExpenseRepository RepoWith(params Expense[] expenses)
    {
        var repo = new InMemoryExpenseRepository();
        foreach (var e in expenses) repo.Add(e);
        return repo;
    }

    // ─── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_ShouldPersistAndReturnExpense()
    {
        var repo = EmptyRepo();
        var expense = MakeExpense();

        var result = repo.Add(expense);

        result.Should().BeEquivalentTo(expense);
        repo.GetById(expense.Id).Should().NotBeNull();
    }

    // ─── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_EmptyRepository_ReturnsEmptyCollection()
    {
        var repo = EmptyRepo();
        repo.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_NoFilter_ReturnsAllExpenses()
    {
        var e1 = MakeExpense("Coffee", 5m, "Drinks");
        var e2 = MakeExpense("Taxi", 20m, "Transport");
        var repo = RepoWith(e1, e2);

        repo.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void GetAll_CategoryFilter_ReturnsCaseInsensitiveMatch()
    {
        var food1 = MakeExpense("Pizza", 15m, "Food");
        var food2 = MakeExpense("Burger", 12m, "food");  // lowercase
        var other = MakeExpense("Bus", 3m, "Transport");
        var repo = RepoWith(food1, food2, other);

        var result = repo.GetAll("FOOD"); // uppercase query

        result.Should().HaveCount(2)
              .And.OnlyContain(e => e.Category.Equals("food", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetAll_CategoryFilter_NoMatch_ReturnsEmptyCollection()
    {
        var repo = RepoWith(MakeExpense(category: "Food"));

        var result = repo.GetAll("Nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_WhitespaceCategory_TreatedAsNoFilter()
    {
        var repo = RepoWith(MakeExpense(), MakeExpense());

        // A blank category should return everything
        repo.GetAll("   ").Should().HaveCount(2);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetById_ExistingId_ReturnsExpense()
    {
        var expense = MakeExpense();
        var repo = RepoWith(expense);

        repo.GetById(expense.Id).Should().BeEquivalentTo(expense);
    }

    [Fact]
    public void GetById_NonExistentId_ReturnsNull()
    {
        var repo = EmptyRepo();
        repo.GetById(Guid.NewGuid()).Should().BeNull();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingId_ReturnsTrueAndRemovesExpense()
    {
        var expense = MakeExpense();
        var repo = RepoWith(expense);

        var result = repo.Delete(expense.Id);

        result.Should().BeTrue();
        repo.GetById(expense.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_NonExistentId_ReturnsFalse()
    {
        var repo = EmptyRepo();
        repo.Delete(Guid.NewGuid()).Should().BeFalse();
    }

    // ─── GetTotal ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetTotal_EmptyRepository_ReturnsZero()
    {
        EmptyRepo().GetTotal().Should().Be(0m);
    }

    [Fact]
    public void GetTotal_AllExpenses_ReturnsSumOfAmounts()
    {
        var repo = RepoWith(
            MakeExpense(amount: 10m, category: "Food"),
            MakeExpense(amount: 20m, category: "Transport"),
            MakeExpense(amount: 5m, category: "Food"));

        repo.GetTotal().Should().Be(35m);
    }

    [Fact]
    public void GetTotal_WithCategory_ReturnsCategorySum()
    {
        var repo = RepoWith(
            MakeExpense(amount: 10m, category: "Food"),
            MakeExpense(amount: 20m, category: "Transport"),
            MakeExpense(amount: 5m, category: "Food"));

        repo.GetTotal("Food").Should().Be(15m);
    }

    [Fact]
    public void GetTotal_WithCategory_CaseInsensitive()
    {
        var repo = RepoWith(
            MakeExpense(amount: 10m, category: "Food"),
            MakeExpense(amount: 5m, category: "FOOD"));

        repo.GetTotal("food").Should().Be(15m);
    }

    [Fact]
    public void GetTotal_CategoryWithNoMatch_ReturnsZero()
    {
        var repo = RepoWith(MakeExpense(amount: 10m, category: "Food"));
        repo.GetTotal("Transport").Should().Be(0m);
    }

    // ─── GetTotalsByCategory ──────────────────────────────────────────────────

    [Fact]
    public void GetTotalsByCategory_EmptyRepository_ReturnsEmptyDictionary()
    {
        EmptyRepo().GetTotalsByCategory().Should().BeEmpty();
    }

    [Fact]
    public void GetTotalsByCategory_ReturnsCorrectGroupings()
    {
        var repo = RepoWith(
            MakeExpense(amount: 10m, category: "Food"),
            MakeExpense(amount: 5m, category: "Food"),
            MakeExpense(amount: 20m, category: "Transport"));

        var result = repo.GetTotalsByCategory();

        result.Should().HaveCount(2);
        result["Food"].Should().Be(15m);
        result["Transport"].Should().Be(20m);
    }

    [Fact]
    public void GetTotalsByCategory_GroupsByCaseInsensitive()
    {
        var repo = RepoWith(
            MakeExpense(amount: 10m, category: "food"),
            MakeExpense(amount: 5m, category: "FOOD"));

        var result = repo.GetTotalsByCategory();

        // Both "food" and "FOOD" should merge into one group
        result.Should().HaveCount(1);
        result.Values.Single().Should().Be(15m);
    }
}
