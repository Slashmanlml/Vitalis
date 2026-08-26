using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitalis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnriquecerEmailLogParaAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "email_logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Evento",
                table: "email_logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MensajeError",
                table: "email_logs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "email_logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TurnoId",
                table: "email_logs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_TurnoId_Evento",
                table: "email_logs",
                columns: new[] { "TurnoId", "Evento" });

            migrationBuilder.AddForeignKey(
                name: "FK_email_logs_turnos_TurnoId",
                table: "email_logs",
                column: "TurnoId",
                principalTable: "turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_logs_turnos_TurnoId",
                table: "email_logs");

            migrationBuilder.DropIndex(
                name: "IX_email_logs_TurnoId_Evento",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "Evento",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "MensajeError",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "TurnoId",
                table: "email_logs");
        }
    }
}
