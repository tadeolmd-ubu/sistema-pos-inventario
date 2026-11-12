using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosInventario.Migrations
{
    /// <inheritdoc />
    public partial class AddCostoDetalleVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "costoUnitario",
                table: "DetalleVenta",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            // Las ventas anteriores no guardaban el costo. Se usa el costo
            // actual del producto como mejor aproximación para que la
            // utilidad histórica no quede en cero.
            migrationBuilder.Sql(
                """
                UPDATE "DetalleVenta"
                SET "costoUnitario" = COALESCE((
                    SELECT "costo" FROM "Producto"
                    WHERE "Producto"."id" = "DetalleVenta"."idProducto"
                ), 0);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DetalleVenta_costoUnitario",
                table: "DetalleVenta",
                sql: "costoUnitario >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DetalleVenta_costoUnitario",
                table: "DetalleVenta");

            migrationBuilder.DropColumn(
                name: "costoUnitario",
                table: "DetalleVenta");
        }
    }
}
