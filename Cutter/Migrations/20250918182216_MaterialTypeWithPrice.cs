using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class MaterialTypeWithPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CutPrice",
                table: "Stores");

            /*migrationBuilder.RenameColumn(
                name: "IdMeterialType",
                table: "Items",
                newName: "MaterialTypeId");*/

            migrationBuilder.AddColumn<int>(
                name: "MaterialTypeId",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MaterialType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialName = table.Column<string>(type: "text", nullable: false),
                    Descr = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreMaterialCutPrice",
                columns: table => new
                {
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialTypeId = table.Column<int>(type: "integer", nullable: false),
                    CutPrice = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreMaterialCutPrice", x => new { x.StoreId, x.MaterialTypeId });
                    table.ForeignKey(
                        name: "FK_StoreMaterialCutPrice_MaterialType_MaterialTypeId",
                        column: x => x.MaterialTypeId,
                        principalTable: "MaterialType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreMaterialCutPrice_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.Sql(@"
        INSERT INTO ""MaterialType"" (""Id"", ""MaterialName"", ""Descr"")
        VALUES
            (0, 'Не указан', 'Тип материала не задан'),
            (1, 'Сталь', 'Черная сталь'),
            (2, 'Алюминий', 'Алюминиевый сплав'),
            (3, 'Медь', 'Медные сплавы'),
            (4, 'Пластик', 'Полимерные материалы');
    ");

            migrationBuilder.CreateIndex(
                name: "IX_Items_MaterialTypeId",
                table: "Items",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreMaterialCutPrice_MaterialTypeId",
                table: "StoreMaterialCutPrice",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreMaterialCutPrice_StoreId",
                table: "StoreMaterialCutPrice",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_MaterialType_MaterialTypeId",
                table: "Items",
                column: "MaterialTypeId",
                principalTable: "MaterialType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_MaterialType_MaterialTypeId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "StoreMaterialCutPrice");

            migrationBuilder.DropTable(
                name: "MaterialType");

            migrationBuilder.DropIndex(
                name: "IX_Items_MaterialTypeId",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "MaterialTypeId",
                table: "Items",
                newName: "IdMeterialType");

            migrationBuilder.AddColumn<float>(
                name: "CutPrice",
                table: "Stores",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.DropColumn(
                name: "MaterialTypeId",
                table: "Items");
        }
    }
}
