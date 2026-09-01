# 21 - Informe de Ajuste Responsive (Ancho Reducido)

## 1. Contexto y Objetivos
Durante demostraciones académicas, evaluaciones de jurado o uso en dispositivos móviles y tabletas (resoluciones típicas de 1024px, 768px y 480px), las interfaces de usuario deben responder adecuadamente sin desbordamiento horizontal global de la página (`body`), manteniendo la legibilidad, usabilidad y accesibilidad de los elementos interactivos.

### Reglas aplicadas
1. **Contención de Tablas**: Todas las tablas de datos extensas están envueltas en contenedores `.table-container` con `overflow-x: auto; -webkit-overflow-scrolling: touch;` y `min-width` para permitir el desplazamiento horizontal del componente sin desbordar el viewport global del navegador.
2. **Formularios adaptativos**: Formularios multi-columna (`.form-row`, `.form-row-2`, `.form-row-4`) colapsan a una sola columna en pantallas menores o iguales a 768px.
3. **Modales fluidos**: Diálogos modales con anchos fijos en píxeles fueron adaptados a `width: 100%; max-width: ...px; padding: 16px;` para ajustarse automáticamente a dispositivos pequeños.
4. **Respeto a la Paleta de Diseño**: Uso exclusivo de tokens y variables HSL de `styles.css` (0 colores hexadecimales introducidos).
5. **Preservación Funcional**: Sin cambios en la lógica TypeScript de los componentes.

---

## 2. Diagnóstico y Correcciones por Pantalla

### 1. Pacientes (`app/pacientes/`)
- **Problema previo**:
  - `.table-container` utilizaba `overflow: hidden;`, provocando corte de columnas de acciones en pantallas menores a 900px.
  - Modal de alta/edición tenía ancho fijo `width: 560px`.
  - Barra de filtros y botones de estado (`.filter-toolbar`) desbordaban en `<= 768px`.
  - Zona de carga de fotografía de perfil (`.photo-upload-section`) no fluía verticalmente en resoluciones móviles.
- **Solución aplicada**:
  - Se configuró `overflow-x: auto;` y `min-width: 650px` en `.data-table`.
  - Modal con `max-width: 560px` y `width: 100%`.
  - `@media (max-width: 768px)`: Barra de filtros y sección de foto en `flex-direction: column`, formularios en 1 columna.
  - `@media (max-width: 480px)`: Botones del footer del modal apilados a ancho completo.

### 2. Profesionales (`app/profesionales/`)
- **Problema previo**:
  - `.table-container` con `overflow: hidden;`.
  - Modal de edición con `width: 520px` rígido.
  - `.form-row` con 2 columnas sin colapso en pantallas reducidas.
- **Solución aplicada**:
  - Se configuró `overflow-x: auto;` con tabla asegurada en `min-width: 650px`.
  - Modal con ancho relativo y `max-width: 520px`.
  - Formularios a 1 columna y botones adaptados en `<= 768px` y `<= 480px`.

### 3. Turnos (`app/turnos/`)
- **Problema previo**:
  - `.table-container` con `overflow: hidden;`.
  - `.filters-bar` no envolvía los elementos en móvil (buscador + selector de profesional/estado).
  - Selector de vista Listado / Agenda (`.vista-switch`) desbordaba en teléfonos.
  - Modal con `width: 560px`.
- **Solución aplicada**:
  - Se habilitó scroll horizontal autónomo en la tabla.
  - En `<= 768px`, `.filters-bar` colapsa verticalmente y `.vista-switch` ocupa el 100% del ancho con botones equidistribuidos (`flex: 1`).
  - Modal fluido con max-width 560px.

### 4. Sala de Espera (`app/sala-espera/`)
- **Problema previo**:
  - Si bien disponía de reglas para 768px, el grid de tarjetas de pacientes (`.waiting-board`) utilizaba `minmax(360px, 1fr)`, produciendo desborde en pantallas de 320px–360px.
  - El modal de llamado (`.call-modal-card`) y las acciones de tarjeta (`.card-actions`) se comprimían excesivamente en `<= 480px`.
- **Solución aplicada**:
  - Reducción de la columna mínima a `minmax(280px, 1fr)`.
  - Se agregó `@media (max-width: 480px)` con ajuste de tamaños tipográficos de cartelera, padding adaptativo en modales y flex-wrap en botones de acción.

### 5. Historia Clínica (`app/historia-clinica/`)
- **Problema previo**:
  - Layout principal `.hc-grid` definido en `grid-template-columns: 1fr 340px;` rompía el diseño en tablets y laptops de 1024px o menos.
  - Selector superior de paciente no fluía bien en pantallas reducidas.
  - Modales de evolución/diagnóstico y carga de adjuntos no eran responsivos.
- **Solución aplicada**:
  - `@media (max-width: 1024px)`: `.hc-grid` pasa a `grid-template-columns: 1fr;` apilando el historial y el panel lateral de antecedentes/alergias de forma limpia.
  - Selector de paciente y formularios colapsan verticalmente en `<= 768px`.
  - Modales fluidos con `max-width: 560px` y `max-width: 640px`.

