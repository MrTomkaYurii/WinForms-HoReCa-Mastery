namespace HoReCa.Domain.Enums;

public enum RoomType
{
    Standard = 1,
    Comfort = 2,
    Deluxe = 3,
    PresidentialSuite = 4
}

public enum RoomStatus
{
    Available = 1,
    Occupied = 2,
    Reserved = 3,
    Cleaning = 4,
    Maintenance = 5
}

public enum TableStatus
{
    Free = 1,
    Occupied = 2,
    BillRequested = 3,
    Reserved = 4
}

public enum TableShape
{
    Round = 1,
    Square = 2,
    Rectangle = 3
}

public enum OrderStatus
{
    New = 1,
    KitchenPending = 2,
    Cooking = 3,
    Ready = 4,
    Served = 5,
    Paid = 6,
    Cancelled = 7
}

public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    RoomCharge = 3,
    ApplePayGooglePay = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Refunded = 3
}

public enum UserRole
{
    Administrator = 1,
    Waiter = 2,
    Receptionist = 3,
    Chef = 4,
    Storekeeper = 5
}

