using HoReCa.Domain.Common;
using HoReCa.Domain.Enums;

namespace HoReCa.Domain.Entities;

public class UserAccount : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Waiter;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}
