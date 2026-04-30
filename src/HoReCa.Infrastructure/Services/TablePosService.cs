using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Services;

public class TablePosService : ITablePosService
{
    private readonly IDbContextFactory<HoReCaDbContext> _contextFactory;

    public TablePosService(IDbContextFactory<HoReCaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<TableZone>> GetZonesAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.TableZones
            .OrderBy(z => z.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<List<TableDto>> GetTablesByZoneAsync(int? zoneId = null, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        
        var query = db.Tables
            .Include(t => t.Zone)
            .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled))
            .AsNoTracking();

        if (zoneId.HasValue)
        {
            query = query.Where(t => t.ZoneId == zoneId.Value);
        }

        var tables = await query.ToListAsync(ct);

        return tables.Select(t =>
        {
            var activeOrder = t.Orders.FirstOrDefault();
            return new TableDto(
                t.Id,
                t.TableNumber,
                t.ZoneId,
                t.Zone.Name,
                t.Capacity,
                t.Status,
                t.Shape,
                t.PositionX,
                t.PositionY,
                t.Width,
                t.Height,
                activeOrder?.Id,
                activeOrder?.TotalAmount,
                activeOrder?.CreatedAt
            );
        }).ToList();
    }

    public async Task UpdateTablePositionAsync(UpdateTablePositionRequest request, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var table = await db.Tables.FindAsync(new object[] { request.TableId }, ct);
        if (table != null)
        {
            table.PositionX = request.NewX;
            table.PositionY = request.NewY;
            table.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<MenuCategory>> GetMenuCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.MenuCategories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<List<MenuItemDto>> GetMenuItemsAsync(int? categoryId = null, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var query = db.MenuItems.Include(m => m.Category).AsNoTracking();
        
        if (categoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == categoryId.Value);
        }

        var items = await query.ToListAsync(ct);
        return items.Select(m => new MenuItemDto(
            m.Id,
            m.CategoryId,
            m.Category.Name,
            m.Name,
            m.Description,
            m.Price,
            m.PreparationMinutes,
            m.IsAvailable,
            m.ImageUrl
        )).ToList();
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}",
            TableId = request.TableId,
            BookingId = request.BookingId,
            WaiterName = request.WaiterName,
            Status = OrderStatus.KitchenPending
        };

        var menuIds = request.Items.Select(i => i.MenuItemId).ToList();
        var menuItems = await db.MenuItems
            .Where(m => menuIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        foreach (var itemReq in request.Items)
        {
            if (menuItems.TryGetValue(itemReq.MenuItemId, out var menuItem))
            {
                order.Items.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = itemReq.Quantity,
                    UnitPrice = menuItem.Price,
                    ModifiersOrNotes = itemReq.ModifiersOrNotes,
                    Status = OrderStatus.KitchenPending
                });
            }
        }

        order.RecalculateTotals();

        // 1. Deduct recipe ingredients automatically from Warehouse stock
        var itemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var recipes = await db.RecipeItems
            .Include(r => r.InventoryItem)
            .Where(r => itemIds.Contains(r.MenuItemId))
            .ToListAsync(ct);

        foreach (var itemReq in request.Items)
        {
            var itemRecipes = recipes.Where(r => r.MenuItemId == itemReq.MenuItemId);
            foreach (var recipe in itemRecipes)
            {
                if (recipe.InventoryItem != null)
                {
                    decimal deductAmount = recipe.QuantityNeeded * itemReq.Quantity;
                    recipe.InventoryItem.CurrentStock = Math.Max(0, recipe.InventoryItem.CurrentStock - deductAmount);
                    recipe.InventoryItem.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        if (request.TableId.HasValue)
        {
            var table = await db.Tables.FindAsync(new object[] { request.TableId.Value }, ct);
            if (table != null)
            {
                table.Status = TableStatus.Occupied;
            }
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return await GetOrderDtoAsync(db, order.Id, ct);
    }

    public async Task<OrderDto?> GetActiveOrderForTableAsync(int tableId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Table)
            .Where(o => o.TableId == tableId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (order == null) return null;

        return MapToDto(order);
    }

    public async Task CloseOrderAndPayAsync(int orderId, PaymentMethod method, decimal amountPaid, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var order = await db.Orders
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order != null)
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;

            if (order.Table != null)
            {
                order.Table.Status = TableStatus.Free;
            }

            db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = amountPaid,
                Method = method,
                Status = PaymentStatus.Completed,
                TransactionReference = $"TRX-{Guid.NewGuid():N}"[..16]
            });

            // Update active staff shift for waiter
            var activeShift = await db.StaffShifts
                .Where(s => s.Status == ShiftStatus.Active && s.User.FullName.Contains(order.WaiterName))
                .FirstOrDefaultAsync(ct);

            if (activeShift != null)
            {
                activeShift.OrdersProcessed++;
                activeShift.TotalSales += amountPaid;
                activeShift.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<OrderDto> GetOrderDtoAsync(HoReCaDbContext db, int orderId, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Table)
            .FirstAsync(o => o.Id == orderId, ct);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.TableId,
            order.Table?.TableNumber,
            order.BookingId,
            order.WaiterName,
            order.Status,
            order.Subtotal,
            order.DiscountPercent,
            order.TotalAmount,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.MenuItemId,
                i.MenuItem.Name,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.ModifiersOrNotes,
                i.Status
            )).ToList()
        );
    }
}
