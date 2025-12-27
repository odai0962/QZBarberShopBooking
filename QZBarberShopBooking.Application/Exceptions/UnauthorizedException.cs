using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Exceptions
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(message, 401)
        {
        }
    }
}
