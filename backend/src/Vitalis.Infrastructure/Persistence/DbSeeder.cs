using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(VitalisDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (!await context.Roles.AnyAsync(cancellationToken))
        {
            var roles = Roles.Todos.Select(nombre => new Rol
            {
                Nombre = nombre,
                Descripcion = $"Rol {nombre}"
            }).ToList();

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Roles iniciales creados.");
        }

        var adminRol = await context.Roles
            .FirstAsync(r => r.Nombre == Roles.Administrador, cancellationToken);
        var medicoRol = await context.Roles
            .FirstAsync(r => r.Nombre == Roles.Medico, cancellationToken);
        var recepRol = await context.Roles
            .FirstAsync(r => r.Nombre == Roles.Recepcionista, cancellationToken);

        bool cambioUsuarios = false;

        if (!await context.Usuarios.AnyAsync(u => u.Email == "admin@vitalis.local", cancellationToken))
        {
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Admin",
                Apellido = "Vitalis",
                Email = "admin@vitalis.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                RolId = adminRol.Id,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            cambioUsuarios = true;
        }

        if (!await context.Usuarios.AnyAsync(u => u.Email == "lmartinez@vitalis.local", cancellationToken))
        {
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Laura",
                Apellido = "Martínez",
                Email = "lmartinez@vitalis.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Medico123!"),
                RolId = medicoRol.Id,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            cambioUsuarios = true;
        }

        if (!await context.Usuarios.AnyAsync(u => u.Email == "recepcion@vitalis.local", cancellationToken))
        {
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Recepción",
                Apellido = "Vitalis",
                Email = "recepcion@vitalis.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recepcion123!"),
                RolId = recepRol.Id,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            cambioUsuarios = true;
        }

        if (cambioUsuarios)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Usuarios de prueba creados/verificados en la base de datos.");
        }

        // Vincular profesional Laura Martínez con su respectivo usuario si ya existen
        var lauraDoc = await context.Profesionales.FirstOrDefaultAsync(p => p.Email == "lmartinez@vitalis.local", cancellationToken);
        var lauraUsr = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == "lmartinez@vitalis.local", cancellationToken);
        if (lauraDoc != null && lauraUsr != null && lauraDoc.UsuarioId != lauraUsr.Id)
        {
            lauraDoc.UsuarioId = lauraUsr.Id;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Vínculo de Laura Martínez con su usuario de Medico actualizado.");
        }

        if (!await context.ObrasSociales.AnyAsync(cancellationToken))
        {
            var obras = new List<ObraSocial>
            {
                new() { Nombre = "OSDE", Codigo = "OSDE", Activa = true },
                new() { Nombre = "Swiss Medical", Codigo = "SM", Activa = true },
                new() { Nombre = "Galeno", Codigo = "GAL", Activa = true },
                new() { Nombre = "PAMI", Codigo = "PAMI", Activa = true },
                new() { Nombre = "OSECAC", Codigo = "OSECAC", Activa = true },
                new() { Nombre = "IOMA", Codigo = "IOMA", Activa = true }
            };

            context.ObrasSociales.AddRange(obras);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Obras Sociales reales iniciales creadas.");
        }

        if (!await context.Especialidades.AnyAsync(cancellationToken))
        {
            var especialidades = new List<Especialidad>
            {
                new() { Nombre = "Clínica Médica", Descripcion = "Atención médica general y primaria" },
                new() { Nombre = "Cardiología", Descripcion = "Prevención y tratamiento de afecciones cardíacas" },
                new() { Nombre = "Pediatría", Descripcion = "Salud y cuidado médico infantil" },
                new() { Nombre = "Dermatología", Descripcion = "Diagnóstico y tratamiento de patologías de la piel" },
                new() { Nombre = "Traumatología", Descripcion = "Lesiones óseas y del sistema locomotor" },
                new() { Nombre = "Oftalmología", Descripcion = "Cuidado y salud ocular" }
            };

            context.Especialidades.AddRange(especialidades);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Especialidades médicas reales iniciales creadas.");
        }

        if (!await context.Profesionales.AnyAsync(cancellationToken))
        {
            var especialidades = await context.Especialidades.ToListAsync(cancellationToken);
            var admin = await context.Usuarios.FirstAsync(u => u.Email == "admin@vitalis.local", cancellationToken);
            var lauraUser = await context.Usuarios.FirstAsync(u => u.Email == "lmartinez@vitalis.local", cancellationToken);

            var profesionales = new List<Profesional>
            {
                new() { Nombre = "Alejandro", Apellido = "Gómez", Matricula = "MP-1001", Email = "agomez@vitalis.local", Telefono = "1123451001", EspecialidadId = especialidades.First(e => e.Nombre == "Cardiología").Id, UsuarioId = admin.Id, Activo = true },
                new() { Nombre = "Laura", Apellido = "Martínez", Matricula = "MP-1002", Email = "lmartinez@vitalis.local", Telefono = "1123451002", EspecialidadId = especialidades.First(e => e.Nombre == "Pediatría").Id, UsuarioId = lauraUser.Id, Activo = true },
                new() { Nombre = "Carlos", Apellido = "Sánchez", Matricula = "MP-1003", Email = "csanchez@vitalis.local", Telefono = "1123451003", EspecialidadId = especialidades.First(e => e.Nombre == "Traumatología").Id, UsuarioId = admin.Id, Activo = true },
                new() { Nombre = "Ana", Apellido = "Díaz", Matricula = "MP-1004", Email = "adiaz@vitalis.local", Telefono = "1123451004", EspecialidadId = especialidades.First(e => e.Nombre == "Clínica Médica").Id, UsuarioId = admin.Id, Activo = true },
                new() { Nombre = "Roberto", Apellido = "Fernández", Matricula = "MP-1005", Email = "rfernandez@vitalis.local", Telefono = "1123451005", EspecialidadId = especialidades.First(e => e.Nombre == "Dermatología").Id, UsuarioId = admin.Id, Activo = true }
            };

            context.Profesionales.AddRange(profesionales);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Profesionales médicos iniciales creados.");
        }

        if (!await context.Pacientes.AnyAsync(cancellationToken))
        {
            var osde = await context.ObrasSociales.FirstAsync(o => o.Codigo == "OSDE", cancellationToken);
            var sm = await context.ObrasSociales.FirstAsync(o => o.Codigo == "SM", cancellationToken);
            var galeno = await context.ObrasSociales.FirstAsync(o => o.Codigo == "GAL", cancellationToken);

            var pacientes = new List<Paciente>
            {
                new() { Nombre = "Sofía", Apellido = "Rodríguez", Dni = "30123456", FechaNacimiento = new DateTime(1990, 5, 12), Telefono = "1155551001", Email = "sofiar@email.com", Direccion = "Av. Corrientes 1200, CABA", ObraSocialId = osde.Id, NumeroAfiliado = "OSD-001", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Mateo", Apellido = "López", Dni = "33456789", FechaNacimiento = new DateTime(1985, 8, 22), Telefono = "1155551002", Email = "mateol@email.com", Direccion = "Calle Florida 500, CABA", ObraSocialId = sm.Id, NumeroAfiliado = "SM-001", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Valentina", Apellido = "Silva", Dni = "28765432", FechaNacimiento = new DateTime(1995, 2, 3), Telefono = "1155551003", Email = "valentinas@email.com", Direccion = "Av. Santa Fe 800, CABA", ObraSocialId = galeno.Id, NumeroAfiliado = "GAL-001", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Lucas", Apellido = "Pérez", Dni = "36123478", FechaNacimiento = new DateTime(2000, 11, 15), Telefono = "1155551004", Email = "lucasp@email.com", Direccion = "Calle Lavalle 300, CABA", ObraSocialId = osde.Id, NumeroAfiliado = "OSD-002", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Emma", Apellido = "González", Dni = "27555666", FechaNacimiento = new DateTime(1992, 7, 30), Telefono = "1155551005", Email = "emmag@email.com", Direccion = "Av. Rivadavia 2000, CABA", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Benjamín", Apellido = "Torres", Dni = "30888999", FechaNacimiento = new DateTime(1988, 4, 18), Telefono = "1155551006", Email = "benjamint@email.com", Direccion = "Calle Callao 600, CABA", ObraSocialId = sm.Id, NumeroAfiliado = "SM-002", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Catalina", Apellido = "Martín", Dni = "32444111", FechaNacimiento = new DateTime(1998, 9, 25), Telefono = "1155551007", Email = "catalinam@email.com", Direccion = "Av. Belgrano 1500, CABA", Activo = true, FechaCreacion = DateTime.UtcNow }
            };

            context.Pacientes.AddRange(pacientes);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Pacientes iniciales creados.");
        }

        if (!await context.Turnos.AnyAsync(cancellationToken))
        {
            var pacientes = await context.Pacientes.ToListAsync(cancellationToken);
            var profesionales = await context.Profesionales.ToListAsync(cancellationToken);
            var obras = await context.ObrasSociales.ToListAsync(cancellationToken);
            var hoy = DateTime.UtcNow.Date;
            var random = new Random(42);

            var obrasPorPaciente = new Dictionary<int, int>();
            foreach (var p in pacientes)
                obrasPorPaciente[p.Id] = p.ObraSocialId ?? obras[random.Next(obras.Count)].Id;

            var turnos = new List<Turno>();

            for (int i = 0; i < 15; i++)
            {
                var paciente = pacientes[random.Next(pacientes.Count)];
                var profesional = profesionales[random.Next(profesionales.Count)];
                var diaOffset = random.Next(-5, 10);
                var dia = hoy.AddDays(diaOffset);
                var hora = DateTime.SpecifyKind(new DateTime(dia.Year, dia.Month, dia.Day, random.Next(8, 18), random.Next(0, 4) * 15, 0), DateTimeKind.Utc);
                var confirmado = random.NextDouble() > 0.3;

                var estado = confirmado ? "Confirmado" : "Solicitado";
                if (diaOffset < 0)
                {
                    estado = "Atendido";
                }

                var turno = new Turno
                {
                    PacienteId = paciente.Id,
                    ProfesionalId = profesional.Id,
                    ObraSocialId = obrasPorPaciente[paciente.Id],
                    FechaHora = hora,
                    Confirmado = confirmado,
                    Estado = estado
                };

                if (estado == "Atendido")
                {
                    turno.ConsultaMedica = new ConsultaMedica
                    {
                        PacienteId = paciente.Id,
                        ProfesionalId = profesional.Id,
                        Fecha = hora,
                        MotivoConsulta = "Consulta de control y monitoreo rutinario",
                        Diagnostico = "Paciente evoluciona favorablemente según lo previsto",
                        Evolucion = "Normotenso, sin particularidades clínicas agudas",
                        Indicaciones = "Mantener esquema de hidratación y controles periódicos trimestrales",
                        Observaciones = "Se solicita análisis de laboratorio para el próximo control"
                    };
                }

                turnos.Add(turno);
            }

            context.Turnos.AddRange(turnos);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Turnos de ejemplo con historias clínicas creados.");
        }

        if (!await context.Prestaciones.AnyAsync(cancellationToken))
        {
            var prestaciones = new List<Prestacion>
            {
                new() { Nombre = "Consulta Médica General", Codigo = "CONS-GEN", ImporteBase = 3000m, Activa = true },
                new() { Nombre = "Electrocardiograma (ECG)", Codigo = "ECG", ImporteBase = 4500m, Activa = true },
                new() { Nombre = "Radiografía de Tórax", Codigo = "RX-TORAX", ImporteBase = 6000m, Activa = true },
                new() { Nombre = "Análisis Clínicos Completo", Codigo = "LAB-COMP", ImporteBase = 8000m, Activa = true },
                new() { Nombre = "Ecografía Abdominal", Codigo = "ECO-ABD", ImporteBase = 9000m, Activa = true }
            };

            context.Prestaciones.AddRange(prestaciones);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Prestaciones iniciales creadas.");
        }

        if (!await context.Facturas.AnyAsync(cancellationToken))
        {
            var pacientes = await context.Pacientes.ToListAsync(cancellationToken);
            var prestaciones = await context.Prestaciones.ToListAsync(cancellationToken);
            var random = new Random(42);

            var facturas = new List<Factura>();
            for (int i = 0; i < 5; i++)
            {
                var paciente = pacientes[random.Next(pacientes.Count)];
                var prest1 = prestaciones[random.Next(prestaciones.Count)];
                var prest2 = prestaciones[random.Next(prestaciones.Count)];

                var factura = new Factura
                {
                    PacienteId = paciente.Id,
                    Fecha = DateTime.UtcNow.AddDays(-random.Next(1, 10)),
                    Estado = "Pendiente",
                    Observaciones = $"Factura de control de prestaciones i={i}",
                    Detalles = new List<FacturaDetalle>
                    {
                        new() { PrestacionId = prest1.Id, Cantidad = 1, PrecioUnitario = prest1.ImporteBase, Subtotal = prest1.ImporteBase },
                        new() { PrestacionId = prest2.Id, Cantidad = 1, PrecioUnitario = prest2.ImporteBase, Subtotal = prest2.ImporteBase }
                    }
                };
                factura.Total = factura.Detalles.Sum(d => d.Subtotal);

                if (i % 2 == 0)
                {
                    factura.Pagos.Add(new Pago
                    {
                        Fecha = DateTime.UtcNow,
                        MedioPago = "Efectivo",
                        Importe = factura.Total / 2,
                        Observaciones = "Pago parcial a cuenta"
                    });
                    factura.Estado = "Pago Parcial";
                }
                else if (i == 3)
                {
                    factura.Pagos.Add(new Pago
                    {
                        Fecha = DateTime.UtcNow,
                        MedioPago = "Tarjeta de Crédito",
                        Importe = factura.Total,
                        Observaciones = "Pago total del servicio"
                    });
                    factura.Estado = "Pagada";
                }

                facturas.Add(factura);
            }

            context.Facturas.AddRange(facturas);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Facturas iniciales creadas.");
        }

        if (!await context.Liquidaciones.AnyAsync(cancellationToken))
        {
            var profesionales = await context.Profesionales.ToListAsync(cancellationToken);
            var liquidaciones = new List<Liquidacion>
            {
                new()
                {
                    ProfesionalId = profesionales.First().Id,
                    PeriodoDesde = DateTime.UtcNow.AddMonths(-1),
                    PeriodoHasta = DateTime.UtcNow,
                    Total = 15000m,
                    Estado = "Liquidada",
                    FechaCreacion = DateTime.UtcNow
                }
            };
            context.Liquidaciones.AddRange(liquidaciones);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Liquidaciones iniciales creadas.");
        }
    }
}
