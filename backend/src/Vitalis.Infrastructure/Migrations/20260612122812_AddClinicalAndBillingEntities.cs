using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vitalis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalAndBillingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "turnos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Solicitado");

            migrationBuilder.CreateTable(
                name: "alergias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    Sustancia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reaccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Severidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alergias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alergias_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antecedentes_clinicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antecedentes_clinicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_antecedentes_clinicos_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consultas_medicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    ProfesionalId = table.Column<int>(type: "integer", nullable: false),
                    TurnoId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MotivoConsulta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Diagnostico = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Evolucion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Indicaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultas_medicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consultas_medicas_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consultas_medicas_profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consultas_medicas_turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pendiente"),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_facturas_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "liquidaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfesionalId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoDesde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodoHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pendiente"),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liquidaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_liquidaciones_profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medicamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Presentacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prestaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImporteBase = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prestaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prescripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsultaMedicaId = table.Column<int>(type: "integer", nullable: false),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    ProfesionalId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prescripciones_consultas_medicas_ConsultaMedicaId",
                        column: x => x.ConsultaMedicaId,
                        principalTable: "consultas_medicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prescripciones_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prescripciones_profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MedioPago = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagos_facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "factura_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    PrestacionId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factura_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_factura_detalles_facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_factura_detalles_prestaciones_PrestacionId",
                        column: x => x.PrestacionId,
                        principalTable: "prestaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescripcion_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PrescripcionId = table.Column<int>(type: "integer", nullable: false),
                    MedicamentoId = table.Column<int>(type: "integer", nullable: false),
                    Dosis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Frecuencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Duracion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Indicaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescripcion_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prescripcion_detalles_medicamentos_MedicamentoId",
                        column: x => x.MedicamentoId,
                        principalTable: "medicamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prescripcion_detalles_prescripciones_PrescripcionId",
                        column: x => x.PrescripcionId,
                        principalTable: "prescripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alergias_PacienteId",
                table: "alergias",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_antecedentes_clinicos_PacienteId",
                table: "antecedentes_clinicos",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_medicas_PacienteId",
                table: "consultas_medicas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_medicas_ProfesionalId",
                table: "consultas_medicas",
                column: "ProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_medicas_TurnoId",
                table: "consultas_medicas",
                column: "TurnoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_factura_detalles_FacturaId",
                table: "factura_detalles",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_factura_detalles_PrestacionId",
                table: "factura_detalles",
                column: "PrestacionId");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_PacienteId",
                table: "facturas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_ProfesionalId",
                table: "liquidaciones",
                column: "ProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_FacturaId",
                table: "pagos",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_prescripcion_detalles_MedicamentoId",
                table: "prescripcion_detalles",
                column: "MedicamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_prescripcion_detalles_PrescripcionId",
                table: "prescripcion_detalles",
                column: "PrescripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_prescripciones_ConsultaMedicaId",
                table: "prescripciones",
                column: "ConsultaMedicaId");

            migrationBuilder.CreateIndex(
                name: "IX_prescripciones_PacienteId",
                table: "prescripciones",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_prescripciones_ProfesionalId",
                table: "prescripciones",
                column: "ProfesionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alergias");

            migrationBuilder.DropTable(
                name: "antecedentes_clinicos");

            migrationBuilder.DropTable(
                name: "factura_detalles");

            migrationBuilder.DropTable(
                name: "liquidaciones");

            migrationBuilder.DropTable(
                name: "pagos");

            migrationBuilder.DropTable(
                name: "prescripcion_detalles");

            migrationBuilder.DropTable(
                name: "prestaciones");

            migrationBuilder.DropTable(
                name: "facturas");

            migrationBuilder.DropTable(
                name: "medicamentos");

            migrationBuilder.DropTable(
                name: "prescripciones");

            migrationBuilder.DropTable(
                name: "consultas_medicas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "turnos");
        }
    }
}
