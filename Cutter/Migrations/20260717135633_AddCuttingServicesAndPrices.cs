using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddCuttingServicesAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuttingServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsExtraService = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    modify_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreServicePrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreServicePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreServicePrices_CuttingServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CuttingServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreServicePrices_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuttingServices_IsActive",
                table: "CuttingServices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingServices_IsExtraService",
                table: "CuttingServices",
                column: "IsExtraService");

            migrationBuilder.CreateIndex(
                name: "IX_StoreServicePrices_ServiceId",
                table: "StoreServicePrices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreServicePrices_StoreId_ServiceId",
                table: "StoreServicePrices",
                columns: new[] { "StoreId", "ServiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreServicePrices");

            migrationBuilder.DropTable(
                name: "CuttingServices");
        }
    }
}
