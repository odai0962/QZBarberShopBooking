using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Exceptions
{
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message, 409)
        {
        }
    }
}
