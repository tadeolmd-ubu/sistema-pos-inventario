using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosInventario.Migrations
{
    /// <inheritdoc />
    public partial class AddPagosVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagoVenta",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idVenta = table.Column<int>(type: "INTEGER", nullable: false),
                    metodo = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    montoDevuelto = table.Column<decimal>(type: "decimal(12,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoVenta", x => x.id);
                    table.CheckConstraint("CK_PagoVenta_metodo", "metodo IN ('Efectivo', 'Tarjeta', 'Transferencia')");
                    table.CheckConstraint("CK_PagoVenta_monto", "monto > 0");
                    table.CheckConstraint("CK_PagoVenta_montoDevuelto", "montoDevuelto >= 0 AND montoDevuelto <= monto");
                    table.ForeignKey(
                        name: "FK_PagoVenta_Venta_idVenta",
                        column: x => x.idVenta,
                        principalTable: "Venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoVenta_idVenta",
                table: "PagoVenta",
                column: "idVenta");

            // Las ventas existentes ya tienen un método y un total: se les crea
            // su fila de pago para que reportes y devoluciones sigan cuadrando.
            migrationBuilder.Sql(
                """
                INSERT INTO "PagoVenta" ("idVenta", "metodo", "monto", "montoDevuelto")
                SELECT "id", "metodoPago", "total", "totalDevuelto"
                FROM "Venta"
                WHERE "total" > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoVenta");
        }
    }
}
