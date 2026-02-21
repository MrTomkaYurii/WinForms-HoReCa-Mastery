using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Services;

public class RoomBookingService : IRoomBookingService
{
    private readonly IDbContextFactory<HoReCaDbContext> _contextFactory;

    public RoomBookingService(IDbContextFactory<HoReCaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<RoomDto>> GetAllRoomsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var rooms = await db.Rooms
            .Include(r => r.Bookings.Where(b => b.IsCheckedIn && !b.IsCheckedOut)).ThenInclude(b => b.Guest)
            .OrderBy(r => r.RoomNumber)
            .AsNoTracking()
            .ToListAsync(ct);

        return rooms.Select(r => new RoomDto(
            r.Id,
            r.RoomNumber,
            r.Floor,
            r.Type,
            r.DailyRate,
            r.Capacity,
            r.Status,
            r.Bookings.FirstOrDefault()?.Guest?.FullName
        )).ToList();
    }

    public async Task<List<BookingDto>> GetBookingsForDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var bookings = await db.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Room)
            .Where(b => b.CheckInDate < to && b.CheckOutDate > from)
            .AsNoTracking()
            .ToListAsync(ct);

        return bookings.Select(b => new BookingDto(
            b.Id,
            b.RoomId,
            b.Room.RoomNumber,
            b.GuestId,
            b.Guest.FullName,
            b.Guest.Phone,
            b.CheckInDate,
            b.CheckOutDate,
            b.TotalPrice,
            b.IsCheckedIn,
            b.IsCheckedOut,
            b.SpecialRequests
        )).ToList();
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        // Find or create guest
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.Phone == request.GuestPhone, ct);
        if (guest == null)
        {
            guest = new Guest
            {
                FullName = request.GuestFullName,
                Phone = request.GuestPhone,
                Email = request.GuestEmail,
                PassportOrId = request.PassportOrId
            };
            db.Guests.Add(guest);
            await db.SaveChangesAsync(ct);
        }

        var room = await db.Rooms.FindAsync(new object[] { request.RoomId }, ct)
            ?? throw new InvalidOperationException($"Номер готелю з ID {request.RoomId} не знайдено.");

        int nights = Math.Max(1, (int)(request.CheckOutDate.Date - request.CheckInDate.Date).TotalDays);
        decimal totalPrice = room.DailyRate * nights;

        var booking = new RoomBooking
        {
            GuestId = guest.Id,
            RoomId = room.Id,
            CheckInDate = request.CheckInDate.Date,
            CheckOutDate = request.CheckOutDate.Date,
            TotalPrice = totalPrice,
            SpecialRequests = request.SpecialRequests
        };

        if (room.Status == RoomStatus.Available)
        {
            room.Status = RoomStatus.Reserved;
        }

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        return new BookingDto(
            booking.Id,
            room.Id,
            room.RoomNumber,
            guest.Id,
            guest.FullName,
            guest.Phone,
            booking.CheckInDate,
            booking.CheckOutDate,
            booking.TotalPrice,
            booking.IsCheckedIn,
            booking.IsCheckedOut,
            booking.SpecialRequests
        );
    }

    public async Task CheckInGuestAsync(int bookingId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var booking = await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking != null)
        {
            booking.IsCheckedIn = true;
            booking.Room.Status = RoomStatus.Occupied;
            booking.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task CheckOutGuestAsync(int bookingId, PaymentMethod paymentMethod, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var booking = await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking != null)
        {
            booking.IsCheckedOut = true;
            booking.Room.Status = RoomStatus.Cleaning;
            booking.UpdatedAt = DateTime.UtcNow;

            db.Payments.Add(new Payment
            {
                BookingId = booking.Id,
                Amount = booking.TotalPrice,
                Method = paymentMethod,
                Status = PaymentStatus.Completed,
                TransactionReference = $"HTL-{Guid.NewGuid():N}"[..16]
            });

            await db.SaveChangesAsync(ct);
        }
    }
}
