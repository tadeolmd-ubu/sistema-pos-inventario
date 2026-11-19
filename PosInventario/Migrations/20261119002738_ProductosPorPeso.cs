using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosInventario.Migrations
{
    /// <inheritdoc />
    public partial class ProductosPorPeso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "unidadMedida",
                table: "Producto",
                type: "TEXT",
                maxLength: 15,
                nullable: false,
                defaultValue: "Pieza");

            migrationBuilder.AlterColumn<long>(
                name: "cantidadDevuelta",
                table: "DetalleVenta",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            // Las cantidades existentes estaban en unidades enteras. A partir
            // de esta migración se guardan en milésimas para admitir pesos.
            migrationBuilder.Sql(
                "UPDATE Producto SET stock = stock * 1000, " +
                "stockMinimo = stockMinimo * 1000;");
            migrationBuilder.Sql(
                "UPDATE DetalleVenta SET cantidad = cantidad * 1000, " +
                "cantidadDevuelta = cantidadDevuelta * 1000;");
            migrationBuilder.Sql(
                "UPDATE DetalleCompra SET cantidad = cantidad * 1000;");
            migrationBuilder.Sql(
                "UPDATE MovimientoInventario SET cantidad = cantidad * 1000, " +
                "stockAnterior = stockAnterior * 1000, " +
                "stockResultante = stockResultante * 1000;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Producto SET stock = stock / 1000, " +
                "stockMinimo = stockMinimo / 1000;");
            migrationBuilder.Sql(
                "UPDATE DetalleVenta SET cantidad = cantidad / 1000, " +
                "cantidadDevuelta = cantidadDevuelta / 1000;");
            migrationBuilder.Sql(
                "UPDATE DetalleCompra SET cantidad = cantidad / 1000;");
            migrationBuilder.Sql(
                "UPDATE MovimientoInventario SET cantidad = cantidad / 1000, " +
                "stockAnterior = stockAnterior / 1000, " +
                "stockResultante = stockResultante / 1000;");

            migrationBuilder.DropColumn(
                name: "unidadMedida",
                table: "Producto");

            migrationBuilder.AlterColumn<int>(
                name: "cantidadDevuelta",
                table: "DetalleVenta",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldDefaultValue: 0L);
        }
    }
}
