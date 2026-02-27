using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Exceptions
{
    public class BusinessRuleException : AppException
    {
        public BusinessRuleException(string message) : base(message, 422)
        {
        }
    }
}