### 6. Prescripciones (`app/prescripciones/`)
- **Problema previo**:
  - `.prescripciones-grid` con `minmax(360px, 1fr)` desbordaba en pantallas pequeñas.
  - La previsualización de impresión de receta médica (`.receta-print-box`) no tenía control de desbordamiento horizontal.
  - Encabezado con selector de paciente y métricas no colapsaba en móvil.
- **Solución aplicada**:
  - Grid con `minmax(280px, 1fr)`.
  - `.receta-print-box` con `overflow-x: auto;` y padding reducido en móvil.
  - Colapso vertical de selectores y tarjeta de datos del paciente en `<= 768px`.

### 7. Facturación (`app/facturacion/`)
- **Problema previo**:
  - `.table-container` con `overflow: hidden;`.
  - Modal grande (`.modal-lg`) fijado en `680px`.
  - Fila de agregado de ítems a la factura (`.detalle-row`) desbordaba horizontalmente al contener selectores y campos de cantidad/precio juntos.
- **Solución aplicada**:
  - Contenedor con `overflow-x: auto;` y `min-width: 650px` en tabla.
  - Modal fluido con `max-width: 680px`.
  - `.detalle-row` con `flex-wrap: wrap;` en `<= 768px`, permitiendo que el selector ocupe la primera línea y los valores numéricos la segunda.

### 8. Liquidaciones (`app/liquidaciones/`)
- **Problema previo**:
  - `.table-container` con `overflow: hidden;`.
  - Modal fijo en `500px`.
  - `.form-row` con flex sin wrap.
- **Solución aplicada**:
  - Tabla con scroll horizontal autónomo.
  - Modal fluido con `max-width: 500px`.
  - `.form-row` pasa a `flex-direction: column` en `<= 768px`.

### 9. Obras Sociales (`app/obras-sociales/`)
- **Problema previo**:
  - Tabla con `overflow: hidden;`.
  - Modal con `width: 460px`.
- **Solución aplicada**:
  - `overflow-x: auto;` en contenedor de tabla con `min-width: 650px`.
  - Modal adaptativo a ancho de pantalla.

### 10. Especialidades (`app/especialidades/`)
- **Problema previo**:
  - Tabla con `overflow: hidden;`.
  - Modal con `width: 460px`.
- **Solución aplicada**:
  - `overflow-x: auto;` con `min-width: 550px`.
  - Modal fluido con `max-width: 460px`.

### 11. Medicamentos (`app/medicamentos/`)
- **Problema previo**:
  - Tabla con `overflow: hidden;`.
  - Modal con `width: 460px`.
- **Solución aplicada**:
  - `overflow-x: auto;` con `min-width: 550px`.
  - Modal fluido con `max-width: 460px`.

### 12. Prestaciones (`app/prestaciones/`)
- **Problema previo**:
  - Tabla con `overflow: hidden;`.
  - Modal fijo en `500px`.
  - `.form-row` de 2 columnas rígido.
- **Solución aplicada**:
  - `overflow-x: auto;` con `min-width: 550px`.
  - Modal fluido con `max-width: 500px`.
  - Formulario a 1 columna en móvil.

### 13. Usuarios (`app/usuarios/`)
- **Problema previo**:
  - Tabla con `overflow: hidden;`.
  - Modal fijo en `460px`.
- **Solución aplicada**:
  - `overflow-x: auto;` con `min-width: 650px`.
  - Modal fluido con `max-width: 460px`.

### 14. Perfil de Usuario (`app/perfil/`)
- **Problema previo**:
  - Grid de perfil (`.profile-grid`) en 2 columnas fijas `1fr 1fr` que apretaba los formularios y datos de usuario en pantallas menores a 900px.
  - Sección de avatar y filas de datos de contacto no colapsaban en <= 480px.
- **Solución aplicada**:
  - `@media (max-width: 900px)`: `.profile-grid` colapsa a `grid-template-columns: 1fr;`.
  - `@media (max-width: 480px)`: Avatar centrado verticalmente y filas clave-valor apiladas para evitar textos truncados.

---

## 3. Matriz de Verificación

| Breakpoint | Comportamiento Verificado |
|---|---|
| **1024px** (Desktop compacto / iPad Pro) | Layouts de 2 columnas colapsan cuando es necesario (ej. Historia Clínica `hc-grid`). Sin scroll horizontal en la ventana. |
| **768px** (Tablets estándar) | Formularios pasan a 1 sola columna. Barras de búsqueda y filtros colapsan verticalmente. Las tablas mantienen legibilidad mediante scroll horizontal interno del contenedor. |
| **480px** (Dispositivos móviles) | Modales ocupan el 100% del viewport disponible con márgenes seguros de 16px. Botones de acción ocupan el ancho completo para facilitar toque táctil. |

---

## 4. Resultados de Pruebas Automatizadas
- **Pruebas Unitarias Vitest**: 44 tests ejecutados y aprobados (100% passing).
- **Linter de Colores CSS**: 0 ocurrencias de códigos hexadecimales manuales en `src/app/`.
- **Compilación de Producción Angular**: Build exitoso sin errores de template ni estilos.
