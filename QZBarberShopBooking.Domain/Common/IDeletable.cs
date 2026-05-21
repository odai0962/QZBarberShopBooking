using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Common
{
    public interface IDeletable
    {
        public bool IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
