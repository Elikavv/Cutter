using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerStoresTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerStores",
                columns: table => new
                {
                    ManagerId = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerStores", x => new { x.ManagerId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_ManagerStores_AspNetUsers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManagerStores_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerStores_StoreId",
                table: "ManagerStores",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerStores");
        }
    }
}
