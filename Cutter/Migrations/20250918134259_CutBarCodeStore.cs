using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class CutBarCodeStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CutBarCode",
                table: "Stores",
                type: "varchar",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlan_UserId",
                table: "CuttingPlan",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingPlan_AspNetUsers_UserId",
                table: "CuttingPlan",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingPlan_AspNetUsers_UserId",
                table: "CuttingPlan");

            migrationBuilder.DropIndex(
                name: "IX_CuttingPlan_UserId",
                table: "CuttingPlan");

            migrationBuilder.DropColumn(
                name: "CutBarCode",
                table: "Stores");
        }
    }
}
