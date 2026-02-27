using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Users
{
    public class UserProfileDto : UserDto
    {
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

}
