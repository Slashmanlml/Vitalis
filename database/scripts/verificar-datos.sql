-- Verificación de los datos sembrados por DbSeeder
--
-- Uso:
--   cd "C:\Users\Tito\Desktop\New project"
--   psql -U postgres -d vitalis -f database\scripts\verificar-datos.sql
--
-- Nota: las TABLAS se llaman en minúscula con guiones bajos (así las declara
-- VitalisDbContext con ToTable), pero las COLUMNAS conservan PascalCase y por eso
-- van entre comillas dobles. Mezclar ambas convenciones es lo que hace que este
-- tipo de consulta falle si se escribe de memoria.

\echo '=== Cantidad de registros por tabla ==='

SELECT 'roles'                 AS tabla, COUNT(*) AS filas FROM roles
UNION ALL SELECT 'usuarios',              COUNT(*) FROM usuarios
UNION ALL SELECT 'obras_sociales',        COUNT(*) FROM obras_sociales
UNION ALL SELECT 'especialidades',        COUNT(*) FROM especialidades
UNION ALL SELECT 'profesionales',         COUNT(*) FROM profesionales
UNION ALL SELECT 'pacientes',             COUNT(*) FROM pacientes
UNION ALL SELECT 'turnos',                COUNT(*) FROM turnos
UNION ALL SELECT 'consultas_medicas',     COUNT(*) FROM consultas_medicas
UNION ALL SELECT 'antecedentes_clinicos', COUNT(*) FROM antecedentes_clinicos
UNION ALL SELECT 'alergias',              COUNT(*) FROM alergias
UNION ALL SELECT 'medicamentos',          COUNT(*) FROM medicamentos
UNION ALL SELECT 'prescripciones',        COUNT(*) FROM prescripciones
UNION ALL SELECT 'prescripcion_detalles', COUNT(*) FROM prescripcion_detalles
UNION ALL SELECT 'prestaciones',          COUNT(*) FROM prestaciones
UNION ALL SELECT 'facturas',              COUNT(*) FROM facturas
UNION ALL SELECT 'factura_detalles',      COUNT(*) FROM factura_detalles
UNION ALL SELECT 'pagos',                 COUNT(*) FROM pagos
UNION ALL SELECT 'liquidaciones',         COUNT(*) FROM liquidaciones
UNION ALL SELECT 'bloqueos_agenda',       COUNT(*) FROM bloqueos_agenda
UNION ALL SELECT 'email_logs',            COUNT(*) FROM email_logs
UNION ALL SELECT 'auditorias',            COUNT(*) FROM auditorias
ORDER BY tabla;

\echo ''
\echo '=== Turnos por estado (de aqui salen los numeros del panel) ==='

SELECT "Estado", COUNT(*) AS cantidad
FROM turnos
GROUP BY "Estado"
ORDER BY cantidad DESC;

\echo ''
\echo '=== Turnos de HOY (si da 0, el panel muestra todo en cero) ==='

SELECT COUNT(*) AS turnos_hoy
FROM turnos
WHERE "FechaHora"::date = CURRENT_DATE;

\echo ''
\echo '=== Pacientes con historia clinica cargada ==='

SELECT p."Nombre" || ' ' || p."Apellido" AS paciente,
       COUNT(c."Id") AS consultas
FROM pacientes p
LEFT JOIN consultas_medicas c ON c."PacienteId" = p."Id"
GROUP BY p."Id", p."Nombre", p."Apellido"
ORDER BY consultas DESC;

\echo ''
\echo '=== Recetas emitidas, con diagnostico y cantidad de medicamentos ==='

SELECT pr."Id"                              AS receta,
       pa."Nombre" || ' ' || pa."Apellido"  AS paciente,
       LEFT(c."Diagnostico", 40)            AS diagnostico,
       COUNT(d."Id")                        AS medicamentos
FROM prescripciones pr
JOIN pacientes pa        ON pa."Id" = pr."PacienteId"
JOIN consultas_medicas c ON c."Id"  = pr."ConsultaMedicaId"
LEFT JOIN prescripcion_detalles d ON d."PrescripcionId" = pr."Id"
GROUP BY pr."Id", pa."Nombre", pa."Apellido", c."Diagnostico"
ORDER BY pr."Id";

\echo ''
\echo '=== Notificaciones registradas, por evento y origen ==='

SELECT "Evento", "Origen", "Estado", COUNT(*) AS cantidad
FROM email_logs
GROUP BY "Evento", "Origen", "Estado"
ORDER BY cantidad DESC;

\echo ''
\echo '=== Control de integridad: nada de esto deberia devolver filas ==='

\echo '-- Recetas cuya consulta no existe:'
SELECT pr."Id" FROM prescripciones pr
LEFT JOIN consultas_medicas c ON c."Id" = pr."ConsultaMedicaId"
WHERE c."Id" IS NULL;

\echo '-- Consultas cuyo turno no existe:'
SELECT c."Id" FROM consultas_medicas c
LEFT JOIN turnos t ON t."Id" = c."TurnoId"
WHERE t."Id" IS NULL;

\echo '-- Turnos marcados Atendido sin consulta asociada:'
SELECT t."Id" FROM turnos t
LEFT JOIN consultas_medicas c ON c."TurnoId" = t."Id"
WHERE t."Estado" = 'Atendido' AND c."Id" IS NULL;

\echo '-- Recetas con cero medicamentos:'
SELECT pr."Id" FROM prescripciones pr
LEFT JOIN prescripcion_detalles d ON d."PrescripcionId" = pr."Id"
GROUP BY pr."Id" HAVING COUNT(d."Id") = 0;
