# 🚀 Quick Start Guide

Get the AlfTekPro HRMS running in **5 minutes**!

## Prerequisites

- **Docker Desktop** installed and running
- **Git** installed

## Step 1: Clone & Setup

```bash
# Clone the repository
git clone <repository-url>
cd alftekpro

# Copy environment file
cp .env.example .env

# (Optional) Review and update .env file if needed
# For local development, default values work fine!
```

## Step 2: Start the Application

### Option A: Using Make (Recommended)

```bash
# Start all services
make up

# Start with tools (pgAdmin, Azurite)
make up-tools
```

### Option B: Using Docker Compose Directly

```bash
# Start core services
docker-compose up -d

# Or with tools
docker-compose --profile tools up -d
```

## Step 3: Verify It's Running

```bash
# Check service status
make status

# Or
docker-compose ps
```

Expected output:
```
NAME                 STATUS              PORTS
alftekpro-api        Up (healthy)        0.0.0.0:5001->80/tcp
alftekpro-worker     Up (healthy)
alftekpro-postgres   Up (healthy)        0.0.0.0:5432->5432/tcp
alftekpro-redis      Up (healthy)        0.0.0.0:6379->6379/tcp
```

## Step 4: Access the Application

| Service | URL | Credentials |
|---------|-----|-------------|
| **API Swagger** | http://localhost:5001/swagger | - |
| **Hangfire Dashboard** | http://localhost:5001/hangfire | - |
| **pgAdmin** | http://localhost:5050 | admin@alftekpro.com / admin123 |

## 🎉 You're Done!

The API is now running and ready for development.

## Next Steps

### 1. Run Database Migrations

```bash
# Run migrations (when .NET projects are created)
make migrate
```

### 2. Seed Initial Data

```bash
# Seed database with regions, sample tenants, etc.
make seed
```

### 3. Test the API

Open **Swagger UI**: http://localhost:5001/swagger

Try the following endpoints:
- `GET /regions` - Get all regions (no auth required)
- `POST /tenants/onboard` - Create a new tenant
- `POST /auth/login` - Login with tenant credentials

### 4. View Logs

```bash
# View all logs
make logs

# View API logs only
make logs-api

# View Worker logs
make logs-worker
```

## 🛠️ Common Commands

```bash
# Stop services
make down

# Restart services
make restart

# Rebuild after code changes
make rebuild

# Run tests
make test

# Create database backup
make backup

# View all available commands
make help
```

## 🐛 Troubleshooting

### Port Already in Use

If you see an error about port 5001 being in use:

```bash
# Change API_PORT in .env file
API_PORT=5002

# Restart services
make down
make up
```

### Database Connection Failed

```bash
# Check PostgreSQL is running
docker-compose ps postgres

# View PostgreSQL logs
make logs-postgres

# Restart PostgreSQL
docker-compose restart postgres
```

### Redis Connection Failed

```bash
# Check Redis is running
docker-compose ps redis

# Restart Redis
docker-compose restart redis
```

### Services Won't Start

```bash
# Clean up and start fresh
make clean
make up
```

### Nuclear Option (⚠️ Deletes All Data)

```bash
# Remove everything and start fresh
make down-volumes
make up
```

## 📚 Additional Resources

- [Full README](README.md) - Complete documentation
- [OpenAPI Spec](openapi.yaml) - API documentation
- [System Design](SYSTEM_DESIGN.md) - Architecture details
- [Implementation Plan](.claude/plans/stateful-growing-tome.md)

## 🆘 Need Help?

- Check logs: `make logs`
- View service status: `make status`
- Check health: `make health`
- Read the full [README.md](README.md)

---

**Happy Coding!** 🎯
