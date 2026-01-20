using HoReCa.Domain.Common;
using HoReCa.Domain.Enums;

namespace HoReCa.Domain.Entities;

public class Guest : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PassportOrId { get; set; }
    public string? Notes { get; set; }

    public ICollection<RoomBooking> Bookings { get; set; } = new List<RoomBooking>();
}

public class Room : BaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; } = 1;
    public RoomType Type { get; set; } = RoomType.Standard;
    public decimal DailyRate { get; set; }
    public int Capacity { get; set; } = 2;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? Description { get; set; }

    public ICollection<RoomBooking> Bookings { get; set; } = new List<RoomBooking>();
}

public class RoomBooking : BaseEntity
{
    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsCheckedIn { get; set; }
    public bool IsCheckedOut { get; set; }
    public string? SpecialRequests { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
