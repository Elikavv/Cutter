using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddCuttingConstraintProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuttingConstraintProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    BladeWidth = table.Column<double>(type: "double precision", nullable: false),
                    MarginTop = table.Column<double>(type: "double precision", nullable: false),
                    MarginBottom = table.Column<double>(type: "double precision", nullable: false),
                    MarginLeft = table.Column<double>(type: "double precision", nullable: false),
                    MarginRight = table.Column<double>(type: "double precision", nullable: false),
                    MinDistanceBetweenParts = table.Column<double>(type: "double precision", nullable: false),
                    CanRotateParts = table.Column<bool>(type: "boolean", nullable: false),
                    MaxCutLength = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingConstraintProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuttingConstraintProfiles_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuttingConstraintProfiles_StoreId",
                table: "CuttingConstraintProfiles",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuttingConstraintProfiles");
        }
    }
}
