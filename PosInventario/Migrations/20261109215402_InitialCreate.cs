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
                name: "Producto",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    sku = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, collation: "NOCASE"),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    precioVenta = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    costo = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    stock = table.Column<int>(type: "INTEGER", nullable: false),
                    stockMinimo = table.Column<int>(type: "INTEGER", nullable: false),
                    fechaArchivado = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    fechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.id);
                    table.CheckConstraint("CK_Producto_costo", "costo >= 0");
                    table.CheckConstraint("CK_Producto_precioVenta", "precioVenta >= 0");
                    table.CheckConstraint("CK_Producto_stock", "stock >= 0");
                    table.CheckConstraint("CK_Producto_stockMinimo", "stockMinimo >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    activo = table.Column<int>(type: "INTEGER", nullable: false),
                    fechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombreUsuario = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    contrasenaHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    activo = table.Column<int>(type: "INTEGER", nullable: false),
                    fechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fechaUltimoAcceso = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Caja",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idUsuarioApertura = table.Column<int>(type: "INTEGER", nullable: false),
                    fechaApertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    montoInicial = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    idUsuarioCierre = table.Column<int>(type: "INTEGER", nullable: true),
                    fechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    montoEsperado = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    montoFinal = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    diferencia = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    estado = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    observaciones = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caja", x => x.id);
                    table.CheckConstraint("CK_Caja_montoEsperado", "montoEsperado IS NULL OR montoEsperado >= 0");
                    table.CheckConstraint("CK_Caja_montoFinal", "montoFinal IS NULL OR montoFinal >= 0");
                    table.CheckConstraint("CK_Caja_montoInicial", "montoInicial >= 0");
                    table.ForeignKey(
                        name: "FK_Caja_Usuario_idUsuarioApertura",
                        column: x => x.idUsuarioApertura,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caja_Usuario_idUsuarioCierre",
                        column: x => x.idUsuarioCierre,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Compra",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idProveedor = table.Column<int>(type: "INTEGER", nullable: false),
                    idUsuario = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    numeroComprobante = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    estado = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    fechaAnulacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    idUsuarioAnulacion = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compra", x => x.id);
                    table.CheckConstraint("CK_Compra_total", "total >= 0");
                    table.ForeignKey(
                        name: "FK_Compra_Proveedor_idProveedor",
                        column: x => x.idProveedor,
                        principalTable: "Proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compra_Usuario_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compra_Usuario_idUsuarioAnulacion",
                        column: x => x.idUsuarioAnulacion,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Venta",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idUsuario = table.Column<int>(type: "INTEGER", nullable: false),
                    idCaja = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    descuento = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    metodoPago = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    montoRecibido = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    vuelto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    estado = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    fechaAnulacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    idUsuarioAnulacion = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta", x => x.id);
                    table.CheckConstraint("CK_Venta_descuento", "descuento >= 0");
                    table.CheckConstraint("CK_Venta_montoRecibido", "montoRecibido >= total");
                    table.CheckConstraint("CK_Venta_subtotal", "subtotal >= 0");
                    table.CheckConstraint("CK_Venta_total", "total >= 0");
                    table.CheckConstraint("CK_Venta_vuelto", "vuelto >= 0");
                    table.ForeignKey(
                        name: "FK_Venta_Caja_idCaja",
                        column: x => x.idCaja,
                        principalTable: "Caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Venta_Usuario_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Venta_Usuario_idUsuarioAnulacion",
                        column: x => x.idUsuarioAnulacion,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleCompra",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idCompra = table.Column<int>(type: "INTEGER", nullable: false),
                    idProducto = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    costoUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleCompra", x => x.id);
                    table.CheckConstraint("CK_DetalleCompra_cantidad", "cantidad > 0");
                    table.CheckConstraint("CK_DetalleCompra_costoUnitario", "costoUnitario >= 0");
                    table.CheckConstraint("CK_DetalleCompra_subtotal", "subtotal >= 0");
                    table.ForeignKey(
                        name: "FK_DetalleCompra_Compra_idCompra",
                        column: x => x.idCompra,
                        principalTable: "Compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetalleCompra_Producto_idProducto",
                        column: x => x.idProducto,
                        principalTable: "Producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleVenta",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idVenta = table.Column<int>(type: "INTEGER", nullable: false),
                    idProducto = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    precioUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleVenta", x => x.id);
                    table.CheckConstraint("CK_DetalleVenta_cantidad", "cantidad > 0");
                    table.CheckConstraint("CK_DetalleVenta_precioUnitario", "precioUnitario >= 0");
                    table.CheckConstraint("CK_DetalleVenta_subtotal", "subtotal >= 0");
                    table.ForeignKey(
                        name: "FK_DetalleVenta_Producto_idProducto",
                        column: x => x.idProducto,
                        principalTable: "Producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetalleVenta_Venta_idVenta",
                        column: x => x.idVenta,
                        principalTable: "Venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientoCaja",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idCaja = table.Column<int>(type: "INTEGER", nullable: false),
                    idUsuario = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    idVenta = table.Column<int>(type: "INTEGER", nullable: true),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientoCaja", x => x.id);
                    table.CheckConstraint("CK_MovimientoCaja_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_MovimientoCaja_Caja_idCaja",
                        column: x => x.idCaja,
                        principalTable: "Caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientoCaja_Usuario_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoCaja_Venta_idVenta",
                        column: x => x.idVenta,
                        principalTable: "Venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientoInventario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idProducto = table.Column<int>(type: "INTEGER", nullable: false),
                    idUsuario = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    stockAnterior = table.Column<int>(type: "INTEGER", nullable: false),
                    stockResultante = table.Column<int>(type: "INTEGER", nullable: false),
                    referencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    idVenta = table.Column<int>(type: "INTEGER", nullable: true),
                    idCompra = table.Column<int>(type: "INTEGER", nullable: true),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientoInventario", x => x.id);
                    table.CheckConstraint("CK_MovimientoInventario_cantidad", "cantidad > 0");
                    table.CheckConstraint("CK_MovimientoInventario_origen", "idVenta IS NULL OR idCompra IS NULL");
                    table.CheckConstraint("CK_MovimientoInventario_stockAnterior", "stockAnterior >= 0");
                    table.CheckConstraint("CK_MovimientoInventario_stockResultante", "stockResultante >= 0");
                    table.ForeignKey(
                        name: "FK_MovimientoInventario_Compra_idCompra",
                        column: x => x.idCompra,
                        principalTable: "Compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoInventario_Producto_idProducto",
                        column: x => x.idProducto,
                        principalTable: "Producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoInventario_Usuario_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoInventario_Venta_idVenta",
                        column: x => x.idVenta,
                        principalTable: "Venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caja_estado",
                table: "Caja",
                column: "estado",
                unique: true,
                filter: "estado = 'Abierta'");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_fechaApertura",
                table: "Caja",
                column: "fechaApertura");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_idUsuarioApertura",
                table: "Caja",
                column: "idUsuarioApertura");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_idUsuarioCierre",
                table: "Caja",
                column: "idUsuarioCierre");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_estado",
                table: "Compra",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_fecha",
                table: "Compra",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_idProveedor",
                table: "Compra",
                column: "idProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_idUsuario",
                table: "Compra",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_idUsuarioAnulacion",
                table: "Compra",
                column: "idUsuarioAnulacion");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCompra_idCompra",
                table: "DetalleCompra",
                column: "idCompra");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCompra_idProducto",
                table: "DetalleCompra",
                column: "idProducto");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_idProducto",
                table: "DetalleVenta",
                column: "idProducto");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_idVenta",
                table: "DetalleVenta",
                column: "idVenta");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoCaja_idCaja_fecha",
                table: "MovimientoCaja",
                columns: new[] { "idCaja", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoCaja_idUsuario",
                table: "MovimientoCaja",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoCaja_idVenta",
                table: "MovimientoCaja",
                column: "idVenta");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_fecha",
                table: "MovimientoInventario",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_idCompra",
                table: "MovimientoInventario",
                column: "idCompra");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_idProducto_fecha",
                table: "MovimientoInventario",
                columns: new[] { "idProducto", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_idUsuario",
                table: "MovimientoInventario",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_idVenta",
                table: "MovimientoInventario",
                column: "idVenta");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_fechaArchivado",
                table: "Producto",
                column: "fechaArchivado");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_nombre",
                table: "Producto",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_sku",
                table: "Producto",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_activo",
                table: "Proveedor",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_nombre",
                table: "Proveedor",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_telefono",
                table: "Proveedor",
                column: "telefono",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_activo",
                table: "Usuario",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_nombreUsuario",
                table: "Usuario",
                column: "nombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_estado",
                table: "Venta",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_fecha",
                table: "Venta",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_idCaja",
                table: "Venta",
                column: "idCaja");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_idUsuario",
                table: "Venta",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_idUsuarioAnulacion",
                table: "Venta",
                column: "idUsuarioAnulacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleCompra");

            migrationBuilder.DropTable(
                name: "DetalleVenta");

            migrationBuilder.DropTable(
                name: "MovimientoCaja");

            migrationBuilder.DropTable(
                name: "MovimientoInventario");

            migrationBuilder.DropTable(
                name: "Compra");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Venta");

            migrationBuilder.DropTable(
                name: "Proveedor");

            migrationBuilder.DropTable(
                name: "Caja");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
