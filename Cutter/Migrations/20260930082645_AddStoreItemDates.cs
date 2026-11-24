using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreItemDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "StoreItems",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StoreItems",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "StoreItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StoreItems");
        }
    }
}
