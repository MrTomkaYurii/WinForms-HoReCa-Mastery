using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Services;

public class ProcurementService : IProcurementService
{
    private readonly IDbContextFactory<HoReCaDbContext> _contextFactory;

    public ProcurementService(IDbContextFactory<HoReCaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.ContactPerson,
                s.Phone,
                s.Email,
                s.Address,
                s.Category,
                s.Edrpou
            ))
            .ToListAsync(ct);
    }

    public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var orders = await db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.InventoryItem)
            .AsNoTracking()
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(ct);

        return orders.Select(po => new PurchaseOrderDto(
            po.Id,
            po.OrderNumber,
            po.SupplierId,
            po.Supplier.Name,
            po.OrderDate,
            po.ExpectedDeliveryDate,
            po.Status,
            po.TotalAmount,
            po.Notes,
            po.Items.Select(i => new PurchaseOrderItemDto(
                i.Id,
                i.InventoryItemId,
                i.InventoryItem.Name,
                i.InventoryItem.SKU,
                i.InventoryItem.Unit,
                i.QuantityOrdered,
                i.UnitCost,
                i.LineTotal
            )).ToList()
        )).ToList();
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest req, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var supplier = await db.Suppliers.FindAsync(new object[] { req.SupplierId }, ct);
        if (supplier == null) throw new InvalidOperationException($"Supplier with id {req.SupplierId} not found.");

        var itemIds = req.Items.Select(i => i.InventoryItemId).ToList();
        var inventoryMap = await db.Inventory
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var po = new PurchaseOrder
        {
            OrderNumber = $"PO-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}",
            SupplierId = req.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = req.ExpectedDeliveryDate ?? DateTime.UtcNow.AddDays(1),
            Status = PurchaseOrderStatus.Sent,
            Notes = req.Notes
        };

        decimal total = 0m;
        foreach (var itemReq in req.Items)
        {
            if (inventoryMap.TryGetValue(itemReq.InventoryItemId, out var inv))
            {
                var poItem = new PurchaseOrderItem
                {
                    InventoryItemId = inv.Id,
                    QuantityOrdered = itemReq.QuantityOrdered,
                    UnitCost = itemReq.UnitCost > 0 ? itemReq.UnitCost : inv.CostPerUnit
                };
                total += poItem.LineTotal;
                po.Items.Add(poItem);
            }
        }

        po.TotalAmount = total;
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(ct);

        return new PurchaseOrderDto(
            po.Id,
            po.OrderNumber,
            po.SupplierId,
            supplier.Name,
            po.OrderDate,
            po.ExpectedDeliveryDate,
            po.Status,
            po.TotalAmount,
            po.Notes,
            po.Items.Select(i => new PurchaseOrderItemDto(
                i.Id,
                i.InventoryItemId,
                inventoryMap[i.InventoryItemId].Name,
                inventoryMap[i.InventoryItemId].SKU,
                inventoryMap[i.InventoryItemId].Unit,
                i.QuantityOrdered,
                i.UnitCost,
                i.LineTotal
            )).ToList()
        );
    }

    public async Task ReceivePurchaseOrderAsync(int purchaseOrderId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var po = await db.PurchaseOrders
            .Include(p => p.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct);

        if (po == null || po.Status == PurchaseOrderStatus.Delivered) return;

        foreach (var item in po.Items)
        {
            if (item.InventoryItem != null)
            {
                item.InventoryItem.CurrentStock += item.QuantityOrdered;
                item.InventoryItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        po.Status = PurchaseOrderStatus.Delivered;
        po.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
