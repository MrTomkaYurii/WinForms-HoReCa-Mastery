using System.IO;
using System.Windows.Forms;
using HoReCa.Application.Interfaces;
using HoReCa.Infrastructure.Persistence;
using HoReCa.Infrastructure.Services;
using HoReCa.WinForms.Diagnostics;
using HoReCa.WinForms.Forms;
using HoReCa.WinForms.Printing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HoReCa.WinForms;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), "UnhandledException: " + e.ExceptionObject?.ToString()); } catch { }
        };
        System.Windows.Forms.Application.ThreadException += (s, e) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), "ThreadException: " + e.Exception?.ToString()); } catch { }
        };

        ApplicationConfiguration.Initialize();

        try
        {
            // Build Generic Host
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .UseSerilog((context, services, configuration) =>
                {
                    configuration
                        .MinimumLevel.Information()
                        .WriteTo.Console()
                        .WriteTo.File("logs/horeca-.log", rollingInterval: RollingInterval.Day)
                        .WriteTo.Sink(new SerilogRichTextBoxSink());
                })
                .ConfigureServices((context, services) =>
                {
                    var providerSetting = context.Configuration["Database:Provider"] ?? "Sqlite";
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=horeca_local.db";
                    var pgConnection = context.Configuration.GetConnectionString("PostgreSql");

                    bool usePostgreSql = false;
                    if (providerSetting.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pgConnection))
                    {
                        try
                        {
                            var npgBuilder = new Npgsql.NpgsqlConnectionStringBuilder(pgConnection) { Timeout = 1 };
                            using var conn = new Npgsql.NpgsqlConnection(npgBuilder.ConnectionString);
                            conn.Open();
                            usePostgreSql = true;
                            Log.Information("Connected to PostgreSQL database cluster successfully.");
                        }
                        catch
                        {
                            usePostgreSql = false;
                            Log.Information("PostgreSQL unreachable. Using zero-config local SQLite.");
                        }
                    }

                    // Register DbContextFactory with SQLite or PostgreSQL support
                    services.AddDbContextFactory<HoReCaDbContext>(options =>
                    {
                        if (usePostgreSql)
                        {
                            options.UseNpgsql(pgConnection);
                        }
                        else
                        {
                            options.UseSqlite(connectionString);
                        }
                    });

                    // Business & Printing Services
                    services.AddSingleton<IAuthService, AuthService>();
                    services.AddScoped<ITablePosService, TablePosService>();
                    services.AddScoped<IRoomBookingService, RoomBookingService>();
                    services.AddScoped<IKitchenService, KitchenService>();
                    services.AddScoped<IInventoryService, InventoryService>();
                    services.AddScoped<IAnalyticsService, AnalyticsService>();
                    services.AddSingleton<IReceiptPrintService, ReceiptPrinterEngine>();

                    // Forms
                    services.AddTransient<WelcomeStartForm>();
                    services.AddTransient<LoginForm>();
                    services.AddTransient<MainShellForm>();
                    services.AddTransient<PosTerminalForm>();
                    services.AddTransient<HotelFrontDeskForm>();
                    services.AddTransient<KitchenDisplayForm>();
                    services.AddTransient<InventoryForm>();
                    services.AddTransient<AnalyticsForm>();
                    services.AddTransient<LogsViewerForm>();
                })
                .Build();

            // Seed initial data if needed
            using (var scope = host.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HoReCaDbContext>>();
                using var db = dbFactory.CreateDbContext();
                DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
            }

            Log.Information("LuxeResto HoReCa Suite successfully started on .NET 10!");

            if (args.Contains("--test-run"))
            {
                Log.Information("Test run completed successfully.");
                return;
            }

            Log.Information("Resolving MainShellForm from DI...");
            var mainForm = host.Services.GetRequiredService<MainShellForm>();
            Log.Information("MainShellForm resolved successfully. Launching Application.Run(mainForm)...");
            System.Windows.Forms.Application.Run(mainForm);
            Log.Information("Application.Run(mainForm) has terminated cleanly.");
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), "Catch in Main: " + ex.ToString()); } catch { }
            Log.Fatal(ex, "Fatal error in Main");
            throw;
        }
    }
}
