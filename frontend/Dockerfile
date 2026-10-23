# ==============================================================================
# Multi-stage Dockerfile for Angular 22 Frontend with Nginx Reverse Proxy
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build Application
# ------------------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies using package-lock.json for deterministic builds
COPY package.json package-lock.json ./
RUN npm ci

# Copy frontend source code and configurations
COPY . .

# Run production build (outputs to dist/frontend/browser)
RUN npm run build

# ------------------------------------------------------------------------------
# Stage 2: Serve with Nginx
# ------------------------------------------------------------------------------
FROM nginx:alpine AS runtime

# Copy compiled Angular browser artifacts to Nginx web root
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Copy custom Nginx configuration supporting Angular routing and API proxying
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
