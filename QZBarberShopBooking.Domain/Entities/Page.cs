using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Page : TEntity
    {
        public required string Code { get; set; } // e.g. "Appointments"
        public required string PageName { get; set; } // e.g. "Appointments Page"
        public string? Icon { get; set; }
        public required string Url { get; set; } // e.g. "/appointments"
    }
}
