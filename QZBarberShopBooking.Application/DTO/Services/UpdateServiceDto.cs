using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Services
{
    public class UpdateServiceDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public TimeSpan? DefaultDuration { get; set; }
        public decimal? BasePrice { get; set; }
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
    }
}
