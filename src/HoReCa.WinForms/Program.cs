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
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Build Generic Host
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.Information()
                    .WriteTo.File("logs/horeca-.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Sink(new SerilogRichTextBoxSink());
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                var sqliteFallback = context.Configuration.GetConnectionString("SqliteFallback") ?? "Data Source=horeca_local.db";

                // Register DbContextFactory with SQLite or PostgreSQL support
                services.AddDbContextFactory<HoReCaDbContext>(options =>
                {
                    // By default in dev/offline mode use SQLite for instant execution, or PostgreSQL if configured
                    if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Host="))
                    {
                        try
                        {
                            options.UseNpgsql(connectionString);
                        }
                        catch
                        {
                            options.UseSqlite(sqliteFallback);
                        }
                    }
                    else
                    {
                        options.UseSqlite(sqliteFallback);
                    }
                });

                // Business & Printing Services
                services.AddScoped<ITablePosService, TablePosService>();
                services.AddScoped<IRoomBookingService, RoomBookingService>();
                services.AddScoped<IKitchenService, KitchenService>();
                services.AddScoped<IInventoryService, InventoryService>();
                services.AddScoped<IAnalyticsService, AnalyticsService>();
                services.AddSingleton<IReceiptPrintService, ReceiptPrinterEngine>();

                // Forms
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

        var mainForm = host.Services.GetRequiredService<MainShellForm>();
        System.Windows.Forms.Application.Run(mainForm);
    }
}
