using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VortexFit.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NOTICIAS",
                columns: table => new
                {
                    ID_NOTICIA = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TITULO = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    RESUMEN = table.Column<string>(type: "NVARCHAR2(400)", maxLength: 400, nullable: false),
                    CONTENIDO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CATEGORIA = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    FECHA_PUBLICACION = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE"),
                    ACTIVO = table.Column<bool>(type: "BOOLEAN", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTICIAS", x => x.ID_NOTICIA);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NOTICIAS");
        }
    }
}
