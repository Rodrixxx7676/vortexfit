using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VortexFit.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SOCIOS",
                columns: table => new
                {
                    ID_SOCIO = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOMBRE_COMPLETO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    TELEFONO = table.Column<string>(type: "NVARCHAR2(15)", maxLength: 15, nullable: true),
                    PASSWORD_HASH = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    PLAN = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    ESTADO = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false, defaultValue: "Activo"),
                    FECHA_REGISTRO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE"),
                    FECHA_VENCIMIENTO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SOCIOS", x => x.ID_SOCIO);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_SOCIOS_EMAIL",
                table: "SOCIOS",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SOCIOS");
        }
    }
}
