# ==================================================
# AlfTekPro HRMS - API Service Dockerfile
# ==================================================
# Multi-stage build for .NET 8 Web API
# Supports both development (hot reload) and production (optimized) targets

# --------------------------------------------------
# Stage 1: Base Runtime Image
# --------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install required dependencies for PostgreSQL and other libraries
RUN apk add --no-cache \
    icu-libs \
    krb5-libs \
    libgcc \
    libintl \
    libssl3 \
    libstdc++ \
    zlib

# Set environment variables for globalization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8

# --------------------------------------------------
# Stage 2: Development Image (with hot reload)
# --------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
WORKDIR /app

# Install dotnet-ef tools for migrations
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy solution and project files first (for layer caching)
COPY ["src/AlfTekPro.Domain/AlfTekPro.Domain.csproj", "src/AlfTekPro.Domain/"]
COPY ["src/AlfTekPro.Application/AlfTekPro.Application.csproj", "src/AlfTekPro.Application/"]
COPY ["src/AlfTekPro.Infrastructure/AlfTekPro.Infrastructure.csproj", "src/AlfTekPro.Infrastructure/"]
COPY ["src/AlfTekPro.API/AlfTekPro.API.csproj", "src/AlfTekPro.API/"]

# Restore dependencies (cached layer if csproj files haven't changed)
RUN dotnet restore "src/AlfTekPro.API/AlfTekPro.API.csproj"

# Copy the rest of the source code
COPY . .

# Set working directory to API project
WORKDIR /app/src/AlfTekPro.API

# Development entry point with hot reload enabled
ENTRYPOINT ["dotnet", "watch", "run", "--no-restore", "--urls", "http://+:80"]

# --------------------------------------------------
# Stage 3: Build Stage
# --------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/AlfTekPro.Domain/AlfTekPro.Domain.csproj", "src/AlfTekPro.Domain/"]
COPY ["src/AlfTekPro.Application/AlfTekPro.Application.csproj", "src/AlfTekPro.Application/"]
COPY ["src/AlfTekPro.Infrastructure/AlfTekPro.Infrastructure.csproj", "src/AlfTekPro.Infrastructure/"]
COPY ["src/AlfTekPro.API/AlfTekPro.API.csproj", "src/AlfTekPro.API/"]

# Restore NuGet packages
RUN dotnet restore "src/AlfTekPro.API/AlfTekPro.API.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR /src/src/AlfTekPro.API
RUN dotnet build "AlfTekPro.API.csproj" -c Release -o /app/build --no-restore

# --------------------------------------------------
# Stage 4: Publish Stage
# --------------------------------------------------
FROM build AS publish
RUN dotnet publish "AlfTekPro.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# --------------------------------------------------
# Stage 5: Production Runtime
# --------------------------------------------------
FROM base AS production
WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser

# Copy published output from publish stage
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "AlfTekPro.API.dll"]
