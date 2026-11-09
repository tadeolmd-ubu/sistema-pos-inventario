using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosInventario.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    activo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Elemento",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    activo = table.Column<int>(type: "INTEGER", nullable: false),
                    fechaArchivado = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    fechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    idCategoria = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elemento", x => x.id);
                    table.ForeignKey(
                        name: "FK_Elemento_Categoria_idCategoria",
                        column: x => x.idCategoria,
                        principalTable: "Categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categoria",
                columns: new[] { "id", "activo", "descripcion", "nombre" },
                values: new object[] { 1, 1, null, "General" });

            migrationBuilder.CreateIndex(
                name: "IX_Categoria_nombre",
                table: "Categoria",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Elemento_idCategoria",
                table: "Elemento",
                column: "idCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Elemento_nombre",
                table: "Elemento",
                column: "nombre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Elemento");

            migrationBuilder.DropTable(
                name: "Categoria");
        }
    }
}
