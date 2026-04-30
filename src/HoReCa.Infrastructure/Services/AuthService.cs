using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<HoReCaDbContext> _contextFactory;
    public UserAccountDto? CurrentUser { get; private set; }

    public AuthService(IDbContextFactory<HoReCaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AuthResult> LoginAsync(string usernameOrPin, string? password, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        UserAccount? user = null;

        // Try login by 4-digit PIN first if string is 4 digits
        if (usernameOrPin.Length == 4 && usernameOrPin.All(char.IsDigit))
        {
            user = await context.Users
                .FirstOrDefaultAsync(u => u.PinCode == usernameOrPin && u.IsActive, ct);
        }

        // Otherwise login by username & password
        if (user == null)
        {
            var trimmedUsername = usernameOrPin.Trim().ToLower();
            user = await context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == trimmedUsername && u.IsActive, ct);

            if (user != null && !string.IsNullOrEmpty(password))
            {
                if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
                {
                    return new AuthResult(false, "Невірний пароль для вказаного облікового запису.", null);
                }
            }
            else if (user != null && string.IsNullOrEmpty(password))
            {
                return new AuthResult(false, "Введіть пароль для входу або скористайтесь 4-значним PIN-кодом.", null);
            }
        }

        if (user == null)
        {
            return new AuthResult(false, "Користувача з таким логіном або PIN-кодом не знайдено.", null);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        CurrentUser = MapToDto(user);
        return new AuthResult(true, null, CurrentUser);
    }

    public async Task<AuthResult> QuickLoginAsync(UserRole role, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Role == role && u.IsActive, ct);

        if (user == null)
        {
            return new AuthResult(false, $"Тестовий акаунт для ролі {role} не знайдено.", null);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        CurrentUser = MapToDto(user);
        return new AuthResult(true, null, CurrentUser);
    }

    public async Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var trimmedUsername = request.Username.Trim().ToLower();
        var exists = await context.Users.AnyAsync(u => u.Username.ToLower() == trimmedUsername, ct);
        if (exists)
        {
            return new AuthResult(false, $"Користувач із логіном '{request.Username}' вже зареєстрований.", null);
        }

        var newUser = new UserAccount
        {
            Username = request.Username.Trim(),
            FullName = request.FullName.Trim(),
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            PinCode = request.PinCode.Trim(),
            Role = request.Role,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync(ct);

        CurrentUser = MapToDto(newUser);
        return new AuthResult(true, null, CurrentUser);
    }

    public async Task<List<UserAccountDto>> GetDemoAccountsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Role)
            .Select(u => MapToDto(u))
            .ToListAsync(ct);
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    private static UserAccountDto MapToDto(UserAccount u) =>
        new(u.Id, u.Username, u.FullName, u.PinCode, u.Role, u.IsActive);
}
