using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitalis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "profesionales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "pacientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstudioAdjuntoUrl",
                table: "consultas_medicas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "profesionales");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "EstudioAdjuntoUrl",
                table: "consultas_medicas");
        }
    }
}
