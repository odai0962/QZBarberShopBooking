using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QZBarberShopBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingService_TimeSlot",
                schema: "booking",
                table: "BookingServices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BookingService_TimeSlot",
                schema: "booking",
                table: "BookingServices",
                column: "TimeSlotId",
                unique: true);
        }
    }
}
