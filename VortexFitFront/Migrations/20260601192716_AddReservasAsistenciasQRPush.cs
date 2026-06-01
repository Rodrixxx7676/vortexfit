using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VortexFit.Migrations
{
    /// <inheritdoc />
    public partial class AddReservasAsistenciasQRPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Oracle: empty string == NULL, so add nullable first, fill rows, then set NOT NULL
            migrationBuilder.AddColumn<string>(
                name: "CODIGO_ACCESO",
                table: "SOCIOS",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: null);

            migrationBuilder.Sql(
                @"UPDATE ""SOCIOS"" SET ""CODIGO_ACCESO"" = " +
                @"UPPER(SUBSTR(RAWTOHEX(SYS_GUID()), 1, 12)) " +
                @"WHERE ""CODIGO_ACCESO"" IS NULL");

            migrationBuilder.Sql(
                @"ALTER TABLE ""SOCIOS"" MODIFY ""CODIGO_ACCESO"" NVARCHAR2(20) NOT NULL");

            migrationBuilder.CreateTable(
                name: "ASISTENCIAS",
                columns: table => new
                {
                    ID_ASISTENCIA = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_SOCIO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FECHA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE"),
                    NOMBRE_CLASE = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    TIPO = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASISTENCIAS", x => x.ID_ASISTENCIA);
                    table.ForeignKey(
                        name: "FK_ASISTENCIAS_SOCIOS_ID_SOCIO",
                        column: x => x.ID_SOCIO,
                        principalTable: "SOCIOS",
                        principalColumn: "ID_SOCIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PUSH_SUSCRIPCIONES",
                columns: table => new
                {
                    ID_SUSCRIPCION = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_SOCIO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ENDPOINT = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    P256DH = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    AUTH_KEY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    FECHA_REGISTRO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUSH_SUSCRIPCIONES", x => x.ID_SUSCRIPCION);
                    table.ForeignKey(
                        name: "FK_PUSH_SUSCRIPCIONES_SOCIOS_ID_SOCIO",
                        column: x => x.ID_SOCIO,
                        principalTable: "SOCIOS",
                        principalColumn: "ID_SOCIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RESERVAS",
                columns: table => new
                {
                    ID_RESERVA = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_SOCIO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NOMBRE_CLASE = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DIA = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    HORA = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    INSTRUCTOR = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    FECHA_RESERVA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE"),
                    ESTADO = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false, defaultValue: "Confirmada")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESERVAS", x => x.ID_RESERVA);
                    table.ForeignKey(
                        name: "FK_RESERVAS_SOCIOS_ID_SOCIO",
                        column: x => x.ID_SOCIO,
                        principalTable: "SOCIOS",
                        principalColumn: "ID_SOCIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ASISTENCIAS_ID_SOCIO",
                table: "ASISTENCIAS",
                column: "ID_SOCIO");

            migrationBuilder.CreateIndex(
                name: "IX_PUSH_SUSCRIPCIONES_ID_SOCIO",
                table: "PUSH_SUSCRIPCIONES",
                column: "ID_SOCIO");

            migrationBuilder.CreateIndex(
                name: "UQ_PUSH_ENDPOINT",
                table: "PUSH_SUSCRIPCIONES",
                column: "ENDPOINT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RESERVAS_ID_SOCIO",
                table: "RESERVAS",
                column: "ID_SOCIO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASISTENCIAS");

            migrationBuilder.DropTable(
                name: "PUSH_SUSCRIPCIONES");

            migrationBuilder.DropTable(
                name: "RESERVAS");

            migrationBuilder.DropColumn(
                name: "CODIGO_ACCESO",
                table: "SOCIOS");
        }
    }
}
