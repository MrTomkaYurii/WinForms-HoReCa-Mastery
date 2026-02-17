using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(HoReCaDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.TableZones.AnyAsync())
        {
            return; // Database already seeded
        }

        // 1. Zones
        var zoneMain = new TableZone { Name = "Основний зал", Description = "Головна зала ресторану з м'якими диванами", DisplayOrder = 1 };
        var zoneTerrace = new TableZone { Name = "Літня тераса", Description = "Панорамний вид на парк", DisplayOrder = 2 };
        var zoneVip = new TableZone { Name = "VIP Лаундж", Description = "Затишна зона для ділових зустрічей", DisplayOrder = 3 };

        context.TableZones.AddRange(zoneMain, zoneTerrace, zoneVip);
        await context.SaveChangesAsync();

        // 2. Tables with coordinates for Drag & Drop UI
        var tables = new List<RestaurantTable>
        {
            // Main Hall
            new() { TableNumber = 1, ZoneId = zoneMain.Id, Capacity = 2, Shape = TableShape.Round, PositionX = 40, PositionY = 40, Width = 80, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 2, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 160, PositionY = 40, Width = 110, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 3, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 310, PositionY = 40, Width = 110, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 4, ZoneId = zoneMain.Id, Capacity = 6, Shape = TableShape.Rectangle, PositionX = 460, PositionY = 40, Width = 130, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 5, ZoneId = zoneMain.Id, Capacity = 2, Shape = TableShape.Square, PositionX = 40, PositionY = 170, Width = 80, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 6, ZoneId = zoneMain.Id, Capacity = 4, Shape = TableShape.Rectangle, PositionX = 160, PositionY = 170, Width = 110, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 7, ZoneId = zoneMain.Id, Capacity = 8, Shape = TableShape.Rectangle, PositionX = 310, PositionY = 170, Width = 160, Height = 90, Status = TableStatus.Free },

            // Terrace
            new() { TableNumber = 10, ZoneId = zoneTerrace.Id, Capacity = 2, Shape = TableShape.Round, PositionX = 60, PositionY = 50, Width = 80, Height = 80, Status = TableStatus.Free },
            new() { TableNumber = 11, ZoneId = zoneTerrace.Id, Capacity = 4, Shape = TableShape.Square, PositionX = 190, PositionY = 50, Width = 90, Height = 90, Status = TableStatus.Free },
            new() { TableNumber = 12, ZoneId = zoneTerrace.Id, Capacity = 4, Shape = TableShape.Square, PositionX = 330, PositionY = 50, Width = 90, Height = 90, Status = TableStatus.Free },
            new() { TableNumber = 14, ZoneId = zoneTerrace.Id, Capacity = 6, Shape = TableShape.Rectangle, PositionX = 470, PositionY = 50, Width = 120, Height = 85, Status = TableStatus.Free },

            // VIP
            new() { TableNumber = 20, ZoneId = zoneVip.Id, Capacity = 10, Shape = TableShape.Rectangle, PositionX = 100, PositionY = 70, Width = 200, Height = 110, Status = TableStatus.Free },
            new() { TableNumber = 21, ZoneId = zoneVip.Id, Capacity = 6, Shape = TableShape.Round, PositionX = 360, PositionY = 70, Width = 120, Height = 120, Status = TableStatus.Free }
        };

        context.Tables.AddRange(tables);

        // 3. Hotel Rooms
        var rooms = new List<Room>
        {
            new() { RoomNumber = "101", Floor = 1, Type = RoomType.Standard, DailyRate = 1800m, Capacity = 2, Status = RoomStatus.Available, Description = "Затишний одномісний стандарт з видом на сад" },
            new() { RoomNumber = "102", Floor = 1, Type = RoomType.Standard, DailyRate = 1800m, Capacity = 2, Status = RoomStatus.Available, Description = "Стандарт з двома окремими ліжками" },
            new() { RoomNumber = "103", Floor = 1, Type = RoomType.Comfort, DailyRate = 2400m, Capacity = 2, Status = RoomStatus.Available, Description = "Покращений комфорт із робочим місцем" },
            new() { RoomNumber = "201", Floor = 2, Type = RoomType.Comfort, DailyRate = 2600m, Capacity = 3, Status = RoomStatus.Available, Description = "Сімейний комфорт з балконом" },
            new() { RoomNumber = "202", Floor = 2, Type = RoomType.Deluxe, DailyRate = 3800m, Capacity = 2, Status = RoomStatus.Available, Description = "Люкс із панорамним видом та джакузі" },
            new() { RoomNumber = "203", Floor = 2, Type = RoomType.Deluxe, DailyRate = 3800m, Capacity = 2, Status = RoomStatus.Available, Description = "Преміум люкс з терасою" },
            new() { RoomNumber = "301", Floor = 3, Type = RoomType.PresidentialSuite, DailyRate = 7500m, Capacity = 4, Status = RoomStatus.Available, Description = "Президентські апартаменти з власною вітальнею та баром" }
        };

        context.Rooms.AddRange(rooms);

        // 4. Menu Categories
        var catStarters = new MenuCategory { Name = "Холодні закуски", IconKey = "Salad", DisplayOrder = 1, RouteToKitchen = true };
        var catMains = new MenuCategory { Name = "Гарячі страви", IconKey = "Meat", DisplayOrder = 2, RouteToKitchen = true };
        var catPizza = new MenuCategory { Name = "Піца & Паста", IconKey = "Pizza", DisplayOrder = 3, RouteToKitchen = true };
        var catDesserts = new MenuCategory { Name = "Десерти", IconKey = "Cake", DisplayOrder = 4, RouteToKitchen = true };
        var catBeverages = new MenuCategory { Name = "Безалкогольні напої", IconKey = "Glass", DisplayOrder = 5, RouteToKitchen = false };
        var catBar = new MenuCategory { Name = "Бар & Вино", IconKey = "Wine", DisplayOrder = 6, RouteToKitchen = false };

        context.MenuCategories.AddRange(catStarters, catMains, catPizza, catDesserts, catBeverages, catBar);
        await context.SaveChangesAsync();

        // 5. Menu Items
        var menuItems = new List<MenuItem>
        {
            // Starters
            new() { CategoryId = catStarters.Id, Name = "Салат Цезар з куркою су-від", Price = 285m, PreparationMinutes = 10, Description = "Мікс салатів, соус цезар, крутони, пармезан" },
            new() { CategoryId = catStarters.Id, Name = "Тартар із телятини з каперсами", Price = 360m, PreparationMinutes = 12, Description = "Витримана яловичина, трюфельна олія, бріош" },
            new() { CategoryId = catStarters.Id, Name = "Брускети з томатами та страчателлою", Price = 220m, PreparationMinutes = 8, Description = "Чиабата, базилік, песто, томати чері" },

            // Mains
            new() { CategoryId = catMains.Id, Name = "Стейк Рібай з печеними овочами", Price = 690m, PreparationMinutes = 20, Description = "Преміум витримка, вершково-перцевий соус" },
            new() { CategoryId = catMains.Id, Name = "Філе лосося з пюре з селери", Price = 540m, PreparationMinutes = 18, Description = "Спаржа, голландський соус, мікрозелень" },
            new() { CategoryId = catMains.Id, Name = "Качине філе з ягідним соусом", Price = 470m, PreparationMinutes = 22, Description = "Карамелізоване яблуко, журавлинний деміглас" },

            // Pizza & Pasta
            new() { CategoryId = catPizza.Id, Name = "Піца 4 Сири на дровах", Price = 340m, PreparationMinutes = 15, Description = "Моцарела, горгонзола, пармезан, брі, мед" },
            new() { CategoryId = catPizza.Id, Name = "Піца Прошуто Котто & Гриби", Price = 330m, PreparationMinutes = 15, Description = "Томатний соус, печериці, шинка котто" },
            new() { CategoryId = catPizza.Id, Name = "Паста Карбонара класична", Price = 295m, PreparationMinutes = 14, Description = "Спагеті, гуанчале, жовток, пекоріно романо" },

            // Desserts
            new() { CategoryId = catDesserts.Id, Name = "Тірамісу за автентичним рецептом", Price = 195m, PreparationMinutes = 5, Description = "Маскарпоне, савоярді, еспресо, марсала" },
            new() { CategoryId = catDesserts.Id, Name = "Шоколадний фондан з ванільним морозивом", Price = 230m, PreparationMinutes = 12, Description = "Бельгійський темний шоколад, ягідний кюлі" },

            // Beverages
            new() { CategoryId = catBeverages.Id, Name = "Еспресо / Амерікано", Price = 65m, PreparationMinutes = 3, Description = "Свіжообсмажена 100% арабіка" },
            new() { CategoryId = catBeverages.Id, Name = "Капучино на мигдалевому молоці", Price = 95m, PreparationMinutes = 4, Description = "Подвійний шот, оксамитова пінка" },
            new() { CategoryId = catBeverages.Id, Name = "Лимонад Маракуя-М'ята 500мл", Price = 140m, PreparationMinutes = 5, Description = "Пюре маракуї, свіжа м'ята, лайм, содова" },

            // Bar
            new() { CategoryId = catBar.Id, Name = "Коктейль Aperol Spritz", Price = 260m, PreparationMinutes = 4, Description = "Апероль, просеко, содова, апельсин" },
            new() { CategoryId = catBar.Id, Name = "Келих К'янті Класіко DOCG 150мл", Price = 240m, PreparationMinutes = 3, Description = "Тоскана, Італія, сухе червоне" }
        };

        context.MenuItems.AddRange(menuItems);

        // 6. Inventory Items
        var inventory = new List<InventoryItem>
        {
            new() { Name = "Яловича вирізка преміум", SKU = "INV-BEEF-01", Unit = "кг", CurrentStock = 18.5m, MinimumStock = 5.0m, CostPerUnit = 480m },
            new() { Name = "Лосось норвезький філе", SKU = "INV-FISH-01", Unit = "кг", CurrentStock = 9.2m, MinimumStock = 4.0m, CostPerUnit = 620m },
            new() { Name = "Сир Моцарела Fior di Latte", SKU = "INV-CHSE-01", Unit = "кг", CurrentStock = 24.0m, MinimumStock = 8.0m, CostPerUnit = 240m },
            new() { Name = "Сир Пармезан 24 міс.", SKU = "INV-CHSE-02", Unit = "кг", CurrentStock = 6.4m, MinimumStock = 2.0m, CostPerUnit = 780m },
            new() { Name = "Кавові зерна Specialty 100% Arabica", SKU = "INV-COFF-01", Unit = "кг", CurrentStock = 14.0m, MinimumStock = 5.0m, CostPerUnit = 650m },
            new() { Name = "Молоко бариста 3.2%", SKU = "INV-MILK-01", Unit = "л", CurrentStock = 42.0m, MinimumStock = 15.0m, CostPerUnit = 48m },
            new() { Name = "Олія оливкова Extra Virgin", SKU = "INV-OIL-01", Unit = "л", CurrentStock = 3.2m, MinimumStock = 5.0m, CostPerUnit = 380m }, // Low stock!
            new() { Name = "Томати конкасе Mutti", SKU = "INV-TOM-01", Unit = "кг", CurrentStock = 4.0m, MinimumStock = 10.0m, CostPerUnit = 110m } // Low stock!
        };

        context.Inventory.AddRange(inventory);

        // 7. Guests & Sample Bookings
        var guest1 = new Guest { FullName = "Олександр Коваленко", Phone = "+38 (067) 123-45-67", Email = "o.kovalenko@example.com", PassportOrId = "FF123456" };
        var guest2 = new Guest { FullName = "Марія Мельник", Phone = "+38 (050) 987-65-43", Email = "m.melnyk@example.com", PassportOrId = "FG654321" };

        context.Guests.AddRange(guest1, guest2);
        await context.SaveChangesAsync();

        var today = DateTime.Today;
        var booking1 = new RoomBooking
        {
            GuestId = guest1.Id,
            RoomId = rooms[0].Id, // 101
            CheckInDate = today.AddDays(-1),
            CheckOutDate = today.AddDays(2),
            TotalPrice = 5400m,
            IsCheckedIn = true,
            SpecialRequests = "Тихий номер, додаткова подушка"
        };
        rooms[0].Status = RoomStatus.Occupied;

        var booking2 = new RoomBooking
        {
            GuestId = guest2.Id,
            RoomId = rooms[4].Id, // 202 Deluxe
            CheckInDate = today.AddDays(1),
            CheckOutDate = today.AddDays(4),
            TotalPrice = 11400m,
            IsCheckedIn = false,
            SpecialRequests = "Ранній заїзд о 11:00"
        };
        rooms[4].Status = RoomStatus.Reserved;

        context.Bookings.AddRange(booking1, booking2);

        // 8. Sample Active Restaurant Order
        var activeTable = tables[1]; // Table 2
        activeTable.Status = TableStatus.Occupied;

        var sampleOrder = new Order
        {
            OrderNumber = "ORD-2026-001",
            TableId = activeTable.Id,
            WaiterName = "Максим",
            Status = OrderStatus.Cooking,
            DiscountPercent = 0
        };

        sampleOrder.Items.Add(new OrderItem
        {
            MenuItemId = menuItems[0].Id, // Caesar
            Quantity = 2,
            UnitPrice = menuItems[0].Price,
            Status = OrderStatus.Cooking,
            ModifiersOrNotes = "Соус окремо"
        });

        sampleOrder.Items.Add(new OrderItem
        {
            MenuItemId = menuItems[3].Id, // Ribeye
            Quantity = 1,
            UnitPrice = menuItems[3].Price,
            Status = OrderStatus.KitchenPending,
            ModifiersOrNotes = "Прожарка Medium Well"
        });

        sampleOrder.Items.Add(new OrderItem
        {
            MenuItemId = menuItems[13].Id, // Lemonade
            Quantity = 2,
            UnitPrice = menuItems[13].Price,
            Status = OrderStatus.Served
        });

        sampleOrder.RecalculateTotals();
        context.Orders.Add(sampleOrder);

        await context.SaveChangesAsync();
    }
}
