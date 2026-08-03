using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QZBarberShopBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagePermissionAndPagePlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagePermissions",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagePermissions", x => x.Id);
                    table.UniqueConstraint("AK_PagePermissions_PageId_PermissionId", x => new { x.PageId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_PagePermissions_Pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "system",
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagePlatforms",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagePlatforms", x => x.Id);
                    table.UniqueConstraint("AK_PagePlatforms_PageId_Platform", x => new { x.PageId, x.Platform });
                    table.ForeignKey(
                        name: "FK_PagePlatforms_Pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "system",
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagePermission_Page",
                schema: "system",
                table: "PagePermissions",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_PagePermission_Permission",
                schema: "system",
                table: "PagePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PagePlatform_Page",
                schema: "system",
                table: "PagePlatforms",
                column: "PageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagePermissions",
                schema: "system");

            migrationBuilder.DropTable(
                name: "PagePlatforms",
                schema: "system");
        }
    }
}
