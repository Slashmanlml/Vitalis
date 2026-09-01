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
        var facturacionRol = await context.Roles
            .FirstAsync(r => r.Nombre == Roles.Facturacion, cancellationToken);

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

        // El rol Facturacion existe en el modelo de seguridad y gobierna tres
        // pantallas —Facturacion, Liquidaciones y Prestaciones— pero no habia
        // ninguna cuenta con ese rol: solo podia recorrerse como administrador,
        // de modo que la matriz de autorizacion no era demostrable por completo.
        if (!await context.Usuarios.AnyAsync(u => u.Email == "facturacion@vitalis.local", cancellationToken))
        {
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Facturación",
                Apellido = "Vitalis",
                Email = "facturacion@vitalis.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Facturacion123!"),
                RolId = facturacionRol.Id,
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
                new() { Nombre = "Sofía", Apellido = "Rodríguez", Dni = "30123456", FechaNacimiento = DateTime.SpecifyKind(new DateTime(1990, 5, 12), DateTimeKind.Utc), Telefono = "1155551001", Email = "sofiar@email.com", Direccion = "Av. Corrientes 1200, CABA", ObraSocialId = osde.Id, NumeroAfiliado = "OSD-001", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Mateo", Apellido = "López", Dni = "33456789", FechaNacimiento = DateTime.SpecifyKind(new DateTime(1985, 8, 22), DateTimeKind.Utc), Telefono = "1155551002", Email = "mateol@email.com", Direccion = "Calle Florida 500, CABA", ObraSocialId = sm.Id, NumeroAfiliado = "SM-001", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Valentina", Apellido = "Silva", Dni = "28765432", FechaNacimiento = DateTime.SpecifyKind(new DateTime(1995, 2, 3), DateTimeKind.Utc), Telefono = "1155551003", Email = "valentinas@email.com", Direccion = "Av. Santa Fe 800, CABA", ObraSocialId = galeno.Id, NumeroAfiliado = "GAL-001", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Lucas", Apellido = "Pérez", Dni = "36123478", FechaNacimiento = DateTime.SpecifyKind(new DateTime(2000, 11, 15), DateTimeKind.Utc), Telefono = "1155551004", Email = "lucasp@email.com", Direccion = "Calle Lavalle 300, CABA", ObraSocialId = osde.Id, NumeroAfiliado = "OSD-002", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Emma", Apellido = "González", Dni = "27555666", FechaNacimiento = DateTime.SpecifyKind(new DateTime(1992, 7, 30), DateTimeKind.Utc), Telefono = "1155551005", Email = "emmag@email.com", Direccion = "Av. Rivadavia 2000, CABA", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Benjamín", Apellido = "Torres", Dni = "30888999", FechaNacimiento = DateTime.SpecifyKind(new DateTime(1988, 4, 18), DateTimeKind.Utc), Telefono = "1155551006", Email = "benjamint@email.com", Direccion = "Calle Callao 600, CABA", ObraSocialId = sm.Id, NumeroAfiliado = "SM-002", Activo = true, FechaCreacion = DateTime.UtcNow },
                new() { Nombre = "Catalina", Apellido = "Martín", Dni = "32444111", FechaNacimiento = DateTime.SpecifyKind(new DateTime(1998, 9, 25), DateTimeKind.Utc), Telefono = "1155551007", Email = "catalinam@email.com", Direccion = "Av. Belgrano 1500, CABA", Activo = true, FechaCreacion = DateTime.UtcNow }
            };

            context.Pacientes.AddRange(pacientes);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Pacientes iniciales creados.");
        }

        if (!await context.Turnos.AnyAsync(cancellationToken))
        {
            var pacientes = await context.Pacientes.ToListAsync(cancellationToken);
            var profesionales = await context.Profesionales
                .Include(p => p.Especialidad)
                .ToListAsync(cancellationToken);

            // La plantilla clínica se elige por la especialidad del profesional.
            // Depender de la propiedad de navegación resultó frágil: llegaba nula y
            // el respaldo "Clínica Médica" se aplicaba a todas las consultas, de modo
            // que un cardiólogo terminaba diagnosticando cefalea tensional. Se resuelve
            // por id contra un diccionario propio, que no depende de cómo EF Core
            // rellene las navegaciones.
            var nombreEspecialidadPorId = await context.Especialidades
                .AsNoTracking()
                .ToDictionaryAsync(e => e.Id, e => e.Nombre, cancellationToken);
            var obras = await context.ObrasSociales.ToListAsync(cancellationToken);
            var hoy = DateTime.UtcNow.Date;
            var random = new Random(42);

            var obrasPorPaciente = new Dictionary<int, int>();
            foreach (var p in pacientes)
                obrasPorPaciente[p.Id] = p.ObraSocialId ?? obras[random.Next(obras.Count)].Id;

            var turnos = new List<Turno>();

            // Plantillas de consulta clínicamente coherentes con la especialidad del
            // profesional que atiende el turno. Se proyectan ante un jurado: un
            // cardiólogo no diagnostica una otitis.
            var plantillas = new Dictionary<string, List<(string Motivo, string Diag, string Evo, string Ind, string Obs)>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Cardiología"] = new()
                {
                    ("Control de hipertensión arterial",
                     "Hipertensión arterial grado 1",
                     "Tensión 130/85 mmHg, ritmo regular, sin signos de congestión.",
                     "Enalapril 10 mg cada 12 horas y Amlodipina 5 mg por la mañana. Control en 30 días.",
                     "Laboratorio en ayunas: perfil lipídico y función renal."),
                    ("Palpitaciones ocasionales de semanas",
                     "Extrasístoles supraventriculares benignas",
                     "ECG sin alteraciones significativas, no hay taquiarritmias.",
                     "Reducir cafeína y estrés. Si el perfil lipídico está elevado, Atorvastatina 20 mg por la noche.",
                     "Holter de 24 horas indicado para descartar arritmias."
                    )
                },
                ["Pediatría"] = new()
                {
                    ("Tos y catarro de una semana",
                     "Bronquitis aguda viral",
                     "Buena mecánica ventilatoria, sin dificultad respiratoria.",
                     "Salbutamol aerosol 2 puff cada 6 horas; Amoxicilina 500 mg cada 8 horas por 7 días si persiste; Paracetamol ante fiebre.",
                     "Calendario de vacunación al día."),
                    ("Control de niño sano",
                     "Crecimiento y desarrollo normales",
                     "Percentilos dentro de lo esperado para la edad.",
                     "Continuar con pautas de alimentación y vacunas según el calendario.",
                     "Próximo control en 6 meses."
                    )
                },
                ["Traumatología"] = new()
                {
                    ("Dolor en rodilla al subir escaleras",
                     "Gonalgia por sobreuso / tendinopatía rotuliana",
                     "Sin derrame articular, movilidad conservada.",
                     "Diclofenac 75 mg cada 12 horas por 10 días, reposo relativo y kinesiología.",
                     "Evitar impacto. Reevaluar en 15 días."),
                    ("Esguince de tobillo tras una torcedura",
                     "Esguince de tobillo grado I",
                     "Leve inflamación, sin deformidad ni impotencia funcional.",
                     "Ibuprofeno 400 mg cada 12 horas por 5 días, hielo y compresión.",
                     "Retomar la actividad de forma gradual."
                    )
                },
                ["Clínica Médica"] = new()
                {
                    ("Dolor de cabeza recurrente de varias semanas",
                     "Cefalea tensional",
                     "Tensión arterial y examen neurológico sin particularidades. Mejora con reposo.",
                     "Paracetamol 500 mg cada 8 horas ante el dolor, hidratación y sueño regular. Volver si no cede en 7 días.",
                     "Se descartaron signos de alarma neurológicos."),
                    ("Control de diabetes",
                     "Diabetes mellitus tipo 2 compensada",
                     "Glucemia en ayunas de 118 mg/dl.",
                     "Metformina 850 mg con el desayuno y la cena, dieta hipoglucídica.",
                     "Hemoglobina glicosilada en 3 meses."),
                    ("Cansancio y aumento de peso",
                     "Hipotiroidismo subclínico",
                     "TSH levemente elevada, resto de la función tiroidea normal.",
                     "Levotiroxina 100 mcg en ayunas, 30 minutos antes del desayuno.",
                     "Control de TSH en 3 meses."
                    )
                },
                ["Dermatología"] = new()
                {
                    ("Acné en mejillas y frente",
                     "Acné inflamatorio leve",
                     "Lesiones papulosas sin signos de infección.",
                     "Limpieza diaria con jabón neutro, evitar oclusión. Control en 30 días.",
                     "Por ahora sin antibioticoterapia oral."),
                    ("Picazón y enrojecimiento en el antebrazo",
                     "Dermatitis de contacto",
                     "Eritema localizado, bien delimitado.",
                     "Cremas emolientes y evitar el alérgeno. Control en 15 días.",
                     "Derivación a alergia si recurre."
                    )
                }
            };

            // Índice por especialidad para repartir las plantillas de forma variada.
            var siguientePlantilla = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < 15; i++)
            {
                // Los primeros 12 son consultas ya atendidas (historia clínica); los
                // últimos 3 quedan como turnos futuros para la agenda y sala de espera.
                bool esAtendido = i < 12;

                var paciente = pacientes[i % pacientes.Count];
                var profesional = profesionales[i % profesionales.Count];
                int diaOffset = esAtendido ? -(30 - i * 2) : (i - 11);
                var dia = hoy.AddDays(diaOffset);
                var hora = new DateTime(dia.Year, dia.Month, dia.Day, 9 + (i % 8), (i % 4) * 15, 0);
                hora = DateTime.SpecifyKind(hora, DateTimeKind.Utc);

                var estado = esAtendido ? "Atendido" : (i == 14 ? "Solicitado" : "Confirmado");
                var confirmado = estado != "Solicitado";

                var turno = new Turno
                {
                    PacienteId = paciente.Id,
                    ProfesionalId = profesional.Id,
                    ObraSocialId = obrasPorPaciente[paciente.Id],
                    FechaHora = hora,
                    Confirmado = confirmado,
                    Estado = estado
                };

                if (esAtendido)
                {
                    var especialidad = nombreEspecialidadPorId.TryGetValue(
                        profesional.EspecialidadId, out var nombreEsp) ? nombreEsp : "Clínica Médica";

                    if (!plantillas.TryGetValue(especialidad, out var plantillasEsp))
                    {
                        // Si aparece una especialidad sin plantillas se avisa, en vez de
                        // caer en silencio en la genérica: es exactamente el modo de falla
                        // que hizo que todas las consultas quedaran iguales.
                        logger.LogWarning(
                            "No hay plantillas clínicas para la especialidad '{Especialidad}'; se usa la genérica.",
                            especialidad);
                        plantillasEsp = plantillas["Clínica Médica"];
                    }
                    var idx = siguientePlantilla.GetValueOrDefault(especialidad, 0);
                    var tpl = plantillasEsp[idx % plantillasEsp.Count];
                    siguientePlantilla[especialidad] = idx + 1;

                    turno.ConsultaMedica = new ConsultaMedica
                    {
                        PacienteId = paciente.Id,
                        ProfesionalId = profesional.Id,
                        Fecha = hora,
                        MotivoConsulta = tpl.Motivo,
                        Diagnostico = tpl.Diag,
                        Evolucion = tpl.Evo,
                        Indicaciones = tpl.Ind,
                        Observaciones = tpl.Obs
                    };
                }

                turnos.Add(turno);
            }

            context.Turnos.AddRange(turnos);
            await context.SaveChangesAsync(cancellationToken);

            var repartoPlantillas = string.Join(", ",
                siguientePlantilla.Select(kv => $"{kv.Key}: {kv.Value}"));
            logger.LogInformation(
                "Turnos de ejemplo creados. Consultas por especialidad -> {Reparto}",
                string.IsNullOrEmpty(repartoPlantillas) ? "(ninguna)" : repartoPlantillas);
        }

        if (!await context.Medicamentos.AnyAsync(cancellationToken))
        {
            var medicamentos = new List<Medicamento>
            {
                new() { Nombre = "Ibuprofeno", Presentacion = "400 mg, comprimido recubierto", Activo = true },
                new() { Nombre = "Amoxicilina", Presentacion = "500 mg, cápsula", Activo = true },
                new() { Nombre = "Enalapril", Presentacion = "10 mg, comprimido", Activo = true },
                new() { Nombre = "Metformina", Presentacion = "850 mg, comprimido", Activo = true },
                new() { Nombre = "Losartán", Presentacion = "50 mg, comprimido", Activo = true },
                new() { Nombre = "Omeprazol", Presentacion = "20 mg, cápsula gastroresistente", Activo = true },
                new() { Nombre = "Paracetamol", Presentacion = "500 mg, comprimido", Activo = true },
                new() { Nombre = "Salbutamol", Presentacion = "100 mcg/dosis, aerosol inhalador", Activo = true },
                new() { Nombre = "Levotiroxina", Presentacion = "100 mcg, comprimido", Activo = true },
                new() { Nombre = "Atorvastatina", Presentacion = "20 mg, comprimido", Activo = true },
                new() { Nombre = "Diclofenac", Presentacion = "75 mg, comprimido de liberación prolongada", Activo = true },
                new() { Nombre = "Amlodipina", Presentacion = "5 mg, comprimido", Activo = true }
            };

            context.Medicamentos.AddRange(medicamentos);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Catálogo de medicamentos sembrado.");
        }

        if (!await context.Prescripciones.AnyAsync(cancellationToken))
        {
            var consultas = await context.ConsultasMedicas.AsNoTracking().ToListAsync(cancellationToken);
            var medicamentos = await context.Medicamentos.AsNoTracking().ToListAsync(cancellationToken);

            // Busca la consulta cuyo diagnóstico coincida. Como las consultas se siembran
            // recién si la base es nueva, esta búsqueda es determinista en ese caso.
            ConsultaMedica? porDiagnostico(string clave)
            {
                return consultas.FirstOrDefault(c => (c.Diagnostico ?? string.Empty).Contains(clave, StringComparison.OrdinalIgnoreCase));
            }
            Medicamento porNombre(string nombre) => medicamentos.First(m => m.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

            var prescripciones = new List<Prescripcion>();

            void Agregar(ConsultaMedica? c, (string Medicamento, string Dosis, string Frecuencia, string Duracion, string Indicaciones)[] det)
            {
                if (c == null) return;
                var p = new Prescripcion
                {
                    ConsultaMedicaId = c.Id,
                    PacienteId = c.PacienteId,
                    ProfesionalId = c.ProfesionalId,
                    Fecha = DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc),
                    Observaciones = "Indicaciones de la consulta",
                    Detalles = det.Select(d => new PrescripcionDetalle
                    {
                        MedicamentoId = porNombre(d.Medicamento).Id,
                        Dosis = d.Dosis,
                        Frecuencia = d.Frecuencia,
                        Duracion = d.Duracion,
                        Indicaciones = d.Indicaciones
                    }).ToList()
                };
                prescripciones.Add(p);
            }

            Agregar(porDiagnostico("hipertensión"), new[]
            {
                ("Enalapril", "1 comprimido", "cada 12 horas", "30 días", "Con el estómago lleno"),
                ("Amlodipina", "1 comprimido", "cada 24 horas por la mañana", "30 días", "A la misma hora")
            });
            Agregar(porDiagnostico("diabetes mellitus"), new[]
            {
                ("Metformina", "1 comprimido", "con el desayuno y 1 con la cena", "60 días", "Después de comer")
            });
            Agregar(porDiagnostico("bronquitis"), new[]
            {
                ("Amoxicilina", "1 comprimido", "cada 8 horas", "7 días", "Completar el esquema aunque mejore"),
                ("Salbutamol", "2 puff", "cada 6 horas", "7 días", "Según disnea"),
                ("Paracetamol", "1 comprimido", "cada 8 horas ante fiebre", "7 días", "No superar 4 por día")
            });
            Agregar(porDiagnostico("gonalgia"), new[]
            {
                ("Diclofenac", "1 comprimido", "cada 12 horas", "10 días", "Después de comer")
            });
            Agregar(porDiagnostico("cefalea tensional"), new[]
            {
                ("Paracetamol", "1 comprimido", "cada 8 horas ante dolor", "5 días", "No superar la dosis"),
                ("Ibuprofeno", "1 comprimido", "cada 12 horas si persiste", "5 días", "Con alimento")
            });
            Agregar(porDiagnostico("esguince"), new[]
            {
                ("Ibuprofeno", "1 comprimido", "cada 12 horas", "5 días", "Con alimento")
            });
            Agregar(porDiagnostico("extrasístoles"), new[]
            {
                ("Atorvastatina", "1 comprimido", "cada 24 horas por la noche", "60 días", "Si perfil lipídico elevado")
            });
            Agregar(porDiagnostico("hipertensión"), new[]
            {
                ("Losartán", "1 comprimido", "cada 24 horas", "30 días", "Por la mañana"),
                ("Amlodipina", "1 comprimido", "cada 24 horas", "30 días", "A la misma hora")
            });

            if (prescripciones.Count > 0)
            {
                context.Prescripciones.AddRange(prescripciones);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Prescripciones de ejemplo sembradas.");
            }
        }

        // Registro de notificaciones. Sin esto la pantalla de Notificaciones aparece
        // vacía en la demostración, aun cuando el módulo funciona: los avisos reales
        // recién se generan cuando alguien opera el sistema, y la siembra no pasa por
        // los servicios de negocio.
        //
        // Se incluyen a propósito un envío fallido y uno simulado, porque la pantalla
        // distingue esos tres estados y conviene que se vea esa distinción.
        if (!await context.EmailLogs.AnyAsync(cancellationToken))
        {
            var turnosConPaciente = await context.Turnos
                .Include(t => t.Paciente)
                .Include(t => t.Profesional)
                .OrderBy(t => t.FechaHora)
                .ToListAsync(cancellationToken);

            var logs = new List<EmailLog>();

            string Cuerpo(string titulo, string detalle) =>
                $"<div style='font-family: Arial, sans-serif; padding: 20px;'>" +
                $"<h2 style='color:#0F766E;'>{titulo}</h2><p>{detalle}</p>" +
                $"<p style='font-size:12px;color:#6B7280;'>Vitalis — Sistema de Gestión Médica</p></div>";

            void Registrar(Turno t, string evento, string asunto, string detalle,
                           string estado, int diasAtras, string? error = null)
            {
                if (t.Paciente?.Email == null) return;
                logs.Add(new EmailLog
                {
                    Destinatario = t.Paciente.Email,
                    Asunto = asunto,
                    Cuerpo = Cuerpo(asunto, detalle),
                    FechaEnvio = DateTime.UtcNow.AddDays(-diasAtras),
                    Origen = OrigenNotificacion.Sistema,
                    Evento = evento,
                    TurnoId = t.Id,
                    Estado = estado,
                    MensajeError = error
                });
            }

            string Cuando(Turno t) => t.FechaHora.ToString("dd/MM/yyyy HH:mm");
            string Prof(Turno t) => t.Profesional == null
                ? "su profesional"
                : $"{t.Profesional.Nombre} {t.Profesional.Apellido}";

            for (int i = 0; i < turnosConPaciente.Count && i < 10; i++)
            {
                var t = turnosConPaciente[i];

                Registrar(t, EventoNotificacion.TurnoCreado,
                    "Turno reservado - Vitalis",
                    $"Su turno con {Prof(t)} quedó reservado para el {Cuando(t)}.",
                    EstadoNotificacion.Enviado, 12 - i);

                if (t.Confirmado)
                {
                    Registrar(t, EventoNotificacion.TurnoConfirmado,
                        "Turno confirmado - Vitalis",
                        $"Le confirmamos su turno con {Prof(t)} para el {Cuando(t)}.",
                        EstadoNotificacion.Enviado, 11 - i);
                }

                if (t.Estado == "Atendido" && i % 3 == 0)
                {
                    Registrar(t, EventoNotificacion.RecordatorioTurno,
                        "Recordatorio: su consulta es mañana - Vitalis",
                        $"Le recordamos su consulta con {Prof(t)} el {Cuando(t)}.",
                        EstadoNotificacion.Enviado, 10 - i);
                }
            }

            // Un envío fallido, para que se vea cómo queda registrada la causa.
            var conFalla = turnosConPaciente.FirstOrDefault(t => t.Paciente?.Email != null);
            if (conFalla != null)
            {
                Registrar(conFalla, EventoNotificacion.RecordatorioTurno,
                    "Recordatorio: su consulta es mañana - Vitalis",
                    $"Le recordamos su consulta del {Cuando(conFalla)}.",
                    EstadoNotificacion.Fallido, 2,
                    "SMTP: no se pudo establecer conexión con el servidor de correo (timeout).");
            }

            // Y uno generado a mano desde la pantalla, que es lo que el campo Origen
            // permite distinguir de la evidencia real emitida por el sistema.
            logs.Add(new EmailLog
            {
                Destinatario = "admin@vitalis.local",
                Asunto = "Prueba de plantilla - Vitalis",
                Cuerpo = Cuerpo("Prueba de plantilla",
                                "Envío generado manualmente para verificar el formato."),
                FechaEnvio = DateTime.UtcNow.AddDays(-1),
                Origen = OrigenNotificacion.Simulado,
                Evento = EventoNotificacion.Personalizado,
                Estado = EstadoNotificacion.Simulado
            });

            if (logs.Count > 0)
            {
                context.EmailLogs.AddRange(logs);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Registro de notificaciones de ejemplo sembrado.");
            }
        }

        if (!await context.AntecedentesClinicos.AnyAsync(cancellationToken))
        {
            var pacientes = await context.Pacientes.AsNoTracking().ToListAsync(cancellationToken);
            Paciente porApellido(string apellido) => pacientes.First(p => p.Apellido.Equals(apellido, StringComparison.OrdinalIgnoreCase));

            var antecedentes = new List<AntecedenteClinico>
            {
                new() { PacienteId = porApellido("Rodríguez").Id, Tipo = "Patológico", Descripcion = "Hipertensión arterial diagnosticada hace 5 años", FechaRegistro = DateTime.UtcNow.AddYears(-1) },
                new() { PacienteId = porApellido("Rodríguez").Id, Tipo = "Familiar", Descripcion = "Madre con cardiopatía isquémica", FechaRegistro = DateTime.UtcNow.AddYears(-1) },
                new() { PacienteId = porApellido("López").Id, Tipo = "Quirúrgico", Descripcion = "Apendicectomía en 2015", FechaRegistro = DateTime.UtcNow.AddYears(-2) },
                new() { PacienteId = porApellido("Silva").Id, Tipo = "Patológico", Descripcion = "Diabetes mellitus tipo 2", FechaRegistro = DateTime.UtcNow.AddMonths(-6) },
                new() { PacienteId = porApellido("Silva").Id, Tipo = "Familiar", Descripcion = "Padre diabético y abuelo con antecedente de ACV", FechaRegistro = DateTime.UtcNow.AddMonths(-6) },
                new() { PacienteId = porApellido("Pérez").Id, Tipo = "Quirúrgico", Descripcion = "Amigdalectomía en la infancia", FechaRegistro = DateTime.UtcNow.AddYears(-3) },
                new() { PacienteId = porApellido("González").Id, Tipo = "Patológico", Descripcion = "Migrañas recurrentes desde los 20 años", FechaRegistro = DateTime.UtcNow.AddYears(-1) },
                new() { PacienteId = porApellido("Torres").Id, Tipo = "Patológico", Descripcion = "Asma bronquial infantil", FechaRegistro = DateTime.UtcNow.AddMonths(-4) },
                new() { PacienteId = porApellido("Martín").Id, Tipo = "Familiar", Descripcion = "Hipotiroidismo materno", FechaRegistro = DateTime.UtcNow.AddMonths(-2) },
                new() { PacienteId = porApellido("López").Id, Tipo = "Alérgico", Descripcion = "Reacción previa a penicilina", FechaRegistro = DateTime.UtcNow.AddYears(-1) }
            };

            context.AntecedentesClinicos.AddRange(antecedentes);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Antecedentes clínicos de ejemplo sembrados.");
        }

        if (!await context.Alergias.AnyAsync(cancellationToken))
        {
            var pacientes = await context.Pacientes.AsNoTracking().ToListAsync(cancellationToken);
            Paciente porApellido(string apellido) => pacientes.First(p => p.Apellido.Equals(apellido, StringComparison.OrdinalIgnoreCase));

            var alergias = new List<Alergia>
            {
                new() { PacienteId = porApellido("López").Id, Sustancia = "Penicilina", Reaccion = "Urticaria", Severidad = "Moderada", Activa = true },
                new() { PacienteId = porApellido("González").Id, Sustancia = "Penicilina", Reaccion = "Urticaria generalizada", Severidad = "Moderada", Activa = true },
                new() { PacienteId = porApellido("Rodríguez").Id, Sustancia = "Sulfamidas", Reaccion = "Exantema", Severidad = "Grave", Activa = true },
                new() { PacienteId = porApellido("Pérez").Id, Sustancia = "Amoxicilina", Reaccion = "Náuseas y rash", Severidad = "Leve", Activa = true },
                new() { PacienteId = porApellido("Silva").Id, Sustancia = "Contraste yodado", Reaccion = "Urticaria", Severidad = "Grave", Activa = true },
                new() { PacienteId = porApellido("Torres").Id, Sustancia = "Lactosa", Reaccion = "Distensión abdominal", Severidad = "Leve", Activa = true }
            };

            context.Alergias.AddRange(alergias);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Alergias de ejemplo sembradas.");
        }

        if (!await context.BloqueosAgenda.AnyAsync(cancellationToken))
        {
            var profesionales = await context.Profesionales.AsNoTracking().ToListAsync(cancellationToken);
            Profesional porApellido(string apellido) => profesionales.First(p => p.Apellido.Equals(apellido, StringComparison.OrdinalIgnoreCase));

            var hoy = DateTime.UtcNow.Date;
            var mañanaLunes = DateTime.SpecifyKind(hoy.AddDays(1).AddHours(8), DateTimeKind.Utc);

            var bloqueos = new List<BloqueoAgenda>
            {
                new()
                {
                    ProfesionalId = porApellido("Gómez").Id,
                    FechaHoraInicio = DateTime.SpecifyKind(hoy.AddDays(3).AddHours(8), DateTimeKind.Utc),
                    FechaHoraFin = DateTime.SpecifyKind(hoy.AddDays(3).AddHours(18), DateTimeKind.Utc),
                    Motivo = "Congreso de cardiología"
                },
                new()
                {
                    ProfesionalId = porApellido("Fernández").Id,
                    FechaHoraInicio = DateTime.SpecifyKind(hoy.AddDays(-15).AddHours(8), DateTimeKind.Utc),
                    FechaHoraFin = DateTime.SpecifyKind(hoy.AddDays(-10).AddHours(18), DateTimeKind.Utc),
                    Motivo = "Licencia médica"
                }
            };

            context.BloqueosAgenda.AddRange(bloqueos);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Bloqueos de agenda de ejemplo sembrados.");
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
