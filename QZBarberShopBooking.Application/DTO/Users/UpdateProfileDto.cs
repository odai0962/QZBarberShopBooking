using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Users
{
    public class UpdateProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }

        /// <summary>Only applies to Customer profiles — Employees have no date of birth field.</summary>
        public DateTime? DateOfBirth { get; set; }
    }
}
