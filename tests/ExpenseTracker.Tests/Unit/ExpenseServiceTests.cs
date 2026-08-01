using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Interfaces;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ExpenseTracker.Tests.Unit;

/// <summary>
/// Unit tests for ExpenseService. The repository is mocked so tests focus purely
/// on service-layer business logic (mapping, delegation, return values).
/// </summary>
public class ExpenseServiceTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static (ExpenseService service, Mock<IExpenseRepository> repoMock) CreateSut()
    {
        var repoMock = new Mock<IExpenseRepository>();
        var logger = NullLogger<ExpenseService>.Instance;
        var service = new ExpenseService(repoMock.Object, logger);
        return (service, repoMock);
    }

    private static Expense MakeDomainExpense(
        Guid? id = null,
        string title = "Lunch",
        decimal amount = 10m,
        string category = "Food") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Amount = amount,
            Category = category,
            Date = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow
        };

    private static CreateExpenseRequest MakeRequest(
        string title = "Lunch",
        decimal amount = 10m,
        string category = "Food",
        DateTime? date = null) =>
        new()
        {
            Title = title,
            Amount = amount,
            Category = category,
            Date = date ?? DateTime.UtcNow
        };

    // ─── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_ValidRequest_ReturnsExpenseResponseWithNewGuid()
    {
        var (service, repoMock) = CreateSut();
        var request = MakeRequest();

        repoMock.Setup(r => r.Add(It.IsAny<Expense>()))
                .Returns<Expense>(e => e);

        var result = service.Add(request);

        result.Id.Should().NotBe(Guid.Empty);
        result.Title.Should().Be(request.Title);
        result.Amount.Should().Be(request.Amount);
        result.Category.Should().Be(request.Category);
    }

    [Fact]
    public void Add_TrimsWhitespaceFromTitleAndCategory()
    {
        var (service, repoMock) = CreateSut();
        var request = MakeRequest(title: "  Coffee  ", category: "  Drinks  ");

        repoMock.Setup(r => r.Add(It.IsAny<Expense>()))
                .Returns<Expense>(e => e);

        var result = service.Add(request);

        result.Title.Should().Be("Coffee");
        result.Category.Should().Be("Drinks");
    }

    [Fact]
    public void Add_CallsRepositoryAddOnce()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.Add(It.IsAny<Expense>())).Returns<Expense>(e => e);

        service.Add(MakeRequest());

        repoMock.Verify(r => r.Add(It.IsAny<Expense>()), Times.Once);
    }

    // ─── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_NoFilter_DelegatesToRepositoryWithNullCategory()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.GetAll(null)).Returns(new List<Expense>());

        service.GetAll();

        repoMock.Verify(r => r.GetAll(null), Times.Once);
    }

    [Fact]
    public void GetAll_WithCategory_PassesCategoryToRepository()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.GetAll("Food")).Returns(new List<Expense>());

        service.GetAll("Food");

        repoMock.Verify(r => r.GetAll("Food"), Times.Once);
    }

    [Fact]
    public void GetAll_MapsEachDomainExpenseToResponseDto()
    {
        var (service, repoMock) = CreateSut();
        var domain = MakeDomainExpense();
        repoMock.Setup(r => r.GetAll(null)).Returns(new List<Expense> { domain });

        var result = service.GetAll().ToList();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(domain.Id);
        result[0].Amount.Should().Be(domain.Amount);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetById_ExistingId_ReturnsResponse()
    {
        var (service, repoMock) = CreateSut();
        var domain = MakeDomainExpense();
        repoMock.Setup(r => r.GetById(domain.Id)).Returns(domain);

        var result = service.GetById(domain.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(domain.Id);
    }

    [Fact]
    public void GetById_NonExistentId_ReturnsNull()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((Expense?)null);

        service.GetById(Guid.NewGuid()).Should().BeNull();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingId_ReturnsTrue()
    {
        var (service, repoMock) = CreateSut();
        var id = Guid.NewGuid();
        repoMock.Setup(r => r.Delete(id)).Returns(true);

        service.Delete(id).Should().BeTrue();
    }

    [Fact]
    public void Delete_NonExistentId_ReturnsFalse()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.Delete(It.IsAny<Guid>())).Returns(false);

        service.Delete(Guid.NewGuid()).Should().BeFalse();
    }

    // ─── GetTotal ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetTotal_NoCategory_ReturnsTotalResponseWithNullCategory()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.GetTotal(null)).Returns(100m);

        var result = service.GetTotal();

        result.Total.Should().Be(100m);
        result.Category.Should().BeNull();
    }

    [Fact]
    public void GetTotal_WithCategory_ReturnsTotalResponseWithCategory()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.GetTotal("Food")).Returns(50m);

        var result = service.GetTotal("Food");

        result.Total.Should().Be(50m);
        result.Category.Should().Be("Food");
    }

    [Fact]
    public void GetTotal_EmptyStore_ReturnsZero()
    {
        var (service, repoMock) = CreateSut();
        repoMock.Setup(r => r.GetTotal(null)).Returns(0m);

        service.GetTotal().Total.Should().Be(0m);
    }

    // ─── GetTotalsByCategory ──────────────────────────────────────────────────

    [Fact]
    public void GetTotalsByCategory_DelegatesToRepository()
    {
        var (service, repoMock) = CreateSut();
        var expected = new Dictionary<string, decimal> { ["Food"] = 15m, ["Transport"] = 20m };
        repoMock.Setup(r => r.GetTotalsByCategory()).Returns(expected);

        var result = service.GetTotalsByCategory();

        result.Should().BeEquivalentTo(expected);
    }
}
