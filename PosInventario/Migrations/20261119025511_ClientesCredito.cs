using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosInventario.Migrations
{
    /// <inheritdoc />
    public partial class ClientesCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idCliente",
                table: "Venta",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipoVenta",
                table: "Venta",
                type: "TEXT",
                maxLength: 15,
                nullable: false,
                defaultValue: "Contado");

            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    limiteCredito = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    activo = table.Column<int>(type: "INTEGER", nullable: false),
                    fechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.id);
                    table.CheckConstraint("CK_Cliente_limiteCredito", "limiteCredito >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Abono",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    idCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    idVenta = table.Column<int>(type: "INTEGER", nullable: false),
                    monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    metodo = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    referencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    idUsuario = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abono", x => x.id);
                    table.CheckConstraint("CK_Abono_metodo", "metodo IN ('Efectivo', 'Tarjeta', 'Transferencia')");
                    table.CheckConstraint("CK_Abono_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_Abono_Cliente_idCliente",
                        column: x => x.idCliente,
                        principalTable: "Cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abono_Usuario_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abono_Venta_idVenta",
                        column: x => x.idVenta,
                        principalTable: "Venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Venta_idCliente",
                table: "Venta",
                column: "idCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_tipoVenta",
                table: "Venta",
                column: "tipoVenta");

            migrationBuilder.CreateIndex(
                name: "IX_Abono_idCliente_fecha",
                table: "Abono",
                columns: new[] { "idCliente", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Abono_idUsuario",
                table: "Abono",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Abono_idVenta",
                table: "Abono",
                column: "idVenta");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_activo",
                table: "Cliente",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_nombre",
                table: "Cliente",
                column: "nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_Venta_Cliente_idCliente",
                table: "Venta",
                column: "idCliente",
                principalTable: "Cliente",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venta_Cliente_idCliente",
                table: "Venta");

            migrationBuilder.DropTable(
                name: "Abono");

            migrationBuilder.DropTable(
                name: "Cliente");

            migrationBuilder.DropIndex(
                name: "IX_Venta_idCliente",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_tipoVenta",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "idCliente",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "tipoVenta",
                table: "Venta");
        }
    }
}
