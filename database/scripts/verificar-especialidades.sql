-- ¿Cada médico diagnostica lo que corresponde a su especialidad?
--
-- Uso:
--   cd "C:\Users\Tito\Desktop\New project"
--   psql -U postgres -d vitalis -f database\scripts\verificar-especialidades.sql
--
-- Qué se espera: que aparezcan VARIAS especialidades, cada una con diagnósticos
-- propios (Cardiología con hipertensión, Pediatría con bronquitis, etc.).
--
-- Si aparece una sola especialidad, o todas con los mismos diagnósticos, la
-- plantilla clínica no se está eligiendo según el profesional que atiende.

\echo '=== Diagnósticos por especialidad del profesional ==='

SELECT e."Nombre"      AS especialidad,
       c."Diagnostico" AS diagnostico,
       COUNT(*)        AS consultas
FROM consultas_medicas c
JOIN profesionales  p ON p."Id" = c."ProfesionalId"
JOIN especialidades e ON e."Id" = p."EspecialidadId"
GROUP BY e."Nombre", c."Diagnostico"
ORDER BY e."Nombre", c."Diagnostico";

\echo ''
\echo '=== Resumen: cuantas especialidades distintas tienen consultas ==='

SELECT COUNT(DISTINCT e."Nombre") AS especialidades_con_consultas
FROM consultas_medicas c
JOIN profesionales  p ON p."Id" = c."ProfesionalId"
JOIN especialidades e ON e."Id" = p."EspecialidadId";

\echo ''
\echo '=== Recetas creadas (deberian ser mas de 2 si la siembra quedo variada) ==='

SELECT COUNT(*) AS recetas FROM prescripciones;
