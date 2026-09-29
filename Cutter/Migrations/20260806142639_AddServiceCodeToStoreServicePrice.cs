using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCodeToStoreServicePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Price",
                table: "StoreServicePrices",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "ServiceCode",
                table: "StoreServicePrices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceCode",
                table: "StoreServicePrices");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "StoreServicePrices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");
        }
    }
}
