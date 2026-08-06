using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QZBarberShopBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePhotoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                schema: "identity",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                schema: "identity",
                table: "Users");
        }
    }
}
