using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceMaterialLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultCuttingServiceId",
                table: "MaterialType",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialType_DefaultCuttingServiceId",
                table: "MaterialType",
                column: "DefaultCuttingServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialType_CuttingServices_DefaultCuttingServiceId",
                table: "MaterialType",
                column: "DefaultCuttingServiceId",
                principalTable: "CuttingServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialType_CuttingServices_DefaultCuttingServiceId",
                table: "MaterialType");

            migrationBuilder.DropIndex(
                name: "IX_MaterialType_DefaultCuttingServiceId",
                table: "MaterialType");

            migrationBuilder.DropColumn(
                name: "DefaultCuttingServiceId",
                table: "MaterialType");
        }
    }
}
