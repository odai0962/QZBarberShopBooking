using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class RolePermission : TEntity
    {
        public int PermissionId { get; set; }
        public Permission Permission { get; set; }
        public int RoleId { get; set; }
        public UserRole Role { get; set; }
    }
}
