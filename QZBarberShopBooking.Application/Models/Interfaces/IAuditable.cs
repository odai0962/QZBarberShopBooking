using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Models.Interfaces
{
    public interface IAuditable
    {
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }
    }
}
