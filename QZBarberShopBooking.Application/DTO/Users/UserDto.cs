using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Users
{
    public class UserDto
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }

        public required string FirstName { get; set; }
        public required string LastName { get; set; }

        public required string PhoneNumber { get; set; }
        public required string Role { get; set; }
        public bool IsActive { get; set; }
    }
}
