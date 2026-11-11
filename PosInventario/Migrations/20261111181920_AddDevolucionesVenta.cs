using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosInventario.Migrations
{
    /// <inheritdoc />
    public partial class AddDevolucionesVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "totalDevuelto",
                table: "Venta",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "cantidadDevuelta",
                table: "DetalleVenta",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Venta_totalDevuelto",
                table: "Venta",
                sql: "totalDevuelto >= 0 AND totalDevuelto <= total");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DetalleVenta_cantidadDevuelta",
                table: "DetalleVenta",
                sql: "cantidadDevuelta >= 0 AND cantidadDevuelta <= cantidad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Venta_totalDevuelto",
                table: "Venta");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DetalleVenta_cantidadDevuelta",
                table: "DetalleVenta");

            migrationBuilder.DropColumn(
                name: "totalDevuelto",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "cantidadDevuelta",
                table: "DetalleVenta");
        }
    }
}
