using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddModifierToCutPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "modifier_id",
                table: "CuttingPlan",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlan_modifier_id",
                table: "CuttingPlan",
                column: "modifier_id");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingPlan_AspNetUsers_modifier_id",
                table: "CuttingPlan",
                column: "modifier_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingPlan_AspNetUsers_modifier_id",
                table: "CuttingPlan");

            migrationBuilder.DropIndex(
                name: "IX_CuttingPlan_modifier_id",
                table: "CuttingPlan");

            migrationBuilder.DropColumn(
                name: "modifier_id",
                table: "CuttingPlan");
        }
    }
}
