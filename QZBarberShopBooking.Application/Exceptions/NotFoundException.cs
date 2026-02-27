using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string resourceName, object key)
            : base($"Resource '{resourceName}' with key '{key}' was not found.", 404)
        {
        }

        public NotFoundException(string message) : base(message, 404)
        {
        }
    }
}
