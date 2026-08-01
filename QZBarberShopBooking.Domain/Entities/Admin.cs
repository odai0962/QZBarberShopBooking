namespace QZBarberShopBooking.Domain.Entities
{
    /// <summary>
    /// A first-class administrator account. Distinct from <see cref="Employee"/> and
    /// <see cref="Customer"/> so Admins can be provisioned like any other user type instead
    /// of only ever existing as a hand-inserted database row.
    /// </summary>
    public class Admin : User
    {
    }
}
