using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vitalis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObrasSocialesEspecialidadesProfesionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObraSocial",
                table: "pacientes");

            migrationBuilder.AddColumn<int>(
                name: "ObraSocialId",
                table: "pacientes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "especialidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especialidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "obras_sociales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obras_sociales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "profesionales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Matricula = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EspecialidadId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profesionales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profesionales_especialidades_EspecialidadId",
                        column: x => x.EspecialidadId,
                        principalTable: "especialidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_profesionales_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_ObraSocialId",
                table: "pacientes",
                column: "ObraSocialId");

            migrationBuilder.CreateIndex(
                name: "IX_obras_sociales_Codigo",
                table: "obras_sociales",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profesionales_EspecialidadId",
                table: "profesionales",
                column: "EspecialidadId");

            migrationBuilder.CreateIndex(
                name: "IX_profesionales_Matricula",
                table: "profesionales",
                column: "Matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profesionales_UsuarioId",
                table: "profesionales",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_pacientes_obras_sociales_ObraSocialId",
                table: "pacientes",
                column: "ObraSocialId",
                principalTable: "obras_sociales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pacientes_obras_sociales_ObraSocialId",
                table: "pacientes");

            migrationBuilder.DropTable(
                name: "obras_sociales");

            migrationBuilder.DropTable(
                name: "profesionales");

            migrationBuilder.DropTable(
                name: "especialidades");

            migrationBuilder.DropIndex(
                name: "IX_pacientes_ObraSocialId",
                table: "pacientes");

            migrationBuilder.DropColumn(
                name: "ObraSocialId",
                table: "pacientes");

            migrationBuilder.AddColumn<string>(
                name: "ObraSocial",
                table: "pacientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
