# Lesson 02: Dual Database Persistence with EF Core 10 & IDbContextFactory

## 1. Overview

In desktop enterprise software, database connectivity faces unique challenges:
1. **Network Disconnection**: On-premise terminals might lose access to a centralized PostgreSQL cluster.
2. **Developer Onboarding**: New developers or automated testing pipelines need a zero-configuration engine (SQLite) that boots instantly without requiring a running Docker or PostgreSQL service.
3. **Threading Model**: A desktop app executes user actions and background workers concurrently. A single EF Core `DbContext` instance is **not thread-safe** and will throw `InvalidOperationException` if accessed simultaneously.

In this lesson, we implement dual-database provider support and register `IDbContextFactory<HoReCaDbContext>`.

---

## 2. The IDbContextFactory Pattern

Instead of injecting `HoReCaDbContext` directly into services, we inject `IDbContextFactory<HoReCaDbContext>`. Each service method creates and disposes of a dedicated context:

```csharp
public async Task<List<TableDto>> GetTablesByZoneAsync(int? zoneId = null, CancellationToken ct = default)
{
    await using var context = await _contextFactory.CreateDbContextAsync(ct);
    
    var query = context.Tables
        .Include(t => t.Zone)
        .AsNoTracking();

    if (zoneId.HasValue)
        query = query.Where(t => t.ZoneId == zoneId.Value);

    return await query.Select(t => t.ToDto()).ToListAsync(ct);
}
```

---

## 3. Provider Switching Logic

We inspect `appsettings.json` to detect whether PostgreSQL or SQLite is requested:

```csharp
var provider = configuration["Database:Provider"] ?? "Sqlite";
var connectionString = configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=horeca_mastery.db";

services.AddDbContextFactory<HoReCaDbContext>(options =>
{
    if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            npgsqlOptions.MigrationsAssembly(typeof(HoReCaDbContext).Assembly.FullName);
        });
    }
    else
    {
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.MigrationsAssembly(typeof(HoReCaDbContext).Assembly.FullName);
        });
    }
});
```

---

## 4. Automatic Database Creation & Seeding

At startup, we ensure the database is initialized and mock data is inserted:

```csharp
public static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HoReCaDbContext>>();
    await using var context = await factory.CreateDbContextAsync();

    // Ensure database exists
    await context.Database.EnsureCreatedAsync();

    // Check if seeding is needed
    if (!await context.Tables.AnyAsync())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAllAsync(context);
    }
}
```

---

## 5. Performance Tips for Desktop EF Core

1. **`AsNoTracking()`**: Always use `AsNoTracking()` for read operations (e.g. populating DataGridViews, ComboBoxes, or reports). This bypasses the EF Core change tracker, slashing memory overhead.
2. **Short Lifetimes**: Keep `DbContext` lifetimes as brief as possible. Do not store a `DbContext` in a Form field.
3. **SQLite Foreign Keys**: In SQLite, ensure foreign keys are enabled by adding `Foreign Keys=True` to the connection string if relational deletion cascading is required.
