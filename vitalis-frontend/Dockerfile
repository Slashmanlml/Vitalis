# ---------------------------------------------------------------------------
# Frontend de Vitalis (Angular 22)
#
# Igual que la API, en dos etapas: Node compila, y el resultado (HTML, CSS y JS
# estaticos) se sirve con nginx. La imagen final no tiene Node ni node_modules.
# ---------------------------------------------------------------------------

FROM node:22-alpine AS build
WORKDIR /origen

# npm ci en lugar de npm install: instala exactamente lo que dice
# package-lock.json. Una construccion reproducible no puede depender de que hoy
# salga una version nueva de una dependencia.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# La configuracion de produccion reemplaza environment.ts por environment.prod.ts,
# donde apiUrl es '/api' (relativo): el frontend y la API quedan bajo el mismo
# origen y no hace falta CORS.
RUN npx ng build --configuration production

# --- imagen final ---
FROM nginx:alpine AS final
COPY --from=build /origen/dist/vitalis-frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
