using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Permission : TEntity
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<PagePermission> PagePermissions { get; set; } = new List<PagePermission>();
    }
}
