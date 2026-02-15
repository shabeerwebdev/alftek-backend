# AlfTekPro Multi-Tenant HRMS

A comprehensive, multi-tenant Human Resource Management System built with **.NET 8**, **PostgreSQL 15**, **Redis**, and **Docker**.

## 🏗️ Architecture

- **Pattern**: Modular Monolith with Clean Architecture
- **Multi-Tenancy**: Row-Level Isolation (Shared Database, Shared Schema)
- **Regions**: UAE (RTL/Arabic), USA (LTR/English), India (LTR/Hindi)
- **API**: RESTful with OpenAPI 3.0 specification
- **Database**: PostgreSQL 15 with JSONB for flexible schemas
- **Caching**: Redis for permissions, regions, and form schemas
- **Background Jobs**: Hangfire for payroll processing and bulk imports
- **Storage**: Azure Blob Storage (local: Azurite emulator)

## 📋 Prerequisites

- **Docker Desktop** (v20.10+) - [Download](https://www.docker.com/products/docker-desktop)
- **Docker Compose** (v2.0+) - Included with Docker Desktop
- **.NET 8 SDK** (for local development without Docker) - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Git** - [Download](https://git-scm.com/downloads)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd alftekpro
```

### 2. Configure Environment Variables

```bash
# Copy example environment file
cp .env.example .env

# Edit .env file and update values (especially JWT_SECRET for production)
# For local development, default values work fine
```

### 3. Start All Services

```bash
# Start core services (API, Worker, PostgreSQL, Redis)
docker-compose up -d

# Or start with tools (pgAdmin, Azurite)
docker-compose --profile tools up -d
```

### 4. Verify Services Are Running

```bash
# Check all containers are healthy
docker-compose ps

# Expected output:
# alftekpro-api       running   0.0.0.0:5001->80/tcp
# alftekpro-worker    running
# alftekpro-postgres  running   0.0.0.0:5432->5432/tcp
# alftekpro-redis     running   0.0.0.0:6379->6379/tcp
```

### 5. Access the Application

| Service | URL | Credentials |
|---------|-----|-------------|
| **API** | http://localhost:5001 | - |
| **API Swagger** | http://localhost:5001/swagger | - |
| **Hangfire Dashboard** | http://localhost:5001/hangfire | - |
| **pgAdmin** | http://localhost:5050 | admin@alftekpro.com / admin123 |
| **PostgreSQL** | localhost:5432 | hrms_user / hrms_dev_pass_2024 |
| **Redis** | localhost:6379 | Password: redis_dev_pass_2024 |

## 🛠️ Development Workflow

### View Logs

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f worker
docker-compose logs -f postgres
```

### Restart Services

```bash
# Restart specific service
docker-compose restart api

# Restart all services
docker-compose restart
```

### Stop Services

```bash
# Stop all services (keeps data)
docker-compose stop

# Stop and remove containers (keeps volumes)
docker-compose down

# Stop and remove everything including volumes (⚠️ DELETES ALL DATA)
docker-compose down -v
```

### Rebuild After Code Changes

```bash
# Rebuild and restart API
docker-compose up -d --build api

# Rebuild and restart Worker
docker-compose up -d --build worker
```

### Run Database Migrations

```bash
# Access API container
docker exec -it alftekpro-api sh

# Inside container, run migrations
dotnet ef database update

# Exit container
exit
```

### Access PostgreSQL Database

```bash
# Using psql command line
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms

# Or use pgAdmin at http://localhost:5050
```

### Access Redis CLI

```bash
# Access Redis container
docker exec -it alftekpro-redis redis-cli

# Authenticate
AUTH redis_dev_pass_2024

# Test connection
PING
# Should return: PONG
```

## 📁 Project Structure

```
alftekpro/
├── backend/                    # .NET 8 Backend
│   ├── src/
│   │   ├── AlfTekPro.Domain/           # Domain entities, interfaces
│   │   ├── AlfTekPro.Application/      # Business logic, services
│   │   ├── AlfTekPro.Infrastructure/   # Data access, external services
│   │   └── AlfTekPro.API/              # Web API controllers, middleware
│   ├── tests/
│   │   ├── AlfTekPro.UnitTests/
│   │   └── AlfTekPro.IntegrationTests/
│   ├── Dockerfile                      # API Dockerfile
│   └── Dockerfile.Worker               # Worker Dockerfile
├── scripts/
│   └── init-db.sql             # Database initialization
├── docs/                       # Documentation
├── docker-compose.yml          # Docker orchestration
├── .env.example                # Environment variables template
├── .dockerignore               # Docker ignore rules
├── openapi.yaml                # API specification
└── README.md                   # This file
```

## 🧪 Running Tests

```bash
# Run all tests in the container (Integration tests may fail if Docker socket is not shared)
docker exec -it alftekpro-api dotnet test /app/tests

# Run unit tests only
docker exec -it alftekpro-api dotnet test /app/tests/AlfTekPro.UnitTests

# Run integration tests only
# NOTE: Integration tests use Testcontainers and require access to the Docker daemon.
# These tests are best run locally on the host machine.
docker exec -it alftekpro-api dotnet test /app/tests/AlfTekPro.IntegrationTests

# Run with coverage
docker exec -it alftekpro-api dotnet test /app/tests /p:CollectCoverage=true
```

## 🔍 Troubleshooting

### Issue: Port Already in Use

```bash
# Find process using port 5001
netstat -ano | findstr :5001    # Windows
lsof -i :5001                   # macOS/Linux

# Kill the process or change API_PORT in .env file
```

### Issue: Database Connection Failed

```bash
# Check PostgreSQL is running
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Restart PostgreSQL
docker-compose restart postgres
```

### Issue: Redis Connection Failed

```bash
# Check Redis is running
docker-compose ps redis

# Test Redis connection
docker exec -it alftekpro-redis redis-cli PING
```

### Issue: Cannot Access Swagger UI

```bash
# Ensure API is running
docker-compose ps api

# Check API logs for errors
docker-compose logs api

# Restart API
docker-compose restart api
```

### Issue: Hot Reload Not Working

Hot reload is enabled via volume mounts. If changes aren't reflecting:

```bash
# Rebuild the container
docker-compose up -d --build api

# Or restart without rebuild
docker-compose restart api
```

## 📊 Database Management

### Backup Database

```bash
# Create backup
docker exec alftekpro-postgres pg_dump -U hrms_user alftekpro_hrms > backup.sql

# With timestamp
docker exec alftekpro-postgres pg_dump -U hrms_user alftekpro_hrms > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore Database

```bash
# Restore from backup
docker exec -i alftekpro-postgres psql -U hrms_user -d alftekpro_hrms < backup.sql
```

### Reset Database

```bash
# Stop services
docker-compose down

# Remove volumes (⚠️ DELETES ALL DATA)
docker volume rm alftekpro-postgres-data

# Start services (will recreate database)
docker-compose up -d
```

## 🔐 Security Notes

### For Development

- Default passwords are intentionally simple for local development
- JWT secret is a placeholder and not secure
- HTTPS is disabled for simplicity

### For Production

1. **Change all passwords** in `.env` file
2. **Generate strong JWT secret**: `openssl rand -base64 32`
3. **Enable HTTPS** with valid SSL certificates
4. **Use Azure Key Vault** for secrets management
5. **Restrict database access** with firewall rules
6. **Enable Redis AUTH** with strong password
7. **Review Azure Storage** access policies

## 📝 Environment Variables Reference

| Variable | Default | Description |
|----------|---------|-------------|
| `POSTGRES_DB` | alftekpro_hrms | PostgreSQL database name |
| `POSTGRES_USER` | hrms_user | PostgreSQL username |
| `POSTGRES_PASSWORD` | hrms_dev_pass_2024 | PostgreSQL password |
| `POSTGRES_PORT` | 5432 | PostgreSQL port |
| `REDIS_PASSWORD` | redis_dev_pass_2024 | Redis password |
| `REDIS_PORT` | 6379 | Redis port |
| `API_PORT` | 5001 | API exposed port |
| `JWT_SECRET` | (placeholder) | JWT signing key (min 32 chars) |
| `AZURE_STORAGE_CONNECTION` | UseDevelopmentStorage=true | Azure Storage connection string |

## 🚢 Deployment

### Deploy to Azure

See `docs/deployment-azure.md` for detailed Azure deployment instructions.

### Deploy with GitHub Actions

See `.github/workflows/` for CI/CD pipeline configuration.

## 📚 Additional Documentation

- [System Design Document](SYSTEM_DESIGN.md)
- [API Documentation](openapi.yaml) - View in [Swagger Editor](https://editor.swagger.io/)
- [Implementation Plan](.claude/plans/stateful-growing-tome.md)
- Database Schema: See `docs/database-schema.md` (to be created)
- Architecture Diagrams: See `docs/architecture/` (to be created)

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/your-feature-name`
2. Make your changes
3. Run tests: `docker exec -it alftekpro-api dotnet test`
4. Commit changes: `git commit -m "feat: your feature description"`
5. Push branch: `git push origin feature/your-feature-name`
6. Create Pull Request

## 📄 License

Proprietary - AlfTekPro © 2024

## 📞 Support

For issues and questions:
- Email: support@alftekpro.com
- Documentation: [Internal Wiki](#)
- Bug Reports: [GitHub Issues](#)

---

**Happy Coding!** 🚀
# alftek-backend
