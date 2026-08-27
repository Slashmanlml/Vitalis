using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Domain.Entities;

namespace Vitalis.Infrastructure.Data;

public class VitalisDbContext(DbContextOptions<VitalisDbContext> options, IHttpContextAccessor httpContextAccessor) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Profesional> Profesionales => Set<Profesional>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<ConsultaMedica> ConsultasMedicas => Set<ConsultaMedica>();
    public DbSet<AntecedenteClinico> AntecedentesClinicos => Set<AntecedenteClinico>();
    public DbSet<Alergia> Alergias => Set<Alergia>();
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Prescripcion> Prescripciones => Set<Prescripcion>();
    public DbSet<PrescripcionDetalle> PrescripcionDetalles => Set<PrescripcionDetalle>();
    public DbSet<Prestacion> Prestaciones => Set<Prestacion>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<BloqueoAgenda> BloqueosAgenda => Set<BloqueoAgenda>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Descripcion).HasMaxLength(200);
            entity.HasIndex(r => r.Nombre).IsUnique();
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Apellido).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ObraSocial>(entity =>
        {
            entity.ToTable("obras_sociales");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(o => o.Codigo).HasMaxLength(50).IsRequired();
            entity.HasIndex(o => o.Codigo).IsUnique();
        });

        modelBuilder.Entity<Especialidad>(entity =>
        {
            entity.ToTable("especialidades");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
        });

        modelBuilder.Entity<Profesional>(entity =>
        {
            entity.ToTable("profesionales");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Apellido).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Matricula).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Email).HasMaxLength(150);
            entity.Property(p => p.Telefono).HasMaxLength(50);
            entity.HasIndex(p => p.Matricula).IsUnique();
            entity.HasOne(p => p.Especialidad)
                .WithMany()
                .HasForeignKey(p => p.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.ToTable("pacientes");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Apellido).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Dni).HasMaxLength(20).IsRequired();
            entity.Property(p => p.Telefono).HasMaxLength(50);
            entity.Property(p => p.Email).HasMaxLength(150);
            entity.Property(p => p.Direccion).HasMaxLength(200);
            entity.Property(p => p.NumeroAfiliado).HasMaxLength(50);
            entity.HasIndex(p => p.Dni).IsUnique();
            entity.HasOne(p => p.ObraSocial)
                .WithMany()
                .HasForeignKey(p => p.ObraSocialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.ToTable("turnos");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.FechaHora).IsRequired();
            entity.Property(t => t.Confirmado).IsRequired();
            entity.Property(t => t.Estado).HasMaxLength(50).HasDefaultValue("Solicitado");
            entity.HasOne(t => t.Paciente)
                .WithMany()
                .HasForeignKey(t => t.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Profesional)
                .WithMany()
                .HasForeignKey(t => t.ProfesionalId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.ObraSocial)
                .WithMany()
                .HasForeignKey(t => t.ObraSocialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConsultaMedica>(entity =>
        {
            entity.ToTable("consultas_medicas");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.MotivoConsulta).HasMaxLength(500).IsRequired();
            entity.Property(c => c.Diagnostico).HasMaxLength(1000);
            entity.Property(c => c.Evolucion).HasMaxLength(2000);
            entity.Property(c => c.Indicaciones).HasMaxLength(2000);
            entity.Property(c => c.Observaciones).HasMaxLength(2000);
            entity.HasOne(c => c.Paciente)
                .WithMany()
                .HasForeignKey(c => c.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Profesional)
                .WithMany()
                .HasForeignKey(c => c.ProfesionalId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Turno)
                .WithOne(t => t.ConsultaMedica)
                .HasForeignKey<ConsultaMedica>(c => c.TurnoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AntecedenteClinico>(entity =>
        {
            entity.ToTable("antecedentes_clinicos");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Tipo).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Descripcion).HasMaxLength(500).IsRequired();
            entity.HasOne(a => a.Paciente)
                .WithMany()
                .HasForeignKey(a => a.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alergia>(entity =>
        {
            entity.ToTable("alergias");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Sustancia).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Reaccion).HasMaxLength(500);
            entity.Property(a => a.Severidad).HasMaxLength(50);
            entity.HasOne(a => a.Paciente)
                .WithMany()
                .HasForeignKey(a => a.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Medicamento>(entity =>
        {
            entity.ToTable("medicamentos");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Presentacion).HasMaxLength(200);
        });

        modelBuilder.Entity<Prescripcion>(entity =>
        {
            entity.ToTable("prescripciones");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Observaciones).HasMaxLength(1000);
            entity.HasOne(p => p.ConsultaMedica)
                .WithMany()
                .HasForeignKey(p => p.ConsultaMedicaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.Paciente)
                .WithMany()
                .HasForeignKey(p => p.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.Profesional)
                .WithMany()
                .HasForeignKey(p => p.ProfesionalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrescripcionDetalle>(entity =>
        {
            entity.ToTable("prescripcion_detalles");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Dosis).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Frecuencia).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Duracion).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Indicaciones).HasMaxLength(500);
            entity.HasOne(p => p.Prescripcion)
                .WithMany(p => p.Detalles)
                .HasForeignKey(p => p.PrescripcionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Medicamento)
                .WithMany()
                .HasForeignKey(p => p.MedicamentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Prestacion>(entity =>
        {
            entity.ToTable("prestaciones");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(p => p.ImporteBase).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.ToTable("facturas");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Total).HasColumnType("decimal(10,2)");
            entity.Property(f => f.Estado).HasMaxLength(50).HasDefaultValue("Pendiente");
            entity.Property(f => f.Observaciones).HasMaxLength(500);
            entity.HasOne(f => f.Paciente)
                .WithMany()
                .HasForeignKey(f => f.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FacturaDetalle>(entity =>
        {
            entity.ToTable("factura_detalles");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.PrecioUnitario).HasColumnType("decimal(10,2)");
            entity.Property(f => f.Subtotal).HasColumnType("decimal(10,2)");
            entity.HasOne(f => f.Factura)
                .WithMany(f => f.Detalles)
                .HasForeignKey(f => f.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.Prestacion)
                .WithMany()
                .HasForeignKey(f => f.PrestacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("pagos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.MedioPago).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Importe).HasColumnType("decimal(10,2)");
            entity.Property(p => p.Observaciones).HasMaxLength(500);
            entity.HasOne(p => p.Factura)
                .WithMany(f => f.Pagos)
                .HasForeignKey(p => p.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Liquidacion>(entity =>
        {
            entity.ToTable("liquidaciones");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Total).HasColumnType("decimal(10,2)");
            entity.Property(l => l.Estado).HasMaxLength(50).HasDefaultValue("Pendiente");
            entity.HasOne(l => l.Profesional)
                .WithMany()
                .HasForeignKey(l => l.ProfesionalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.ToTable("auditorias");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Accion).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Tabla).HasMaxLength(100).IsRequired();
            entity.Property(a => a.ClavePrimaria).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<BloqueoAgenda>(entity =>
        {
            entity.ToTable("bloqueos_agenda");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.FechaHoraInicio).IsRequired();
            entity.Property(b => b.FechaHoraFin).IsRequired();
            entity.Property(b => b.Motivo).HasMaxLength(250);
            entity.HasOne(b => b.Profesional)
                .WithMany()
                .HasForeignKey(b => b.ProfesionalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("email_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Destinatario).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Asunto).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Cuerpo).IsRequired();
            entity.Property(e => e.FechaEnvio).IsRequired();

            entity.Property(e => e.Origen).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Evento).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(40).IsRequired();
            entity.Property(e => e.MensajeError).HasMaxLength(1000);

            entity.HasOne(e => e.Turno)
                .WithMany()
                .HasForeignKey(e => e.TurnoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TurnoId, e.Evento });
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Auditoria || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry);
            auditEntry.Tabla = entry.Metadata.GetTableName() ?? entry.Metadata.Name;
            auditEntries.Add(auditEntry);

            if (entry.State == EntityState.Added)
            {
                auditEntry.Accion = "CREAR";
                auditEntry.ValoresNuevos = System.Text.Json.JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditEntry.Accion = "ELIMINAR";
                auditEntry.ValoresAnteriores = System.Text.Json.JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
            }
            else if (entry.State == EntityState.Modified)
            {
                auditEntry.Accion = "MODIFICAR";
                auditEntry.ValoresAnteriores = System.Text.Json.JsonSerializer.Serialize(
                    entry.Properties.Where(p => p.IsModified).ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                auditEntry.ValoresNuevos = System.Text.Json.JsonSerializer.Serialize(
                    entry.Properties.Where(p => p.IsModified).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
        }

        return auditEntries;
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            if (auditEntry.TempProperties.Any())
            {
                foreach (var prop in auditEntry.TempProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.ClavePrimaria = prop.CurrentValue?.ToString() ?? string.Empty;
                    }
                }
            }

            var auditLog = new Auditoria
            {
                Tabla = auditEntry.Tabla,
                Accion = auditEntry.Accion,
                ClavePrimaria = auditEntry.ClavePrimaria,
                Fecha = DateTime.UtcNow,
                ValoresAnteriores = auditEntry.ValoresAnteriores,
                ValoresNuevos = auditEntry.ValoresNuevos,
                UsuarioEmail = ObtenerUsuarioActual()
            };

            Auditorias.Add(auditLog);
        }

        await base.SaveChangesAsync();
    }

    /// <summary>
    /// Registra un acceso de LECTURA en la auditoria.
    ///
    /// El mecanismo automatico de auditoria se engancha a SaveChanges, de modo que
    /// solo ve escrituras. Una lectura no modifica nada, asi que pasa sin dejar
    /// rastro. En una historia clinica eso es un hueco: la contracara de permitir
    /// que cualquier medico consulte a cualquier paciente (necesario para la
    /// continuidad de la atencion, y mas todavia en una urgencia) es dejar
    /// constancia de quien miro que.
    ///
    /// Falla cerrado a proposito: si no se puede registrar el acceso, la consulta
    /// falla. No cuesta disponibilidad, porque la auditoria escribe en la misma
    /// base de la que se acaba de leer: si esa escritura no anda, la lectura
    /// tampoco hubiera andado.
    /// </summary>
    public async Task RegistrarAccesoAsync(
        string tabla,
        string clavePrimaria,
        string? detalle = null,
        CancellationToken cancellationToken = default)
    {
        Auditorias.Add(new Auditoria
        {
            Tabla = tabla,
            Accion = "CONSULTAR",
            ClavePrimaria = clavePrimaria,
            Fecha = DateTime.UtcNow,
            UsuarioEmail = ObtenerUsuarioActual(),
            ValoresNuevos = detalle
        });

        // base.SaveChangesAsync y no el propio: el interceptor de auditoria ya
        // ignora las entidades Auditoria, pero llamar a la base deja explicito
        // que este registro no se audita a si mismo.
        await base.SaveChangesAsync(cancellationToken);
    }

    private string ObtenerUsuarioActual()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue(ClaimTypes.Email)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? "sistema";
    }

    private class AuditEntry
    {
        public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            Entry = entry;
            TempProperties = entry.Properties.Where(p => p.Metadata.IsPrimaryKey() && entry.State == EntityState.Added).ToList();
            if (!TempProperties.Any())
            {
                var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (pkProp != null)
                {
                    ClavePrimaria = pkProp.CurrentValue?.ToString() ?? string.Empty;
                }
            }
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
        public string Tabla { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string ClavePrimaria { get; set; } = string.Empty;
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNuevos { get; set; }
        public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TempProperties { get; }
    }
}
