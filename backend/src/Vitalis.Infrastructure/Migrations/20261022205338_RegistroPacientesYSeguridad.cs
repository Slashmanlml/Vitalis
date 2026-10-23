using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vitalis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegistroPacientesYSeguridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoHasta",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            // Los usuarios que ya existen (todo el personal) se dan por
            // confirmados: los creó el administrador, no el registro público.
            // El default solo se usa para rellenar las filas existentes; el
            // modelo no lo declara, así EF manda siempre el valor explícito al
            // insertar (ver VitalisDbContext).
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmado",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConfirmacionEmail",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidos",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcceso",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "pacientes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tokens_verificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Proposito = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaUso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens_verificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tokens_verificacion_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_UsuarioId",
                table: "pacientes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tokens_verificacion_TokenHash",
                table: "tokens_verificacion",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tokens_verificacion_UsuarioId_Proposito",
                table: "tokens_verificacion",
                columns: new[] { "UsuarioId", "Proposito" });

            migrationBuilder.AddForeignKey(
                name: "FK_pacientes_usuarios_UsuarioId",
                table: "pacientes",
                column: "UsuarioId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Hallazgo de la auditoría de seguridad: la auditoría automática
            // copiaba el PasswordHash de cada usuario creado o editado. El código
            // ya no lo hace; esto limpia lo que quedó guardado de antes.
            migrationBuilder.Sql("""
                UPDATE auditorias
                SET "ValoresNuevos" = regexp_replace("ValoresNuevos", '"PasswordHash":"[^"]*"', '"PasswordHash":"***"', 'g')
                WHERE "ValoresNuevos" LIKE '%"PasswordHash":"$2%';

                UPDATE auditorias
                SET "ValoresAnteriores" = regexp_replace("ValoresAnteriores", '"PasswordHash":"[^"]*"', '"PasswordHash":"***"', 'g')
                WHERE "ValoresAnteriores" LIKE '%"PasswordHash":"$2%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pacientes_usuarios_UsuarioId",
                table: "pacientes");

            migrationBuilder.DropTable(
                name: "tokens_verificacion");

            migrationBuilder.DropIndex(
                name: "IX_pacientes_UsuarioId",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "BloqueadoHasta",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "EmailConfirmado",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacionEmail",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "IntentosFallidos",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "UltimoAcceso",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "pacientes");
        }
    }
}
