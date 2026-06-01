using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VortexFit.Migrations
{
    /// <inheritdoc />
    public partial class AddRolToSocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ROL",
                table: "SOCIOS",
                type: "NVARCHAR2(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ROL",
                table: "SOCIOS");
        }
    }
}
