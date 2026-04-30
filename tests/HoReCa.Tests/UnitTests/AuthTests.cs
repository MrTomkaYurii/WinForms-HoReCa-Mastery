using FluentAssertions;
using FluentValidation.TestHelper;
using HoReCa.Application.DTOs;
using HoReCa.Application.Validators;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using HoReCa.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HoReCa.Tests.UnitTests;

public class AuthTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HoReCaDbContext> _options;
    private readonly AuthService _authService;
    private readonly RegisterUserRequestValidator _validator = new();

    private class TestDbContextFactory(DbContextOptions<HoReCaDbContext> options) : IDbContextFactory<HoReCaDbContext>
    {
        public HoReCaDbContext CreateDbContext() => new(options);
        public Task<HoReCaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new HoReCaDbContext(options));
    }

    public AuthTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<HoReCaDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initContext = new HoReCaDbContext(_options);
        initContext.Database.EnsureCreated();

        // Seed default test user
        var user = new UserAccount
        {
            Username = "testadmin",
            FullName = "Головний Тестувальник",
            PasswordHash = PasswordHelper.HashPassword("secret123"),
            PinCode = "7788",
            Role = UserRole.Administrator,
            IsActive = true
        };
        initContext.Users.Add(user);
        initContext.SaveChanges();

        _authService = new AuthService(new TestDbContextFactory(_options));
    }

    [Fact]
    public void Validator_WhenValidData_ShouldNotHaveErrors()
    {
        var request = new RegisterUserRequest("johndoe", "John Doe", "password123", "1234", UserRole.Waiter);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_WhenPinNotFourDigits_ShouldFail()
    {
        var request = new RegisterUserRequest("johndoe", "John Doe", "password123", "12", UserRole.Waiter);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PinCode)
            .WithErrorMessage("PIN-код має складатися рівно з 4 цифр.");
    }

    [Fact]
    public void Validator_WhenPasswordTooShort_ShouldFail()
    {
        var request = new RegisterUserRequest("johndoe", "John Doe", "123", "1234", UserRole.Waiter);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task LoginAsync_WithValidUsernameAndPassword_ShouldSucceed()
    {
        var result = await _authService.LoginAsync("testadmin", "secret123");

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("testadmin");
        result.User.Role.Should().Be(UserRole.Administrator);
        _authService.CurrentUser.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValid4DigitPin_ShouldSucceedWithoutPassword()
    {
        var result = await _authService.LoginAsync("7788", null);

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("testadmin");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldFail()
    {
        var result = await _authService.LoginAsync("testadmin", "wrong_password");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Невірний пароль");
    }

    [Fact]
    public async Task RegisterAsync_WithNewUser_ShouldCreateUserAndAuthenticate()
    {
        var request = new RegisterUserRequest("newwaiter", "Олена Сидоренко", "waiterPass", "9900", UserRole.Waiter);

        var result = await _authService.RegisterAsync(request);

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("newwaiter");
        result.User.Role.Should().Be(UserRole.Waiter);

        using var verifyContext = new HoReCaDbContext(_options);
        var created = await verifyContext.Users.FirstOrDefaultAsync(u => u.Username == "newwaiter");
        created.Should().NotBeNull();
        created!.FullName.Should().Be("Олена Сидоренко");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ShouldFail()
    {
        var request = new RegisterUserRequest("testadmin", "Дублікат", "pass1234", "1122", UserRole.Chef);

        var result = await _authService.RegisterAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("вже зареєстрований");
    }

    [Fact]
    public async Task QuickLoginAsync_ShouldAuthenticateUserByRole()
    {
        var result = await _authService.QuickLoginAsync(UserRole.Administrator);

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be(UserRole.Administrator);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
