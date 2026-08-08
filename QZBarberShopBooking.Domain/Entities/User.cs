using QZBarberShopBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public abstract class User : TEntity, IAuditable, IDeletable
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string PhoneNumber { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Blacklist (Admin-only). Distinct from IsActive/soft-delete so it carries a reason/audit
        // trail; blacklisting always also forces IsActive = false as a side effect.
        public bool IsBlacklisted { get; set; }
        public string? BlacklistedReason { get; set; }
        public DateTime? BlacklistedAt { get; set; }
        public int? BlacklistedByAdminId { get; set; }
        public User? BlacklistedByAdmin { get; set; }

        // Password reset
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public int ResetPasswordAttempts { get; set; }

        // Audit
        public bool IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }

        // Navigation
        public int RoleId { get; set; }
        public UserRole Role { get; set; } = null!;
    }
}
