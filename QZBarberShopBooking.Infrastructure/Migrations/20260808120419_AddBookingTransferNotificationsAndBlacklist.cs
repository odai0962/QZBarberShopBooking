using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QZBarberShopBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingTransferNotificationsAndBlacklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.AddColumn<DateTime>(
                name: "BlacklistedAt",
                schema: "identity",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BlacklistedByAdminId",
                schema: "identity",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlacklistedReason",
                schema: "identity",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlacklisted",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "booking",
                table: "Bookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitiatedByUserId",
                schema: "booking",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RescheduleCount",
                schema: "booking",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "booking",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Customer");

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RelatedBookingId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Bookings_RelatedBookingId",
                        column: x => x.RelatedBookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_Blacklisted",
                schema: "identity",
                table: "Users",
                column: "IsBlacklisted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BlacklistedByAdminId",
                schema: "identity",
                table: "Users",
                column: "BlacklistedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_InitiatedByUserId",
                schema: "booking",
                table: "Bookings",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RecipientCreated",
                schema: "notification",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "CreationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RecipientRead",
                schema: "notification",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedBookingId",
                schema: "notification",
                table: "Notifications",
                column: "RelatedBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceToken_Token",
                schema: "notification",
                table: "UserDeviceTokens",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceToken_UserActive",
                schema: "notification",
                table: "UserDeviceTokens",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_InitiatedByUserId",
                schema: "booking",
                table: "Bookings",
                column: "InitiatedByUserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_BlacklistedByAdminId",
                schema: "identity",
                table: "Users",
                column: "BlacklistedByAdminId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_InitiatedByUserId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_BlacklistedByAdminId",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens",
                schema: "notification");

            migrationBuilder.DropIndex(
                name: "IX_User_Blacklisted",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BlacklistedByAdminId",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_InitiatedByUserId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BlacklistedAt",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BlacklistedByAdminId",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BlacklistedReason",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBlacklisted",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "InitiatedByUserId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RescheduleCount",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "booking",
                table: "Bookings");
        }
    }
}
