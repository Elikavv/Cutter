using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class New : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Добавляем поле Phone в существующую таблицу Stores
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Stores",
                type: "varchar",
                nullable: false,
                defaultValue: string.Empty);

            // 2. Создаём таблицу StoreItems — с ItemId как int
            migrationBuilder.CreateTable(
                name: "StoreItems",
                columns: table => new
                {
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),  // ← Guid — потому что Store.Id = Guid
                    ItemId = table.Column<int>(type: "integer", nullable: false),                  // ← int — потому что Items.Id = int
                    Price = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinOrderQuantity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreItems", x => new { x.StoreId, x.ItemId });

                    table.ForeignKey(
                        name: "FK_StoreItems_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",        // → ссылается на Stores.Id (Guid)
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_StoreItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",        // → ссылается на Items.Id (int)
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. Индексы
            migrationBuilder.CreateIndex(
                name: "IX_StoreItems_ItemId",
                table: "StoreItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreItems_StoreId",
                table: "StoreItems",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                    name: "StoreItems");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Stores");
        }
    }
}
