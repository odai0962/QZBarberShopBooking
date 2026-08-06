using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Page : TEntity
    {
        public required string Code { get; set; } // e.g. "Appointments"
        public required string PageNameEn { get; set; } // e.g. "Appointments"
        public required string PageNameAr { get; set; } // e.g. "المواعيد"
        public string? Icon { get; set; }
        public required string Url { get; set; } // e.g. "/appointments"

        public ICollection<PagePermission> PagePermissions { get; set; } = new List<PagePermission>();
        public ICollection<PagePlatform> PagePlatforms { get; set; } = new List<PagePlatform>();
    }
}
