using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cutter.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreSubdepartmentId",
                table: "StoreItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ImportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessingTimeMs = table.Column<int>(type: "integer", nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    CreatedItems = table.Column<int>(type: "integer", nullable: false),
                    UpdatedItems = table.Column<int>(type: "integer", nullable: false),
                    CreatedDepartments = table.Column<int>(type: "integer", nullable: false),
                    CreatedSubdepartments = table.Column<int>(type: "integer", nullable: false),
                    SkippedRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorsCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImportLogs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreDepartments_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreSubdepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreDepartmentId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSubdepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSubdepartments_StoreDepartments_StoreDepartmentId",
                        column: x => x.StoreDepartmentId,
                        principalTable: "StoreDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreItems_StoreSubdepartmentId",
                table: "StoreItems",
                column: "StoreSubdepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_ImportDate",
                table: "ImportLogs",
                column: "ImportDate");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_StoreId",
                table: "ImportLogs",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_UserId",
                table: "ImportLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreDepartments_StoreId_Code",
                table: "StoreDepartments",
                columns: new[] { "StoreId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSubdepartments_StoreDepartmentId_Code",
                table: "StoreSubdepartments",
                columns: new[] { "StoreDepartmentId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreItems_StoreSubdepartments_StoreSubdepartmentId",
                table: "StoreItems",
                column: "StoreSubdepartmentId",
                principalTable: "StoreSubdepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreItems_StoreSubdepartments_StoreSubdepartmentId",
                table: "StoreItems");

            migrationBuilder.DropTable(
                name: "ImportLogs");

            migrationBuilder.DropTable(
                name: "StoreSubdepartments");

            migrationBuilder.DropTable(
                name: "StoreDepartments");

            migrationBuilder.DropIndex(
                name: "IX_StoreItems_StoreSubdepartmentId",
                table: "StoreItems");

            migrationBuilder.DropColumn(
                name: "StoreSubdepartmentId",
                table: "StoreItems");
        }
    }
}
