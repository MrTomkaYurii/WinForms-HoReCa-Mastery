using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(HoReCaDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Users & Staff accounts (16 realistic staff members)
        if (!await context.Users.AnyAsync())
        {
            var users = new List<UserAccount>
            {
                // Management & Administration
                new()
                {
                    Username = "admin",
                    FullName = "Олександр Коваленко (Керуючий / General Manager)",
                    PasswordHash = PasswordHelper.HashPassword("admin123"),
                    PinCode = "1111",
                    Role = UserRole.Administrator,
                    IsActive = true
                },
                new()
                {
                    Username = "olena.m",
                    FullName = "Олена Мельничук (Фінансовий директор / Заступник)",
                    PasswordHash = PasswordHelper.HashPassword("admin123"),
                    PinCode = "1234",
                    Role = UserRole.Administrator,
                    IsActive = true
                },

                // Waiters / Front-of-House
                new()
                {
                    Username = "waiter",
                    FullName = "Максим Іванов (Старший офіціант ресторану)",
                    PasswordHash = PasswordHelper.HashPassword("waiter123"),
                    PinCode = "2222",
                    Role = UserRole.Waiter,
                    IsActive = true
                },
                new()
                {
                    Username = "a.petrenko",
                    FullName = "Анастасія Петренко (Офіціант основної зали)",
                    PasswordHash = PasswordHelper.HashPassword("waiter123"),
                    PinCode = "2121",
                    Role = UserRole.Waiter,
                    IsActive = true
                },
                new()
                {
                    Username = "d.koval",
                    FullName = "Данило Коваль (Офіціант літньої тераси)",
                    PasswordHash = PasswordHelper.HashPassword("waiter123"),
                    PinCode = "2323",
                    Role = UserRole.Waiter,
                    IsActive = true
                },
                new()
                {
                    Username = "v.shevchuk",
                    FullName = "Вікторія Шевчук (Офіціант VIP-зали)",
                    PasswordHash = PasswordHelper.HashPassword("waiter123"),
                    PinCode = "2424",
                    Role = UserRole.Waiter,
                    IsActive = true
                },
                new()
                {
                    Username = "y.moroz",
                    FullName = "Ярослав Мороз (Офіціант-раннер)",
                    PasswordHash = PasswordHelper.HashPassword("waiter123"),
                    PinCode = "2525",
                    Role = UserRole.Waiter,
                    IsActive = true
                },

                // Kitchen Brigade (Chefs & Cooks)
                new()
                {
                    Username = "chef",
                    FullName = "Тарас Шевченко (Бренд-шеф ресторану)",
                    PasswordHash = PasswordHelper.HashPassword("chef123"),
                    PinCode = "4444",
                    Role = UserRole.Chef,
                    IsActive = true
                },
                new()
                {
                    Username = "s.bondar",
                    FullName = "Сергій Бондаренко (Су-шеф гарячого цеху / Grill)",
                    PasswordHash = PasswordHelper.HashPassword("chef123"),
                    PinCode = "4141",
                    Role = UserRole.Chef,
                    IsActive = true
                },
                new()
                {
                    Username = "k.tkachenko",
                    FullName = "Катерина Ткаченко (Кухар холодного цеху / Салати)",
                    PasswordHash = PasswordHelper.HashPassword("chef123"),
                    PinCode = "4242",
                    Role = UserRole.Chef,
                    IsActive = true
                },
                new()
                {
                    Username = "i.melnyk",
                    FullName = "Ігор Мельник (Піцайоло / Пекар)",
                    PasswordHash = PasswordHelper.HashPassword("chef123"),
                    PinCode = "4343",
                    Role = UserRole.Chef,
                    IsActive = true
                },
                new()
                {
                    Username = "o.kravets",
                    FullName = "Оксана Кравець (Шеф-кондитер)",
                    PasswordHash = PasswordHelper.HashPassword("chef123"),
                    PinCode = "4545",
                    Role = UserRole.Chef,
                    IsActive = true
                },

                // Baristas & Bartenders
                new()
                {
                    Username = "barista",
                    FullName = "Назар Савченко (Старший бариста / Шеф-бармен)",
                    PasswordHash = PasswordHelper.HashPassword("bar123"),
                    PinCode = "6666",
                    Role = UserRole.Barista,
                    IsActive = true
                },
                new()
                {
                    Username = "d.boyko",
                    FullName = "Діана Бойко (Бариста кав'ярні)",
                    PasswordHash = PasswordHelper.HashPassword("bar123"),
                    PinCode = "6161",
                    Role = UserRole.Barista,
                    IsActive = true
                },

                // Hotel Reception
                new()
                {
                    Username = "reception",
                    FullName = "Ірина Мельник (Старший адміністратор готелю)",
                    PasswordHash = PasswordHelper.HashPassword("hotel123"),
                    PinCode = "3333",
                    Role = UserRole.Receptionist,
                    IsActive = true
                },
                new()
                {
                    Username = "m.romanenko",
                    FullName = "Михайло Романенко (Нічний портьє)",
                    PasswordHash = PasswordHelper.HashPassword("hotel123"),
                    PinCode = "3131",
                    Role = UserRole.Receptionist,
                    IsActive = true
                },

                // Warehouse & Procurement
                new()
                {
                    Username = "store",
                    FullName = "Василь Бондар (Керівник складського обліку & логістики)",
                    PasswordHash = PasswordHelper.HashPassword("store123"),
                    PinCode = "5555",
                    Role = UserRole.Storekeeper,
                    IsActive = true
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        // Ensure all existing menu items have ImageUrls configured
        var itemsWithoutImages = await context.MenuItems.Where(m => m.ImageUrl == null).ToListAsync();
        if (itemsWithoutImages.Count > 0)
        {
            for (int i = 0; i < itemsWithoutImages.Count; i++)
            {
                itemsWithoutImages[i].ImageUrl = $"Assets/Images/Dishes/dish_{((itemsWithoutImages[i].Id - 1) % 42) + 1}.jpg";
            }
            await context.SaveChangesAsync();
        }

        // If zones exist, DB was already populated
        if (await context.TableZones.AnyAsync())
        {
            return;
        }

        // 2. Suppliers (6 key HoReCa distributors)
        var supMeat = new Supplier
        {
            Name = "ТОВ «М'ясний Дім Галичина»",
            ContactPerson = "Олег Дмитрович Сидоренко",
            Phone = "+38 (067) 555-12-34",
            Email = "orders@galychyna-meat.ua",
            Address = "м. Львів, вул. Промислова, 42",
            Category = "М'ясо, птиця та витримана яловичина",
            Edrpou = "38491024"
        };
        var supFish = new Supplier
        {
            Name = "ТОВ «Нордік Сіфуд Україна»",
            ContactPerson = "Ганна Сергіївна Вовк",
            Phone = "+38 (050) 444-98-76",
            Email = "supply@nordicseafood.com.ua",
            Address = "м. Одеса, вул. Приморська, 15",
            Category = "Охолоджена риба та морепродукти",
            Edrpou = "41982736"
        };
        var supFarm = new Supplier
        {
            Name = "ФГ «ЕкоФерма Поділля»",
            ContactPerson = "Ярослав Васильович Кравчук",
            Phone = "+38 (098) 777-33-22",
            Email = "agro@podillia-farm.ua",
            Address = "м. Вінниця, вул. Хмельницьке шосе, 88",
            Category = "Свіжі овочі, фрукти, зелень та яйця",
            Edrpou = "33219088"
        };
        var supItal = new Supplier
        {
            Name = "ТД «Італійські Делікатеси La Bella»",
            ContactPerson = "Марко Россі / Наталія Дудка",
            Phone = "+38 (044) 222-77-88",
            Email = "horeca@labella.ua",
            Address = "м. Київ, вул. Васильківська, 14",
            Category = "Сири, прошуто, трюфелі, паста, оливкова олія",
            Edrpou = "39948215"
        };
        var supCoffee = new Supplier
        {
            Name = "ТОВ «Specialty Coffee Roasters Київ»",
            ContactPerson = "Артем Левицький",
            Phone = "+38 (063) 111-99-00",
            Email = "roastery@specialtycoffee.kiev.ua",
            Address = "м. Київ, вул. Подоляночка, 6",
            Category = "Свіжообсмажена зернова кава, чай, сиропи",
            Edrpou = "43029184"
        };
        var supDairy = new Supplier
        {
            Name = "АТ «Молочний Альянс HoReCa»",
            ContactPerson = "Світлана Миколаївна Бойко",
            Phone = "+38 (0800) 50-20-10",
            Email = "b2b@milky-horeca.com.ua",
            Address = "м. Київ, вул. Євгена Коновальця, 36",
            Category = "Молоко Barista, вершки 33%, масло вершкове, моцарела",
            Edrpou = "32984112"
        };

        context.Suppliers.AddRange(supMeat, supFish, supFarm, supItal, supCoffee, supDairy);
        await context.SaveChangesAsync();

        // 3. Inventory Items (38 comprehensive ingredients)
        var invList = new List<InventoryItem>
        {
            // Meats & Poultry (Supplier: supMeat)
            new() { Name = "Яловича вирізка преміум (Tenderloin)", SKU = "INV-BEEF-01", Unit = "кг", CurrentStock = 18.5m, MinimumStock = 6.0m, CostPerUnit = 490m, SupplierId = supMeat.Id },
            new() { Name = "Стейк Рібай зернова витримка", SKU = "INV-BEEF-02", Unit = "кг", CurrentStock = 12.0m, MinimumStock = 5.0m, CostPerUnit = 580m, SupplierId = supMeat.Id },
            new() { Name = "Куряче філе фермерське", SKU = "INV-CHIK-01", Unit = "кг", CurrentStock = 25.0m, MinimumStock = 8.0m, CostPerUnit = 165m, SupplierId = supMeat.Id },
            new() { Name = "Качине філе Magret", SKU = "INV-DUCK-01", Unit = "кг", CurrentStock = 8.4m, MinimumStock = 4.0m, CostPerUnit = 380m, SupplierId = supMeat.Id },
            new() { Name = "Свинячі реберця BBQ", SKU = "INV-PORK-01", Unit = "кг", CurrentStock = 14.0m, MinimumStock = 5.0m, CostPerUnit = 220m, SupplierId = supMeat.Id },

            // Fish & Seafood (Supplier: supFish)
            new() { Name = "Лосось норвезький філе Trim E", SKU = "INV-FISH-01", Unit = "кг", CurrentStock = 9.5m, MinimumStock = 5.0m, CostPerUnit = 640m, SupplierId = supFish.Id },
            new() { Name = "Тигрові креветки 16/20 очищені", SKU = "INV-SHRM-01", Unit = "кг", CurrentStock = 11.0m, MinimumStock = 4.0m, CostPerUnit = 520m, SupplierId = supFish.Id },
            new() { Name = "Тунець Bluefin стейк", SKU = "INV-TUNA-01", Unit = "кг", CurrentStock = 6.0m, MinimumStock = 3.0m, CostPerUnit = 720m, SupplierId = supFish.Id },

            // Italian Specialties & Cheeses (Supplier: supItal)
            new() { Name = "Сир Пармезан Parmigiano Reggiano 24м", SKU = "INV-PARM-01", Unit = "кг", CurrentStock = 6.5m, MinimumStock = 3.0m, CostPerUnit = 790m, SupplierId = supItal.Id },
            new() { Name = "Сир Моцарела Fior di Latte", SKU = "INV-MOZZ-01", Unit = "кг", CurrentStock = 22.0m, MinimumStock = 10.0m, CostPerUnit = 245m, SupplierId = supItal.Id },
            new() { Name = "Сир Страчателла кремова", SKU = "INV-STRA-01", Unit = "кг", CurrentStock = 5.0m, MinimumStock = 3.0m, CostPerUnit = 380m, SupplierId = supItal.Id },
            new() { Name = "Прошуто ді Парма 18 міс.", SKU = "INV-PROS-01", Unit = "кг", CurrentStock = 7.2m, MinimumStock = 3.0m, CostPerUnit = 690m, SupplierId = supItal.Id },
            new() { Name = "Гуанчале (в'ялена свиняча щока)", SKU = "INV-GUAN-01", Unit = "кг", CurrentStock = 4.8m, MinimumStock = 2.0m, CostPerUnit = 540m, SupplierId = supItal.Id },
            new() { Name = "Паста спагеті De Cecco Bronze", SKU = "INV-PAST-01", Unit = "кг", CurrentStock = 30.0m, MinimumStock = 10.0m, CostPerUnit = 115m, SupplierId = supItal.Id },
            new() { Name = "Борошно пшеничне Caputo Tipo 00", SKU = "INV-FLOR-01", Unit = "кг", CurrentStock = 45.0m, MinimumStock = 15.0m, CostPerUnit = 65m, SupplierId = supItal.Id },
            new() { Name = "Олія оливкова Extra Virgin DOP", SKU = "INV-OIL-01", Unit = "л", CurrentStock = 3.2m, MinimumStock = 8.0m, CostPerUnit = 390m, SupplierId = supItal.Id }, // DEFICIT!
            new() { Name = "Трюфельна паста Tartufo Nero", SKU = "INV-TRUF-01", Unit = "кг", CurrentStock = 0.8m, MinimumStock = 2.0m, CostPerUnit = 1450m, SupplierId = supItal.Id }, // DEFICIT!

            // Vegetables & Greens (Supplier: supFarm)
            new() { Name = "Томати конкасе Mutti Pelati", SKU = "INV-TOM-01", Unit = "кг", CurrentStock = 5.0m, MinimumStock = 12.0m, CostPerUnit = 115m, SupplierId = supFarm.Id }, // DEFICIT!
            new() { Name = "Томати чері на гілці солодкі", SKU = "INV-TCHR-01", Unit = "кг", CurrentStock = 3.5m, MinimumStock = 6.0m, CostPerUnit = 140m, SupplierId = supFarm.Id }, // DEFICIT!
            new() { Name = "Мікс салатів Бебі-Ромен / Рукола", SKU = "INV-SALA-01", Unit = "кг", CurrentStock = 8.0m, MinimumStock = 4.0m, CostPerUnit = 190m, SupplierId = supFarm.Id },
            new() { Name = "Авокадо Hass стигле", SKU = "INV-AVOC-01", Unit = "кг", CurrentStock = 7.0m, MinimumStock = 3.0m, CostPerUnit = 280m, SupplierId = supFarm.Id },
            new() { Name = "Печериці свіжі відбірні", SKU = "INV-MUSH-01", Unit = "кг", CurrentStock = 12.0m, MinimumStock = 5.0m, CostPerUnit = 95m, SupplierId = supFarm.Id },
            new() { Name = "Картопля молода бебі", SKU = "INV-POTA-01", Unit = "кг", CurrentStock = 40.0m, MinimumStock = 15.0m, CostPerUnit = 28m, SupplierId = supFarm.Id },
            new() { Name = "Яйця фермерські С0", SKU = "INV-EGGS-01", Unit = "шт", CurrentStock = 180m, MinimumStock = 60m, CostPerUnit = 5.5m, SupplierId = supFarm.Id },
            new() { Name = "Лимони свіжі сицилійські", SKU = "INV-LEMN-01", Unit = "кг", CurrentStock = 10.0m, MinimumStock = 4.0m, CostPerUnit = 85m, SupplierId = supFarm.Id },

            // Dairy & Bakery (Supplier: supDairy)
            new() { Name = "Молоко Barista Pro 3.2%", SKU = "INV-MILK-01", Unit = "л", CurrentStock = 48.0m, MinimumStock = 20.0m, CostPerUnit = 49m, SupplierId = supDairy.Id },
            new() { Name = "Молоко рослинне Мигдалеве Alpro", SKU = "INV-AMILK-01", Unit = "л", CurrentStock = 15.0m, MinimumStock = 6.0m, CostPerUnit = 110m, SupplierId = supDairy.Id },
            new() { Name = "Вершки кулінарні 33%", SKU = "INV-CRME-01", Unit = "л", CurrentStock = 16.0m, MinimumStock = 6.0m, CostPerUnit = 175m, SupplierId = supDairy.Id },
            new() { Name = "Сир Маскарпоне 82%", SKU = "INV-MASC-01", Unit = "кг", CurrentStock = 8.0m, MinimumStock = 4.0m, CostPerUnit = 320m, SupplierId = supDairy.Id },
            new() { Name = "Масло вершкове 82.5% ГОСТ", SKU = "INV-BUTR-01", Unit = "кг", CurrentStock = 14.0m, MinimumStock = 5.0m, CostPerUnit = 290m, SupplierId = supDairy.Id },

            // Coffee & Bar (Supplier: supCoffee)
            new() { Name = "Кава в зернах Specialty Ethiopia Yirgacheffe", SKU = "INV-COFF-01", Unit = "кг", CurrentStock = 18.0m, MinimumStock = 8.0m, CostPerUnit = 680m, SupplierId = supCoffee.Id },
            new() { Name = "Какао бельгійський порошок", SKU = "INV-COCO-01", Unit = "кг", CurrentStock = 6.0m, MinimumStock = 2.0m, CostPerUnit = 320m, SupplierId = supCoffee.Id },
            new() { Name = "Чай листовий Трав'яний Альпійський", SKU = "INV-TEA-01", Unit = "кг", CurrentStock = 4.5m, MinimumStock = 2.0m, CostPerUnit = 480m, SupplierId = supCoffee.Id },
            new() { Name = "Печиво Савоярді дамські пальчики", SKU = "INV-SAVO-01", Unit = "кг", CurrentStock = 6.0m, MinimumStock = 3.0m, CostPerUnit = 210m, SupplierId = supCoffee.Id },
            new() { Name = "Шоколад чорний 70% Callebaut", SKU = "INV-CHOC-01", Unit = "кг", CurrentStock = 9.0m, MinimumStock = 4.0m, CostPerUnit = 560m, SupplierId = supCoffee.Id },
            new() { Name = "Сироп маракуя Monin", SKU = "INV-SYRP-01", Unit = "л", CurrentStock = 5.0m, MinimumStock = 2.0m, CostPerUnit = 290m, SupplierId = supCoffee.Id },
            new() { Name = "Апероль Aperitivo Barbieri 1л", SKU = "INV-APER-01", Unit = "л", CurrentStock = 8.0m, MinimumStock = 3.0m, CostPerUnit = 620m, SupplierId = supCoffee.Id },
            new() { Name = "Просеко DOC Spumante Brut", SKU = "INV-WINE-01", Unit = "бут", CurrentStock = 24.0m, MinimumStock = 10.0m, CostPerUnit = 320m, SupplierId = supCoffee.Id }
        };

        context.Inventory.AddRange(invList);
        await context.SaveChangesAsync();

        // 4. Restaurant Zones (3 refined zones)
        var zoneMain = new TableZone { Name = "Основний зал", Description = "Головний зал ресторану з панорамними вікнами та сценою", DisplayOrder = 1 };
        var zoneTerrace = new TableZone { Name = "Літня тераса", Description = "Відкрита лаунж-тераса з видом на парк", DisplayOrder = 2 };
        var zoneVip = new TableZone { Name = "VIP Лаундж", Description = "Приватна кімната з каміном для ділових вечерь", DisplayOrder = 3 };

        context.TableZones.AddRange(zoneMain, zoneTerrace, zoneVip);
        await context.SaveChangesAsync();

        // 5. Restaurant Tables with clean, non-overlapping default layout
        var tables = new List<RestaurantTable>
        {
            // Main Hall (Zone 1) - Neat grid
            new() { TableNumber = 1, ZoneId = zoneMain.Id, Capacity = 2, Shape = TableShape.Round, PositionX = 40, PositionY = 30, Width = 95, Height = 95, Status = TableStatus.Free },
            new() { TableNumber = 2, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 170, PositionY = 30, Width = 120, Height = 95, Status = TableStatus.Occupied },
            new() { TableNumber = 3, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 320, PositionY = 30, Width = 120, Height = 95, Status = TableStatus.Free },
            new() { TableNumber = 4, ZoneId = zoneMain.Id, Capacity = 6, Shape = TableShape.Rectangle, PositionX = 470, PositionY = 30, Width = 140, Height = 95, Status = TableStatus.Free },
            new() { TableNumber = 5, ZoneId = zoneMain.Id, Capacity = 2, Shape = TableShape.Square, PositionX = 40, PositionY = 160, Width = 95, Height = 95, Status = TableStatus.BillRequested },
            new() { TableNumber = 6, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 170, PositionY = 160, Width = 120, Height = 95, Status = TableStatus.Free },
            new() { TableNumber = 7, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 320, PositionY = 160, Width = 120, Height = 95, Status = TableStatus.Free },
            new() { TableNumber = 8, ZoneId = zoneMain.Id, Capacity = 8, Shape = TableShape.Rectangle, PositionX = 470, PositionY = 160, Width = 150, Height = 95, Status = TableStatus.Free },

            // Terrace (Zone 2)
            new() { TableNumber = 11, ZoneId = zoneTerrace.Id, Capacity = 2, Shape = TableShape.Round, PositionX = 50, PositionY = 40, Width = 90, Height = 90, Status = TableStatus.Free },
            new() { TableNumber = 12, ZoneId = zoneTerrace.Id, Capacity = 4, Shape = TableShape.Square, PositionX = 180, PositionY = 40, Width = 100, Height = 90, Status = TableStatus.Occupied },
            new() { TableNumber = 13, ZoneId = zoneTerrace.Id, Capacity = 4, Shape = TableShape.Square, PositionX = 320, PositionY = 40, Width = 100, Height = 90, Status = TableStatus.Free },
            new() { TableNumber = 14, ZoneId = zoneTerrace.Id, Capacity = 6, Shape = TableShape.Rectangle, PositionX = 460, PositionY = 40, Width = 130, Height = 90, Status = TableStatus.Free },
            new() { TableNumber = 15, ZoneId = zoneTerrace.Id, Capacity = 4, Shape = TableShape.Square, PositionX = 180, PositionY = 170, Width = 100, Height = 90, Status = TableStatus.Free },
            new() { TableNumber = 16, ZoneId = zoneTerrace.Id, Capacity = 6, Shape = TableShape.Rectangle, PositionX = 320, PositionY = 170, Width = 130, Height = 90, Status = TableStatus.Free },

            // VIP Lounge (Zone 3)
            new() { TableNumber = 21, ZoneId = zoneVip.Id, Capacity = 10, Shape = TableShape.Rectangle, PositionX = 80, PositionY = 60, Width = 220, Height = 120, Status = TableStatus.Reserved },
            new() { TableNumber = 22, ZoneId = zoneVip.Id, Capacity = 8, Shape = TableShape.Round, PositionX = 350, PositionY = 60, Width = 140, Height = 140, Status = TableStatus.Free }
        };

        context.Tables.AddRange(tables);

        // 6. Hotel Rooms (12 rooms across 3 floors)
        var rooms = new List<Room>
        {
            // Floor 1 - Standard & Comfort
            new() { RoomNumber = "101", Floor = 1, Type = RoomType.Standard, DailyRate = 1900m, Capacity = 2, Status = RoomStatus.Occupied, Description = "Затишний стандарт з двоспальним ліжком King-size та душовою кабіною" },
            new() { RoomNumber = "102", Floor = 1, Type = RoomType.Standard, DailyRate = 1900m, Capacity = 2, Status = RoomStatus.Available, Description = "Стандартний номер з двома окремими ліжками Twin" },
            new() { RoomNumber = "103", Floor = 1, Type = RoomType.Comfort, DailyRate = 2500m, Capacity = 2, Status = RoomStatus.Reserved, Description = "Покращений номер із комфортною лаунж-зоною та кавомашиною" },
            new() { RoomNumber = "104", Floor = 1, Type = RoomType.Comfort, DailyRate = 2500m, Capacity = 3, Status = RoomStatus.Available, Description = "Просторий тримісний номер з виходом у внутрішній дворик" },

            // Floor 2 - Comfort & Deluxe
            new() { RoomNumber = "201", Floor = 2, Type = RoomType.Comfort, DailyRate = 2700m, Capacity = 2, Status = RoomStatus.Available, Description = "Комфорт з панорамним балконом та видом на парк" },
            new() { RoomNumber = "202", Floor = 2, Type = RoomType.Deluxe, DailyRate = 3900m, Capacity = 2, Status = RoomStatus.Occupied, Description = "Люкс із окремою спальнею, джакузі та дизайнерським ремонтом" },
            new() { RoomNumber = "203", Floor = 2, Type = RoomType.Deluxe, DailyRate = 3900m, Capacity = 2, Status = RoomStatus.Available, Description = "Люкс Делюкс з каміном та міні-баром" },
            new() { RoomNumber = "204", Floor = 2, Type = RoomType.Deluxe, DailyRate = 4200m, Capacity = 4, Status = RoomStatus.Available, Description = "Сімейний дворівневий люкс з дитячим куточком" },

            // Floor 3 - Executive Suites & Presidential
            new() { RoomNumber = "301", Floor = 3, Type = RoomType.Deluxe, DailyRate = 5800m, Capacity = 2, Status = RoomStatus.Reserved, Description = "Преміум апартаменти з терасою на даху" },
            new() { RoomNumber = "302", Floor = 3, Type = RoomType.Deluxe, DailyRate = 5800m, Capacity = 3, Status = RoomStatus.Available, Description = "Королівський сьют з мармуровою ванною кімнатою" },
            new() { RoomNumber = "303", Floor = 3, Type = RoomType.PresidentialSuite, DailyRate = 8900m, Capacity = 4, Status = RoomStatus.Occupied, Description = "Президентські апартаменти з конференц-залою та персональним дворецьким" },
            new() { RoomNumber = "304", Floor = 3, Type = RoomType.PresidentialSuite, DailyRate = 8900m, Capacity = 4, Status = RoomStatus.Available, Description = "Пентхаус з панорамним круговим оглядом 360°" }
        };

        context.Rooms.AddRange(rooms);

        // 7. Menu Categories (8 detailed categories)
        var catStarters = new MenuCategory { Name = "Холодні закуски & Антипасті", IconKey = "Salad", DisplayOrder = 1, RouteToKitchen = true };
        var catSoups = new MenuCategory { Name = "Гарячі закуски & Супи", IconKey = "Soup", DisplayOrder = 2, RouteToKitchen = true };
        var catMains = new MenuCategory { Name = "Основні страви & Гриль", IconKey = "Meat", DisplayOrder = 3, RouteToKitchen = true };
        var catPizza = new MenuCategory { Name = "Автентична піца", IconKey = "Pizza", DisplayOrder = 4, RouteToKitchen = true };
        var catPasta = new MenuCategory { Name = "Паста & Різотто", IconKey = "Pasta", DisplayOrder = 5, RouteToKitchen = true };
        var catDesserts = new MenuCategory { Name = "Десерти від шефа", IconKey = "Cake", DisplayOrder = 6, RouteToKitchen = true };
        var catCoffee = new MenuCategory { Name = "Кава & Чай", IconKey = "Coffee", DisplayOrder = 7, RouteToKitchen = false };
        var catBar = new MenuCategory { Name = "Бар & Коктейлі", IconKey = "Wine", DisplayOrder = 8, RouteToKitchen = false };

        context.MenuCategories.AddRange(catStarters, catSoups, catMains, catPizza, catPasta, catDesserts, catCoffee, catBar);
        await context.SaveChangesAsync();

        // 8. Menu Items (42 signature dishes)
        var menuItems = new List<MenuItem>
        {
            // Starters (6 items)
            new() { CategoryId = catStarters.Id, Name = "Салат Цезар з фермерською куркою", Price = 285m, PreparationMinutes = 10, Description = "Мікс листя салату, курка су-від, томати чері, соус Цезар, крутони, пармезан 24м" },
            new() { CategoryId = catStarters.Id, Name = "Салат Цезар з тигровими креветками", Price = 360m, PreparationMinutes = 12, Description = "Обсмажені на грилі креветки, салат ромен, перепелині яйця, соус Цезар, пармезан" },
            new() { CategoryId = catStarters.Id, Name = "Тартар із телятини з каперсами та трюфелем", Price = 370m, PreparationMinutes = 12, Description = "Витримана вирізка яловичини, трюфельна паста, перепелиний жовток, хрумка бріош" },
            new() { CategoryId = catStarters.Id, Name = "Тартар із лосося та свіжого авокадо", Price = 390m, PreparationMinutes = 10, Description = "Охолоджений лосось Trim E, стигле авокадо Hass, оливкова олія EV, лаймовий дресинг" },
            new() { CategoryId = catStarters.Id, Name = "Брускети з томатами та кремовою страчателлою", Price = 220m, PreparationMinutes = 8, Description = "Чиабата на заквасці, страчателла, конкасе з солодких чері, базилік, песто" },
            new() { CategoryId = catStarters.Id, Name = "Антипасто Плато делікатесів", Price = 480m, PreparationMinutes = 10, Description = "Прошуто ді Парма, сир пармезан, оливки каламата, в'ялені томати, грісіні" },

            // Soups (4 items)
            new() { CategoryId = catSoups.Id, Name = "Борщ український з копченими реберцями", Price = 240m, PreparationMinutes = 12, Description = "Традиційний борщ за авторським рецептом, пампушки з часником, сметана та сало" },
            new() { CategoryId = catSoups.Id, Name = "Крем-суп із білих грибів з трюфелем", Price = 265m, PreparationMinutes = 15, Description = "Карпатські лісові гриби, вершки 33%, трюфельна олія, хрусткі крутони" },
            new() { CategoryId = catSoups.Id, Name = "Том Ям з тигровими креветками", Price = 390m, PreparationMinutes = 16, Description = "Автентичний тайський бульйон, креветки, печериці, томати чері, кінза, рис басматі" },
            new() { CategoryId = catSoups.Id, Name = "Курячий бульйон з домашньою локшиною", Price = 175m, PreparationMinutes = 8, Description = "Прозорий наваристий бульйон, перепелине яйце, фрикадельки з курки, зелень" },

            // Mains & Grill (7 items)
            new() { CategoryId = catMains.Id, Name = "Стейк Рібай Prime з перцевим соусом", Price = 690m, PreparationMinutes = 22, Description = "Яловичина зернової витримки 300г, запечені овочі гриль, зелене масло з травами" },
            new() { CategoryId = catMains.Id, Name = "Стейк Філе Міньйон з трюфельним пюре", Price = 740m, PreparationMinutes = 20, Description = "Ніжна центральна вирізка, вершкове пюре з картоплі, деміглас з чорним трюфелем" },
            new() { CategoryId = catMains.Id, Name = "Філе норвезького лосося на подушці зі спаржі", Price = 540m, PreparationMinutes = 18, Description = "Лосось на грилі, ніжна спаржа, голландський соус, мікрозелень горошку" },
            new() { CategoryId = catMains.Id, Name = "Стейк тунця у кунжуті з гуакамоле", Price = 580m, PreparationMinutes = 15, Description = "Обсмажений тунець Bluefin (Rare), крем з авокадо Hass, соус понзу" },
            new() { CategoryId = catMains.Id, Name = "Качине філе Magret з ягідним демігласом", Price = 470m, PreparationMinutes = 20, Description = "Ніжна качка з хрусткою скоринкою, пюре з селери, карамелізоване яблуко" },
            new() { CategoryId = catMains.Id, Name = "Свинячі реберця BBQ томлені у печі", Price = 420m, PreparationMinutes = 20, Description = "Глазуровані фірмовим соусом BBQ реберця, молода бебі-картопля фрі, маринована цибуля" },
            new() { CategoryId = catMains.Id, Name = "Курча тапака з часниково-пряним соусом", Price = 380m, PreparationMinutes = 25, Description = "Золотисте курча запечене під гнітом, свіжа кінза, лаваш, соус ткемалі" },

            // Pizza (5 items)
            new() { CategoryId = catPizza.Id, Name = "Піца Маргарита Класична", Price = 250m, PreparationMinutes = 14, Description = "Томатний соус Mutti, моцарела Fior di Latte, оливкова олія EV, свіжий базилік" },
            new() { CategoryId = catPizza.Id, Name = "Піца 4 Сири з трюфельним медом", Price = 340m, PreparationMinutes = 15, Description = "Моцарела, горгонзола піканте, пармезан 24м, брі де мо, волоський горіх, мед" },
            new() { CategoryId = catPizza.Id, Name = "Піца Прошуто Котто & Печериці", Price = 330m, PreparationMinutes = 15, Description = "Томатний соус, моцарела, італійська шинка котто, свіжі печериці, орегано" },
            new() { CategoryId = catPizza.Id, Name = "Піца Дьявола з гострою салямі", Price = 320m, PreparationMinutes = 15, Description = "Томатний соус, моцарела, салямі вентричіна, перець чилі, базилік" },
            new() { CategoryId = catPizza.Id, Name = "Піца з морепродуктами Frutti di Mare", Price = 420m, PreparationMinutes = 16, Description = "Білий вершковий соус, тигрові креветки, лосось, моцарела, рукола, лимон" },

            // Pasta & Risotto (5 items)
            new() { CategoryId = catPasta.Id, Name = "Паста Карбонара за римським рецептом", Price = 295m, PreparationMinutes = 14, Description = "Спагеті De Cecco, хрустке гуанчале, фермерські яєчні жовтки, сир пекоріно романо" },
            new() { CategoryId = catPasta.Id, Name = "Тальятеле з білими грибами та трюфелем", Price = 340m, PreparationMinutes = 15, Description = "Домашня яєчна паста, карпатські білі гриби, вершки 33%, пармезан, трюфельна паста" },
            new() { CategoryId = catPasta.Id, Name = "Фетучіні з лососем та шпинатом у вершках", Price = 360m, PreparationMinutes = 15, Description = "Шматочки свіжого лосося, шпинат, вершковий соус, томати конкасе, пармезан" },
            new() { CategoryId = catPasta.Id, Name = "Спагеті з тигровими креветками та чері", Price = 380m, PreparationMinutes = 15, Description = "Тигрові креветки, біле вино, томати чері, часник, базилік, оливкова олія" },
            new() { CategoryId = catPasta.Id, Name = "Різотто з морепродуктами та шафраном", Price = 410m, PreparationMinutes = 18, Description = "Рис Карнаролі, шафран, креветки, шматочки риби, вершкове масло, пармезан" },

            // Desserts (5 items)
            new() { CategoryId = catDesserts.Id, Name = "Тірамісу автентичний з маскарпоне", Price = 195m, PreparationMinutes = 5, Description = "Савоярді, крем з маскарпоне та жовтків, еспресо Yirgacheffe, марсала, какао" },
            new() { CategoryId = catDesserts.Id, Name = "Шоколадний фондан з ванільним gelato", Price = 230m, PreparationMinutes = 12, Description = "Бельгійський темний шоколад Callebaut 70%, рідка серцевина, мадагаскарська ваніль" },
            new() { CategoryId = catDesserts.Id, Name = "Баскський палений чизкейк Сан-Себастьян", Price = 210m, PreparationMinutes = 5, Description = "Ніжна карамелізована скоринка, кремова текстура, ягідне пюре з малини" },
            new() { CategoryId = catDesserts.Id, Name = "Панакота з кулі з маракуї", Price = 180m, PreparationMinutes = 5, Description = "Вершки 33%, натуральна ваніль, екзотичний соус із свіжої маракуї" },
            new() { CategoryId = catDesserts.Id, Name = "Сорбет тріо (Лимон, Манго, Малина)", Price = 160m, PreparationMinutes = 4, Description = "Освіжаючий натуральний фруктовий сорбет без цукру та барвників" },

            // Coffee & Tea (5 items)
            new() { CategoryId = catCoffee.Id, Name = "Еспресо Specialty 100% Arabica", Price = 65m, PreparationMinutes = 3, Description = "Подвійний шот Ефіопія Yirgacheffe, ноти бергамоту та ягід" },
            new() { CategoryId = catCoffee.Id, Name = "Капучино класичний на молоці Barista", Price = 85m, PreparationMinutes = 4, Description = "Подвійний еспресо, оксамитова молочна мікропінка 3.2%" },
            new() { CategoryId = catCoffee.Id, Name = "Лате на мигдалевому молоці Alpro", Price = 115m, PreparationMinutes = 4, Description = "Ароматний шот кави, збите рослинне мигдалеве молоко, без лактози" },
            new() { CategoryId = catCoffee.Id, Name = "Флет Вайт подвійної міцності", Price = 95m, PreparationMinutes = 4, Description = "Подвійний рістрето, тонкий шар оксамитового теплого молока" },
            new() { CategoryId = catCoffee.Id, Name = "Чай Карпатський з медом та обліпихою 600мл", Price = 140m, PreparationMinutes = 6, Description = "Альпійські трави, свіжа гірська обліпиха, лимон, натуральний мед" },

            // Bar & Cocktails (5 items)
            new() { CategoryId = catBar.Id, Name = "Коктейль Aperol Spritz Венеція", Price = 260m, PreparationMinutes = 4, Description = "Aperol Barbieri, італійське просеко DOC, содова вода, скибочка апельсина, лід" },
            new() { CategoryId = catBar.Id, Name = "Коктейль Negroni Класичний", Price = 280m, PreparationMinutes = 4, Description = "London Dry Gin, Campari, солодкий червоний вермут, цедра апельсина" },
            new() { CategoryId = catBar.Id, Name = "Келих Просеко DOC Spumante 150мл", Price = 190m, PreparationMinutes = 2, Description = "Венето, Італія, сухе ігристе біле вино з нотами зеленого яблука" },
            new() { CategoryId = catBar.Id, Name = "Келих К'янті Класіко DOCG 150мл", Price = 240m, PreparationMinutes = 2, Description = "Тоскана, Італія, витримане сухе червоне вино, санджовезе" },
            new() { CategoryId = catBar.Id, Name = "Лимонад Маракуя-М'ята крафтовий 500мл", Price = 150m, PreparationMinutes = 4, Description = "Пюре з натуральної маракуї, свіжа м'ята, лайм, тростинний сироп, газована вода" }
        };

        for (int i = 0; i < menuItems.Count; i++)
        {
            menuItems[i].ImageUrl = $"Assets/Images/Dishes/dish_{i + 1}.jpg";
        }

        context.MenuItems.AddRange(menuItems);
        await context.SaveChangesAsync();

        // 9. Recipe Tech Cards (Mapping dishes to exact inventory quantities)
        // Helper dictionary to lookup inventory by SKU
        var invBySku = invList.ToDictionary(i => i.SKU);

        var recipes = new List<RecipeItem>
        {
            // Caesar with Chicken: 0.15kg Chicken, 0.04kg Parmesan, 0.08kg Salad, 0.05kg Tomato
            new() { MenuItemId = menuItems[0].Id, InventoryItemId = invBySku["INV-CHIK-01"].Id, QuantityNeeded = 0.150m },
            new() { MenuItemId = menuItems[0].Id, InventoryItemId = invBySku["INV-PARM-01"].Id, QuantityNeeded = 0.040m },
            new() { MenuItemId = menuItems[0].Id, InventoryItemId = invBySku["INV-SALA-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[0].Id, InventoryItemId = invBySku["INV-TCHR-01"].Id, QuantityNeeded = 0.050m },

            // Caesar with Shrimps: 0.12kg Shrimps, 0.04kg Parmesan, 0.08kg Salad, 2 eggs
            new() { MenuItemId = menuItems[1].Id, InventoryItemId = invBySku["INV-SHRM-01"].Id, QuantityNeeded = 0.120m },
            new() { MenuItemId = menuItems[1].Id, InventoryItemId = invBySku["INV-PARM-01"].Id, QuantityNeeded = 0.040m },
            new() { MenuItemId = menuItems[1].Id, InventoryItemId = invBySku["INV-SALA-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[1].Id, InventoryItemId = invBySku["INV-EGGS-01"].Id, QuantityNeeded = 2m },

            // Beef Tartare: 0.18kg Beef Tenderloin, 0.01kg Truffle Paste, 0.015L Olive Oil, 1 egg
            new() { MenuItemId = menuItems[2].Id, InventoryItemId = invBySku["INV-BEEF-01"].Id, QuantityNeeded = 0.180m },
            new() { MenuItemId = menuItems[2].Id, InventoryItemId = invBySku["INV-TRUF-01"].Id, QuantityNeeded = 0.010m },
            new() { MenuItemId = menuItems[2].Id, InventoryItemId = invBySku["INV-OIL-01"].Id, QuantityNeeded = 0.015m },
            new() { MenuItemId = menuItems[2].Id, InventoryItemId = invBySku["INV-EGGS-01"].Id, QuantityNeeded = 1m },

            // Salmon & Avocado Tartare: 0.15kg Salmon, 0.10kg Avocado, 0.02kg Lemon, 0.015L Olive Oil
            new() { MenuItemId = menuItems[3].Id, InventoryItemId = invBySku["INV-FISH-01"].Id, QuantityNeeded = 0.150m },
            new() { MenuItemId = menuItems[3].Id, InventoryItemId = invBySku["INV-AVOC-01"].Id, QuantityNeeded = 0.100m },
            new() { MenuItemId = menuItems[3].Id, InventoryItemId = invBySku["INV-LEMN-01"].Id, QuantityNeeded = 0.020m },
            new() { MenuItemId = menuItems[3].Id, InventoryItemId = invBySku["INV-OIL-01"].Id, QuantityNeeded = 0.015m },

            // Bruschetta with Stracciatella: 0.10kg Stracciatella, 0.08kg Cherry Tomato, 0.015L Olive Oil
            new() { MenuItemId = menuItems[4].Id, InventoryItemId = invBySku["INV-STRA-01"].Id, QuantityNeeded = 0.100m },
            new() { MenuItemId = menuItems[4].Id, InventoryItemId = invBySku["INV-TCHR-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[4].Id, InventoryItemId = invBySku["INV-OIL-01"].Id, QuantityNeeded = 0.015m },

            // Antipasto: 0.08kg Prosciutto, 0.06kg Parmesan, 0.05kg Stracciatella
            new() { MenuItemId = menuItems[5].Id, InventoryItemId = invBySku["INV-PROS-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[5].Id, InventoryItemId = invBySku["INV-PARM-01"].Id, QuantityNeeded = 0.060m },
            new() { MenuItemId = menuItems[5].Id, InventoryItemId = invBySku["INV-STRA-01"].Id, QuantityNeeded = 0.050m },

            // Ribeye Prime: 0.35kg Ribeye, 0.03L Olive Oil, 0.03kg Butter
            new() { MenuItemId = menuItems[10].Id, InventoryItemId = invBySku["INV-BEEF-02"].Id, QuantityNeeded = 0.350m },
            new() { MenuItemId = menuItems[10].Id, InventoryItemId = invBySku["INV-OIL-01"].Id, QuantityNeeded = 0.030m },
            new() { MenuItemId = menuItems[10].Id, InventoryItemId = invBySku["INV-BUTR-01"].Id, QuantityNeeded = 0.030m },

            // Filet Mignon: 0.25kg Beef Tenderloin, 0.15kg Potato, 0.05L Cream, 0.01kg Truffle
            new() { MenuItemId = menuItems[11].Id, InventoryItemId = invBySku["INV-BEEF-01"].Id, QuantityNeeded = 0.250m },
            new() { MenuItemId = menuItems[11].Id, InventoryItemId = invBySku["INV-POTA-01"].Id, QuantityNeeded = 0.150m },
            new() { MenuItemId = menuItems[11].Id, InventoryItemId = invBySku["INV-CRME-01"].Id, QuantityNeeded = 0.050m },
            new() { MenuItemId = menuItems[11].Id, InventoryItemId = invBySku["INV-TRUF-01"].Id, QuantityNeeded = 0.010m },

            // Salmon Fillet: 0.22kg Salmon, 0.02kg Butter, 0.02kg Lemon
            new() { MenuItemId = menuItems[12].Id, InventoryItemId = invBySku["INV-FISH-01"].Id, QuantityNeeded = 0.220m },
            new() { MenuItemId = menuItems[12].Id, InventoryItemId = invBySku["INV-BUTR-01"].Id, QuantityNeeded = 0.020m },
            new() { MenuItemId = menuItems[12].Id, InventoryItemId = invBySku["INV-LEMN-01"].Id, QuantityNeeded = 0.020m },

            // Tuna Steak: 0.22kg Tuna, 0.12kg Avocado, 0.02kg Lemon
            new() { MenuItemId = menuItems[13].Id, InventoryItemId = invBySku["INV-TUNA-01"].Id, QuantityNeeded = 0.220m },
            new() { MenuItemId = menuItems[13].Id, InventoryItemId = invBySku["INV-AVOC-01"].Id, QuantityNeeded = 0.120m },
            new() { MenuItemId = menuItems[13].Id, InventoryItemId = invBySku["INV-LEMN-01"].Id, QuantityNeeded = 0.020m },

            // Duck Magret: 0.25kg Duck, 0.02kg Butter
            new() { MenuItemId = menuItems[14].Id, InventoryItemId = invBySku["INV-DUCK-01"].Id, QuantityNeeded = 0.250m },
            new() { MenuItemId = menuItems[14].Id, InventoryItemId = invBySku["INV-BUTR-01"].Id, QuantityNeeded = 0.020m },

            // BBQ Pork Ribs: 0.45kg Pork Ribs, 0.15kg Potato
            new() { MenuItemId = menuItems[15].Id, InventoryItemId = invBySku["INV-PORK-01"].Id, QuantityNeeded = 0.450m },
            new() { MenuItemId = menuItems[15].Id, InventoryItemId = invBySku["INV-POTA-01"].Id, QuantityNeeded = 0.150m },

            // Pizza Margherita: 0.22kg Flour, 0.12kg Mozzarella, 0.08kg Tomatoes, 0.01L Olive Oil
            new() { MenuItemId = menuItems[17].Id, InventoryItemId = invBySku["INV-FLOR-01"].Id, QuantityNeeded = 0.220m },
            new() { MenuItemId = menuItems[17].Id, InventoryItemId = invBySku["INV-MOZZ-01"].Id, QuantityNeeded = 0.120m },
            new() { MenuItemId = menuItems[17].Id, InventoryItemId = invBySku["INV-TOM-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[17].Id, InventoryItemId = invBySku["INV-OIL-01"].Id, QuantityNeeded = 0.010m },

            // Pizza 4 Cheese: 0.22kg Flour, 0.10kg Mozzarella, 0.05kg Parmesan, 0.04kg Stracciatella
            new() { MenuItemId = menuItems[18].Id, InventoryItemId = invBySku["INV-FLOR-01"].Id, QuantityNeeded = 0.220m },
            new() { MenuItemId = menuItems[18].Id, InventoryItemId = invBySku["INV-MOZZ-01"].Id, QuantityNeeded = 0.100m },
            new() { MenuItemId = menuItems[18].Id, InventoryItemId = invBySku["INV-PARM-01"].Id, QuantityNeeded = 0.050m },
            new() { MenuItemId = menuItems[18].Id, InventoryItemId = invBySku["INV-STRA-01"].Id, QuantityNeeded = 0.040m },

            // Pizza Prosciutto & Mushrooms: 0.22kg Flour, 0.10kg Mozzarella, 0.06kg Prosciutto, 0.08kg Mushrooms, 0.08kg Tomatoes
            new() { MenuItemId = menuItems[19].Id, InventoryItemId = invBySku["INV-FLOR-01"].Id, QuantityNeeded = 0.220m },
            new() { MenuItemId = menuItems[19].Id, InventoryItemId = invBySku["INV-MOZZ-01"].Id, QuantityNeeded = 0.100m },
            new() { MenuItemId = menuItems[19].Id, InventoryItemId = invBySku["INV-PROS-01"].Id, QuantityNeeded = 0.060m },
            new() { MenuItemId = menuItems[19].Id, InventoryItemId = invBySku["INV-MUSH-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[19].Id, InventoryItemId = invBySku["INV-TOM-01"].Id, QuantityNeeded = 0.080m },

            // Carbonara: 0.12kg Pasta, 0.07kg Guanciale, 0.04kg Parmesan, 2 eggs
            new() { MenuItemId = menuItems[22].Id, InventoryItemId = invBySku["INV-PAST-01"].Id, QuantityNeeded = 0.120m },
            new() { MenuItemId = menuItems[22].Id, InventoryItemId = invBySku["INV-GUAN-01"].Id, QuantityNeeded = 0.070m },
            new() { MenuItemId = menuItems[22].Id, InventoryItemId = invBySku["INV-PARM-01"].Id, QuantityNeeded = 0.040m },
            new() { MenuItemId = menuItems[22].Id, InventoryItemId = invBySku["INV-EGGS-01"].Id, QuantityNeeded = 2m },

            // Tagliatelle with Truffle: 0.12kg Pasta, 0.08kg Mushrooms, 0.06L Cream, 0.015kg Truffle, 0.03kg Parmesan
            new() { MenuItemId = menuItems[23].Id, InventoryItemId = invBySku["INV-PAST-01"].Id, QuantityNeeded = 0.120m },
            new() { MenuItemId = menuItems[23].Id, InventoryItemId = invBySku["INV-MUSH-01"].Id, QuantityNeeded = 0.080m },
            new() { MenuItemId = menuItems[23].Id, InventoryItemId = invBySku["INV-CRME-01"].Id, QuantityNeeded = 0.060m },
            new() { MenuItemId = menuItems[23].Id, InventoryItemId = invBySku["INV-TRUF-01"].Id, QuantityNeeded = 0.015m },
            new() { MenuItemId = menuItems[23].Id, InventoryItemId = invBySku["INV-PARM-01"].Id, QuantityNeeded = 0.030m },

            // Tiramisu: 0.10kg Mascarpone, 0.04kg Savoiardi, 2 eggs, 0.02kg Cocoa, 0.018kg Coffee
            new() { MenuItemId = menuItems[27].Id, InventoryItemId = invBySku["INV-MASC-01"].Id, QuantityNeeded = 0.100m },
            new() { MenuItemId = menuItems[27].Id, InventoryItemId = invBySku["INV-SAVO-01"].Id, QuantityNeeded = 0.040m },
            new() { MenuItemId = menuItems[27].Id, InventoryItemId = invBySku["INV-EGGS-01"].Id, QuantityNeeded = 2m },
            new() { MenuItemId = menuItems[27].Id, InventoryItemId = invBySku["INV-COCO-01"].Id, QuantityNeeded = 0.020m },
            new() { MenuItemId = menuItems[27].Id, InventoryItemId = invBySku["INV-COFF-01"].Id, QuantityNeeded = 0.018m },

            // Chocolate Fondant: 0.07kg Chocolate Callebaut, 0.04kg Butter, 1 egg
            new() { MenuItemId = menuItems[28].Id, InventoryItemId = invBySku["INV-CHOC-01"].Id, QuantityNeeded = 0.070m },
            new() { MenuItemId = menuItems[28].Id, InventoryItemId = invBySku["INV-BUTR-01"].Id, QuantityNeeded = 0.040m },
            new() { MenuItemId = menuItems[28].Id, InventoryItemId = invBySku["INV-EGGS-01"].Id, QuantityNeeded = 1m },

            // Espresso: 0.018kg Coffee
            new() { MenuItemId = menuItems[32].Id, InventoryItemId = invBySku["INV-COFF-01"].Id, QuantityNeeded = 0.018m },

            // Cappuccino: 0.018kg Coffee, 0.18L Milk Barista
            new() { MenuItemId = menuItems[33].Id, InventoryItemId = invBySku["INV-COFF-01"].Id, QuantityNeeded = 0.018m },
            new() { MenuItemId = menuItems[33].Id, InventoryItemId = invBySku["INV-MILK-01"].Id, QuantityNeeded = 0.180m },

            // Almond Latte: 0.018kg Coffee, 0.22L Almond Milk
            new() { MenuItemId = menuItems[34].Id, InventoryItemId = invBySku["INV-COFF-01"].Id, QuantityNeeded = 0.018m },
            new() { MenuItemId = menuItems[34].Id, InventoryItemId = invBySku["INV-AMILK-01"].Id, QuantityNeeded = 0.220m },

            // Flat White: 0.020kg Coffee, 0.14L Milk Barista
            new() { MenuItemId = menuItems[35].Id, InventoryItemId = invBySku["INV-COFF-01"].Id, QuantityNeeded = 0.020m },
            new() { MenuItemId = menuItems[35].Id, InventoryItemId = invBySku["INV-MILK-01"].Id, QuantityNeeded = 0.140m },

            // Alpine Tea: 0.015kg Tea Leaves, 0.03kg Lemon
            new() { MenuItemId = menuItems[36].Id, InventoryItemId = invBySku["INV-TEA-01"].Id, QuantityNeeded = 0.015m },
            new() { MenuItemId = menuItems[36].Id, InventoryItemId = invBySku["INV-LEMN-01"].Id, QuantityNeeded = 0.030m },

            // Aperol Spritz: 0.06L Aperol, 0.09L Prosecco
            new() { MenuItemId = menuItems[37].Id, InventoryItemId = invBySku["INV-APER-01"].Id, QuantityNeeded = 0.060m },
            new() { MenuItemId = menuItems[37].Id, InventoryItemId = invBySku["INV-WINE-01"].Id, QuantityNeeded = 0.150m },

            // Passionfruit Lemonade: 0.05L Passionfruit Syrup, 0.03kg Lemon
            new() { MenuItemId = menuItems[41].Id, InventoryItemId = invBySku["INV-SYRP-01"].Id, QuantityNeeded = 0.050m },
            new() { MenuItemId = menuItems[41].Id, InventoryItemId = invBySku["INV-LEMN-01"].Id, QuantityNeeded = 0.030m }
        };

        context.RecipeItems.AddRange(recipes);
        await context.SaveChangesAsync();

        // 10. Guests & Hotel Bookings (8 guests, realistic historical & current bookings)
        var guests = new List<Guest>
        {
            new() { FullName = "Олександр Коваленко", Phone = "+38 (067) 123-45-67", Email = "o.kovalenko@techholding.ua", PassportOrId = "FF123456", Notes = "VIP гість. Любить тихий кутовий номер." },
            new() { FullName = "Марія Мельник", Phone = "+38 (050) 987-65-43", Email = "m.melnyk@designstudio.com", PassportOrId = "FG654321", Notes = "Алергія на горіхи." },
            new() { FullName = "Дмитро Сидоренко", Phone = "+38 (093) 456-78-90", Email = "d.sydorenko@investbank.ua", PassportOrId = "FK998877", Notes = "Постійний гість номеру Люкс 202." },
            new() { FullName = "Олена Григоренко", Phone = "+38 (068) 222-33-44", Email = "o.grygorenko@partner.law", PassportOrId = "FL445566", Notes = "Ділове відрядження." },
            new() { FullName = "Андрій Бондар", Phone = "+38 (097) 333-44-55", Email = "a.bondar@itcorp.com", PassportOrId = "FM778899", Notes = "Президентський сьют 303." },
            new() { FullName = "Юлія Шевчук", Phone = "+38 (066) 777-88-99", Email = "y.shevchuk@traveler.ua", PassportOrId = "FN112233", Notes = "Сім'я з дитиною." },
            new() { FullName = "Володимир Ткач", Phone = "+38 (099) 888-99-00", Email = "v.tkach@agriholding.ua", PassportOrId = "FO334455", Notes = "Потрібен трансфер з аеропорту." },
            new() { FullName = "Тетяна Кравченко", Phone = "+38 (050) 111-22-33", Email = "t.kravchenko@fashion.ua", PassportOrId = "FP556677", Notes = "Замовлення вечері в номер." }
        };

        context.Guests.AddRange(guests);
        await context.SaveChangesAsync();

        var today = DateTime.Today;
        var bookings = new List<RoomBooking>
        {
            // Room 101 - Occupied by guest 0
            new()
            {
                GuestId = guests[0].Id,
                RoomId = rooms[0].Id,
                CheckInDate = today.AddDays(-2),
                CheckOutDate = today.AddDays(1),
                TotalPrice = 5700m,
                IsCheckedIn = true,
                SpecialRequests = "Тихий номер, додаткова подушка"
            },
            // Room 202 Deluxe - Occupied by guest 2
            new()
            {
                GuestId = guests[2].Id,
                RoomId = rooms[5].Id,
                CheckInDate = today.AddDays(-1),
                CheckOutDate = today.AddDays(3),
                TotalPrice = 15600m,
                IsCheckedIn = true,
                SpecialRequests = "Пляшка шампанського в номер при заїзді"
            },
            // Room 303 Presidential - Occupied by guest 4
            new()
            {
                GuestId = guests[4].Id,
                RoomId = rooms[10].Id,
                CheckInDate = today,
                CheckOutDate = today.AddDays(4),
                TotalPrice = 35600m,
                IsCheckedIn = true,
                SpecialRequests = "Персональний консьєрж-сервіс, ранній сніданок о 07:30"
            },
            // Room 103 - Reserved by guest 1
            new()
            {
                GuestId = guests[1].Id,
                RoomId = rooms[2].Id,
                CheckInDate = today.AddDays(1),
                CheckOutDate = today.AddDays(4),
                TotalPrice = 7500m,
                IsCheckedIn = false,
                SpecialRequests = "Робочий стіл біля вікна, пізній виїзд о 14:00"
            },
            // Room 301 - Reserved by guest 3
            new()
            {
                GuestId = guests[3].Id,
                RoomId = rooms[8].Id,
                CheckInDate = today.AddDays(2),
                CheckOutDate = today.AddDays(5),
                TotalPrice = 17400m,
                IsCheckedIn = false,
                SpecialRequests = "Місце для паркування біля головного входу"
            }
        };

        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync();

        // 11. Sample Restaurant Orders (Rich history for KDS, POS, and Analytics)
        // Order 1: Active at Table 2 (Main Hall) - Cooking in Kitchen
        var order1 = new Order
        {
            OrderNumber = "ORD-2026-101",
            TableId = tables[1].Id, // Table 2
            WaiterName = "Максим Іванов",
            Status = OrderStatus.Cooking,
            DiscountPercent = 0
        };
        order1.Items.Add(new OrderItem { MenuItemId = menuItems[0].Id, Quantity = 2, UnitPrice = menuItems[0].Price, Status = OrderStatus.Cooking, ModifiersOrNotes = "Соус подати окремо" });
        order1.Items.Add(new OrderItem { MenuItemId = menuItems[10].Id, Quantity = 1, UnitPrice = menuItems[10].Price, Status = OrderStatus.Cooking, ModifiersOrNotes = "Ступінь прожарки Medium Well" });
        order1.Items.Add(new OrderItem { MenuItemId = menuItems[17].Id, Quantity = 1, UnitPrice = menuItems[17].Price, Status = OrderStatus.Cooking, ModifiersOrNotes = "Хрустка скоринка" });
        order1.Items.Add(new OrderItem { MenuItemId = menuItems[41].Id, Quantity = 2, UnitPrice = menuItems[41].Price, Status = OrderStatus.Served });
        order1.RecalculateTotals();

        // Order 2: Active at Table 5 (Main Hall) - Bill Requested
        var order2 = new Order
        {
            OrderNumber = "ORD-2026-102",
            TableId = tables[4].Id, // Table 5
            WaiterName = "Анастасія Петренко",
            Status = OrderStatus.Ready,
            DiscountPercent = 5
        };
        order2.Items.Add(new OrderItem { MenuItemId = menuItems[2].Id, Quantity = 1, UnitPrice = menuItems[2].Price, Status = OrderStatus.Ready });
        order2.Items.Add(new OrderItem { MenuItemId = menuItems[11].Id, Quantity = 1, UnitPrice = menuItems[11].Price, Status = OrderStatus.Ready, ModifiersOrNotes = "Прожарка Medium" });
        order2.Items.Add(new OrderItem { MenuItemId = menuItems[37].Id, Quantity = 2, UnitPrice = menuItems[37].Price, Status = OrderStatus.Served });
        order2.RecalculateTotals();

        // Order 3: Active at Table 12 (Terrace) - Cooking
        var order3 = new Order
        {
            OrderNumber = "ORD-2026-103",
            TableId = tables[9].Id, // Table 12
            WaiterName = "Данило Коваль",
            Status = OrderStatus.Cooking,
            DiscountPercent = 0
        };
        order3.Items.Add(new OrderItem { MenuItemId = menuItems[3].Id, Quantity = 1, UnitPrice = menuItems[3].Price, Status = OrderStatus.Cooking });
        order3.Items.Add(new OrderItem { MenuItemId = menuItems[12].Id, Quantity = 1, UnitPrice = menuItems[12].Price, Status = OrderStatus.KitchenPending, ModifiersOrNotes = "Без часнику" });
        order3.Items.Add(new OrderItem { MenuItemId = menuItems[33].Id, Quantity = 2, UnitPrice = menuItems[33].Price, Status = OrderStatus.Cooking });
        order3.RecalculateTotals();

        // Order 4: Closed / Historical Order for Room 101 (Room Service)
        var order4 = new Order
        {
            OrderNumber = "ORD-2026-088",
            BookingId = bookings[0].Id,
            WaiterName = "Максим Іванов",
            Status = OrderStatus.Paid,
            DiscountPercent = 10
        };
        order4.Items.Add(new OrderItem { MenuItemId = menuItems[22].Id, Quantity = 2, UnitPrice = menuItems[22].Price, Status = OrderStatus.Served });
        order4.Items.Add(new OrderItem { MenuItemId = menuItems[27].Id, Quantity = 2, UnitPrice = menuItems[27].Price, Status = OrderStatus.Served });
        order4.Items.Add(new OrderItem { MenuItemId = menuItems[38].Id, Quantity = 2, UnitPrice = menuItems[38].Price, Status = OrderStatus.Served });
        order4.RecalculateTotals();

        var payment4 = new Payment
        {
            Order = order4,
            Amount = order4.TotalAmount,
            Method = PaymentMethod.RoomCharge,
            Status = PaymentStatus.Completed,
            TransactionReference = "ROOM-101-CHG-088"
        };
        order4.Payments.Add(payment4);

        // Order 5: Closed / Paid Table 1 earlier today
        var order5 = new Order
        {
            OrderNumber = "ORD-2026-089",
            TableId = tables[0].Id, // Table 1
            WaiterName = "Анастасія Петренко",
            Status = OrderStatus.Paid,
            DiscountPercent = 0
        };
        order5.Items.Add(new OrderItem { MenuItemId = menuItems[18].Id, Quantity = 1, UnitPrice = menuItems[18].Price, Status = OrderStatus.Served });
        order5.Items.Add(new OrderItem { MenuItemId = menuItems[28].Id, Quantity = 1, UnitPrice = menuItems[28].Price, Status = OrderStatus.Served });
        order5.Items.Add(new OrderItem { MenuItemId = menuItems[35].Id, Quantity = 2, UnitPrice = menuItems[35].Price, Status = OrderStatus.Served });
        order5.RecalculateTotals();

        var payment5 = new Payment
        {
            Order = order5,
            Amount = order5.TotalAmount,
            Method = PaymentMethod.CreditCard,
            Status = PaymentStatus.Completed,
            TransactionReference = "POS-TX-991204"
        };
        order5.Payments.Add(payment5);

        context.Orders.AddRange(order1, order2, order3, order4, order5);
        await context.SaveChangesAsync();

        // 12. Active Staff Shifts for Staff Monitoring Dashboard
        var allUsers = await context.Users.ToListAsync();
        var userWaiterMax = allUsers.First(u => u.Username == "waiter");
        var userWaiterNastya = allUsers.First(u => u.Username == "a.petrenko");
        var userWaiterDanylo = allUsers.First(u => u.Username == "d.koval");
        var userChefTaras = allUsers.First(u => u.Username == "chef");
        var userChefSergey = allUsers.First(u => u.Username == "s.bondar");
        var userBaristaNazar = allUsers.First(u => u.Username == "barista");

        var shifts = new List<StaffShift>
        {
            new()
            {
                UserId = userWaiterMax.Id,
                RoleAtShift = UserRole.Waiter,
                ClockInTime = DateTime.UtcNow.AddHours(-6),
                Status = ShiftStatus.Active,
                AssignedZoneOrStation = "Основний зал",
                OrdersProcessed = 14,
                TotalSales = 18450m,
                TipsEarned = 1850m
            },
            new()
            {
                UserId = userWaiterNastya.Id,
                RoleAtShift = UserRole.Waiter,
                ClockInTime = DateTime.UtcNow.AddHours(-5.5),
                Status = ShiftStatus.Active,
                AssignedZoneOrStation = "Основний зал & VIP",
                OrdersProcessed = 11,
                TotalSales = 14200m,
                TipsEarned = 1450m
            },
            new()
            {
                UserId = userWaiterDanylo.Id,
                RoleAtShift = UserRole.Waiter,
                ClockInTime = DateTime.UtcNow.AddHours(-4),
                Status = ShiftStatus.Active,
                AssignedZoneOrStation = "Літня тераса",
                OrdersProcessed = 8,
                TotalSales = 9850m,
                TipsEarned = 980m
            },
            new()
            {
                UserId = userChefTaras.Id,
                RoleAtShift = UserRole.Chef,
                ClockInTime = DateTime.UtcNow.AddHours(-7),
                Status = ShiftStatus.Active,
                AssignedZoneOrStation = "Бренд-шеф / Контроль видачі",
                OrdersProcessed = 32,
                TotalSales = 42500m,
                TipsEarned = 0m
            },
            new()
            {
                UserId = userChefSergey.Id,
                RoleAtShift = UserRole.Chef,
                ClockInTime = DateTime.UtcNow.AddHours(-6.5),
                Status = ShiftStatus.Active,
                AssignedZoneOrStation = "Гарячий цех / Стейки & Гриль",
                OrdersProcessed = 28,
                TotalSales = 38900m,
                TipsEarned = 0m
            },
            new()
            {
                UserId = userBaristaNazar.Id,
                RoleAtShift = UserRole.Barista,
                ClockInTime = DateTime.UtcNow.AddHours(-5),
                Status = ShiftStatus.Active,
                AssignedZoneOrStation = "Головний бар & Кавова станція",
                OrdersProcessed = 22,
                TotalSales = 7600m,
                TipsEarned = 720m
            }
        };

        context.StaffShifts.AddRange(shifts);
        await context.SaveChangesAsync();
    }
}
