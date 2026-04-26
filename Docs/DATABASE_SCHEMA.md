# Database Schema & Relational Specifications

This document specifies the PostgreSQL relational schema, Entity Framework Core mappings, constraints, and indexes utilized by the **WinForms-HoReCa-Mastery** system.

---

## 1. Entity-Relationship Diagram

```mermaid
erDiagram
    TABLE_ZONE ||--o{ RESTAURANT_TABLE : "contains"
    RESTAURANT_TABLE ||--o{ ORDER : "assigned to"
    MENU_CATEGORY ||--o{ MENU_ITEM : "categorizes"
    MENU_ITEM ||--o{ ORDER_ITEM : "ordered as"
    ORDER ||--|{ ORDER_ITEM : "contains"
    ORDER ||--o| PAYMENT : "settled by"
    
    ROOM ||--o{ ROOM_BOOKING : "booked in"
    GUEST ||--o{ ROOM_BOOKING : "reserves"
    ROOM_BOOKING ||--o{ ORDER : "room charge"
    ROOM_BOOKING ||--o{ PAYMENT : "settled by"

    TABLE_ZONE {
        int id PK
        string name
        string description
        int display_order
    }

    RESTAURANT_TABLE {
        int id PK
        int zone_id FK
        int table_number
        int capacity
        int status
        int shape
        int pos_x
        int pos_y
        int width
        int height
    }

    MENU_CATEGORY {
        int id PK
        string name
        string icon_key
        int display_order
        bool route_to_kitchen
    }

    MENU_ITEM {
        int id PK
        int category_id FK
        string name
        string description
        decimal price
        int preparation_minutes
        bool is_available
        string image_url
    }

    ORDER {
        int id PK
        int table_id FK
        int booking_id FK
        string order_number
        string waiter_name
        int status
        decimal subtotal
        decimal discount_percent
        decimal total_amount
        datetime created_at
    }

    ORDER_ITEM {
        int id PK
        int order_id FK
        int menu_item_id FK
        string item_name
        int quantity
        decimal unit_price
        decimal total_price
        string modifiers_or_notes
        int status
    }

    GUEST {
        int id PK
        string full_name
        string phone
        string email
        string passport_or_id
    }

    ROOM {
        int id PK
        string room_number
        int floor
        int type
        decimal daily_rate
        int capacity
        int status
    }

    ROOM_BOOKING {
        int id PK
        int room_id FK
        int guest_id FK
        datetime check_in_date
        datetime check_out_date
        decimal total_amount
        bool is_checked_in
        bool is_checked_out
        string special_requests
    }

    INVENTORY_ITEM {
        int id PK
        string name
        string sku
        string unit
        decimal current_stock
        decimal minimum_stock
        decimal cost_per_unit
    }

    PAYMENT {
        int id PK
        int order_id FK
        int booking_id FK
        decimal amount
        int method
        int status
        string transaction_reference
        datetime created_at
    }
```

---

## 2. Table Specifications & Indexes

### Hotel Domain Tables

#### `Guests`
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | Unique guest identifier |
| `FullName` | `varchar(150)` | NOT NULL | Full name of the hotel guest |
| `Phone` | `varchar(30)` | NOT NULL | Contact phone number |
| `Email` | `varchar(100)` | NULL | Electronic mail address |
| `PassportOrId`| `varchar(50)` | NULL | Identification document number |
| `CreatedAt` | `timestamptz` | NOT NULL | Record creation timestamp |

#### `Rooms`
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | Unique room identifier |
| `RoomNumber` | `varchar(20)` | NOT NULL, UNIQUE INDEX | Room identifier number |
| `Floor` | `int` | NOT NULL | Floor location |
| `Type` | `int` | NOT NULL (Enum) | 1=Standard, 2=Comfort, 3=Deluxe, 4=PresidentialSuite |
| `DailyRate` | `decimal(12,2)`| NOT NULL | Price per night |
| `Capacity` | `int` | NOT NULL | Maximum guest capacity |
| `Status` | `int` | NOT NULL (Enum) | 1=Available, 2=Occupied, 3=Reserved, 4=Cleaning, 5=Maintenance |

#### `RoomBookings`
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | Unique reservation identifier |
| `RoomId` | `int` | FK -> `Rooms.Id`, NOT NULL | Reserved hotel room |
| `GuestId` | `int` | FK -> `Guests.Id`, NOT NULL | Booking holder |
| `CheckInDate` | `date` | NOT NULL, INDEX | Arrival date |
| `CheckOutDate`| `date` | NOT NULL, INDEX | Departure date |
| `TotalAmount` | `decimal(12,2)`| NOT NULL | Calculated accommodation cost |
| `IsCheckedIn` | `bool` | NOT NULL | Guest currently physically checked in |
| `IsCheckedOut`| `bool` | NOT NULL | Reservation completed and finalized |
| `SpecialRequests`| `varchar(500)`| NULL | Dietary or accommodation preferences |

---

### Restaurant & POS Tables

#### `TableZones`
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | Zone unique identifier |
| `Name` | `varchar(100)` | NOT NULL | Zone name (e.g., Main Hall, Terrace, VIP Lounge) |
| `Description` | `varchar(250)` | NULL | Location description |
| `DisplayOrder` | `int` | NOT NULL | Sort ordering |

#### `Tables`
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | Table unique identifier |
| `ZoneId` | `int` | FK -> `TableZones.Id` | Assigned zone |
| `TableNumber` | `int` | NOT NULL, INDEX | Physical table number |
| `Capacity` | `int` | NOT NULL | Number of guest seats |
| `Status` | `int` | NOT NULL (Enum) | 1=Free, 2=Occupied, 3=BillRequested, 4=Reserved |
| `Shape` | `int` | NOT NULL (Enum) | 1=Round, 2=Square, 3=Rectangle |
| `PositionX` | `int` | NOT NULL | 2D interactive canvas X coordinate (pixels) |
| `PositionY` | `int` | NOT NULL | 2D interactive canvas Y coordinate (pixels) |
| `Width` | `int` | NOT NULL | Control rendering width |
| `Height` | `int` | NOT NULL | Control rendering height |

#### `Orders` & `OrderItems`
- `Orders.OrderNumber`: Unique human-readable code (e.g., `ORD-2026-0042`) with unique index.
- Precision of `decimal(12,2)` applied across all unit prices, discounts, and line totals.
- Foreign key constraints configured with `DeleteBehavior.Cascade` between `Orders` and `OrderItems` to ensure referential integrity.

---

## 3. Seed Data Specification

The database seeder automatically initializes realistic enterprise mock data if the database is unpopulated:
- **3 Table Zones**: Основний Зал, Літня Тераса, VIP Лаундж.
- **14 Tables**: Placed at configured 2D coordinate positions with round and rectangular shapes.
- **6 Menu Categories**: Сніданки, Перші страви, Основні страви, Десерти, Гарячі напої, Бар.
- **16 Menu Items**: Formatted with prices, prep times, and Ukrainian culinary names.
- **7 Hotel Rooms**: Covering Standard, Comfort, Deluxe, and Presidential Suite tiers.
- **8 Core Inventory Items**: Tracking coffee beans, dairy, meats, spirits, and olive oil with low-stock warnings.
