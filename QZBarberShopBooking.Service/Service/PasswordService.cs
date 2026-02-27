using Microsoft.AspNetCore.Identity;
using QZBarberShopBooking.Service.DI.DIType;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Service.Service
{
    public class PasswordService : IScopedService
    {
        private readonly PasswordHasher<object> _passwordHasher = new();

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
