using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QZBarberShopBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageNameLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PageName",
                schema: "system",
                table: "Pages",
                newName: "PageNameEn");

            migrationBuilder.AddColumn<string>(
                name: "PageNameAr",
                schema: "system",
                table: "Pages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Backfill Arabic names for pages seeded before this migration existed — DatabaseSeeder
            // only inserts rows once (short-circuits if any Page row exists), so existing dev/prod
            // databases need this data migration rather than relying on the seeder to fill it in.
            migrationBuilder.Sql("""
                UPDATE [system].[Pages] SET [PageNameAr] = N'الحجوزات' WHERE [Code] = 'Bookings';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الموظفين' WHERE [Code] = 'Employees';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الخدمات' WHERE [Code] = 'Services';
                UPDATE [system].[Pages] SET [PageNameAr] = N'المستخدمين' WHERE [Code] = 'Users';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الملف الشخصي' WHERE [Code] = 'Profile';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الرئيسية' WHERE [Code] = 'Home';
                UPDATE [system].[Pages] SET [PageNameAr] = N'لوحة التحكم' WHERE [Code] = 'Dashboard';
                UPDATE [system].[Pages] SET [PageNameAr] = N'المواعيد' WHERE [Code] = 'Appointments';
                UPDATE [system].[Pages] SET [PageNameAr] = N'العملاء' WHERE [Code] = 'Customers';
                UPDATE [system].[Pages] SET [PageNameAr] = N'ساعات العمل' WHERE [Code] = 'WorkingHours';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الإجازات' WHERE [Code] = 'TimeOff';
                UPDATE [system].[Pages] SET [PageNameAr] = N'المعرض' WHERE [Code] = 'Gallery';
                UPDATE [system].[Pages] SET [PageNameAr] = N'نظرة عامة على المحل' WHERE [Code] = 'ShopOverview';
                UPDATE [system].[Pages] SET [PageNameAr] = N'إعدادات المحل' WHERE [Code] = 'ShopSettings';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الإعدادات' WHERE [Code] = 'Settings';
                UPDATE [system].[Pages] SET [PageNameAr] = N'الإشعارات' WHERE [Code] = 'Notifications';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageNameAr",
                schema: "system",
                table: "Pages");

            migrationBuilder.RenameColumn(
                name: "PageNameEn",
                schema: "system",
                table: "Pages",
                newName: "PageName");
        }
    }
}
