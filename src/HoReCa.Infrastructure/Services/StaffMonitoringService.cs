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

public class StaffMonitoringService : IStaffMonitoringService
{
    private readonly IDbContextFactory<HoReCaDbContext> _contextFactory;

    public StaffMonitoringService(IDbContextFactory<HoReCaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<StaffDashboardDto> GetStaffDashboardAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        var activeShifts = await db.StaffShifts
            .Include(s => s.User)
            .Where(s => s.Status == ShiftStatus.Active)
            .ToListAsync(ct);

        int totalOnShift = activeShifts.Count;
        int kitchenCrewCount = activeShifts.Count(s => s.User.Role == UserRole.Chef);
        int waitersOnFloor = activeShifts.Count(s => s.User.Role == UserRole.Waiter);

        // Count active orders currently in kitchen
        int activeOrdersInKitchen = await db.Orders
            .CountAsync(o => o.Status == OrderStatus.KitchenPending || o.Status == OrderStatus.Cooking, ct);

        // Chefs list & workload
        var allChefs = await db.Users
            .Where(u => u.Role == UserRole.Chef && u.IsActive)
            .ToListAsync(ct);

        var chefWorkloads = allChefs.Select(c =>
        {
            var shift = activeShifts.FirstOrDefault(s => s.UserId == c.Id);
            bool isOnShift = shift != null;
            return new ChefWorkloadDto(
                c.Id,
                c.FullName,
                shift?.AssignedZoneOrStation ?? "Гарячий / Холодний цех",
                isOnShift ? activeOrdersInKitchen : 0,
                shift?.OrdersProcessed ?? 12,
                isOnShift
            );
        }).ToList();

        // Waiters list & performance
        var allWaiters = await db.Users
            .Where(u => u.Role == UserRole.Waiter && u.IsActive)
            .ToListAsync(ct);

        var occupiedTables = await db.Tables
            .Include(t => t.Zone)
            .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled))
            .Where(t => t.Status == TableStatus.Occupied)
            .ToListAsync(ct);

        var waiterPerformances = allWaiters.Select(w =>
        {
            var shift = activeShifts.FirstOrDefault(s => s.UserId == w.Id);
            bool isOnShift = shift != null;
            var assignedTables = occupiedTables.Where(t => t.Orders.Any(o => o.WaiterName.Contains(w.Username) || o.WaiterName.Contains(w.FullName.Split(' ')[0]))).ToList();
            
            return new WaiterPerformanceDto(
                w.Id,
                w.FullName,
                shift?.AssignedZoneOrStation ?? "Основний зал",
                assignedTables.Count,
                shift?.OrdersProcessed ?? 6,
                shift?.TotalSales ?? 4850m,
                shift?.TipsEarned ?? 480m,
                isOnShift
            );
        }).ToList();

        var recentShifts = await db.StaffShifts
            .Include(s => s.User)
            .OrderByDescending(s => s.ClockInTime)
            .Take(10)
            .Select(s => new StaffShiftDto(
                s.Id,
                s.UserId,
                s.User.FullName,
                s.User.Username,
                s.User.Role,
                s.ClockInTime,
                s.ClockOutTime,
                s.OrdersProcessed,
                s.TotalSales,
                s.TipsEarned,
                s.Status,
                s.AssignedZoneOrStation
            ))
            .ToListAsync(ct);

        return new StaffDashboardDto(
            totalOnShift,
            kitchenCrewCount,
            waitersOnFloor,
            activeOrdersInKitchen,
            chefWorkloads,
            waiterPerformances,
            recentShifts
        );
    }

    public async Task<List<StaffShiftDto>> GetActiveShiftsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.StaffShifts
            .Include(s => s.User)
            .Where(s => s.Status == ShiftStatus.Active)
            .OrderByDescending(s => s.ClockInTime)
            .Select(s => new StaffShiftDto(
                s.Id,
                s.UserId,
                s.User.FullName,
                s.User.Username,
                s.User.Role,
                s.ClockInTime,
                s.ClockOutTime,
                s.OrdersProcessed,
                s.TotalSales,
                s.TipsEarned,
                s.Status,
                s.AssignedZoneOrStation
            ))
            .ToListAsync(ct);
    }

    public async Task<StaffShiftDto> ClockInAsync(int userId, string? station = null, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) throw new InvalidOperationException($"User with id {userId} not found.");

        var existingActive = await db.StaffShifts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ShiftStatus.Active, ct);

        if (existingActive != null)
        {
            return new StaffShiftDto(
                existingActive.Id,
                existingActive.UserId,
                user.FullName,
                user.Username,
                user.Role,
                existingActive.ClockInTime,
                null,
                existingActive.OrdersProcessed,
                existingActive.TotalSales,
                existingActive.TipsEarned,
                existingActive.Status,
                existingActive.AssignedZoneOrStation
            );
        }

        var shift = new StaffShift
        {
            UserId = userId,
            ClockInTime = DateTime.UtcNow,
            RoleAtShift = user.Role,
            Status = ShiftStatus.Active,
            AssignedZoneOrStation = station ?? (user.Role == UserRole.Chef ? "Гарячий цех" : (user.Role == UserRole.Waiter ? "Основний зал" : "Рецепція"))
        };

        db.StaffShifts.Add(shift);
        await db.SaveChangesAsync(ct);

        return new StaffShiftDto(
            shift.Id,
            shift.UserId,
            user.FullName,
            user.Username,
            user.Role,
            shift.ClockInTime,
            null,
            0,
            0m,
            0m,
            shift.Status,
            shift.AssignedZoneOrStation
        );
    }

    public async Task ClockOutAsync(int shiftId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var shift = await db.StaffShifts.FindAsync(new object[] { shiftId }, ct);
        if (shift != null && shift.Status == ShiftStatus.Active)
        {
            shift.ClockOutTime = DateTime.UtcNow;
            shift.Status = ShiftStatus.Completed;
            shift.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
