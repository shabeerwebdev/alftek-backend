# ==================================================
# AlfTekPro HRMS - Makefile for Docker Operations
# ==================================================
# Convenient commands for managing the development environment

.PHONY: help up down restart logs clean rebuild test migrate backup restore

# Default target
.DEFAULT_GOAL := help

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

# --------------------------------------------------
# Help
# --------------------------------------------------
help: ## Show this help message
	@echo "$(BLUE)AlfTekPro HRMS - Available Commands$(NC)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "$(GREEN)%-20s$(NC) %s\n", $$1, $$2}'
	@echo ""

# --------------------------------------------------
# Environment Setup
# --------------------------------------------------
setup: ## Initial setup (copy .env.example to .env)
	@if [ ! -f .env ]; then \
		echo "$(YELLOW)Creating .env file from .env.example...$(NC)"; \
		cp .env.example .env; \
		echo "$(GREEN)✓ .env file created. Please review and update values.$(NC)"; \
	else \
		echo "$(YELLOW).env file already exists.$(NC)"; \
	fi

# --------------------------------------------------
# Docker Compose Operations
# --------------------------------------------------
up: ## Start all services
	@echo "$(BLUE)Starting all services...$(NC)"
	docker-compose up -d
	@echo "$(GREEN)✓ Services started successfully$(NC)"
	@make status

up-tools: ## Start all services including tools (pgAdmin, Azurite)
	@echo "$(BLUE)Starting all services with tools...$(NC)"
	docker-compose --profile tools up -d
	@echo "$(GREEN)✓ Services started successfully$(NC)"
	@make status

down: ## Stop all services
	@echo "$(BLUE)Stopping all services...$(NC)"
	docker-compose down
	@echo "$(GREEN)✓ Services stopped$(NC)"

down-volumes: ## Stop all services and remove volumes (⚠️  DELETES ALL DATA)
	@echo "$(RED)⚠️  WARNING: This will delete all data!$(NC)"
	@read -p "Are you sure? (y/N): " confirm && [ "$$confirm" = "y" ] || exit 1
	docker-compose down -v
	@echo "$(GREEN)✓ Services stopped and volumes removed$(NC)"

restart: ## Restart all services
	@echo "$(BLUE)Restarting all services...$(NC)"
	docker-compose restart
	@echo "$(GREEN)✓ Services restarted$(NC)"

restart-api: ## Restart API service only
	@echo "$(BLUE)Restarting API service...$(NC)"
	docker-compose restart api
	@echo "$(GREEN)✓ API service restarted$(NC)"

restart-worker: ## Restart Worker service only
	@echo "$(BLUE)Restarting Worker service...$(NC)"
	docker-compose restart worker
	@echo "$(GREEN)✓ Worker service restarted$(NC)"

# --------------------------------------------------
# Build Operations
# --------------------------------------------------
build: ## Build all services
	@echo "$(BLUE)Building all services...$(NC)"
	docker-compose build
	@echo "$(GREEN)✓ Build completed$(NC)"

rebuild: ## Rebuild and restart all services
	@echo "$(BLUE)Rebuilding all services...$(NC)"
	docker-compose up -d --build
	@echo "$(GREEN)✓ Services rebuilt and restarted$(NC)"

rebuild-api: ## Rebuild and restart API service only
	@echo "$(BLUE)Rebuilding API service...$(NC)"
	docker-compose up -d --build api
	@echo "$(GREEN)✓ API service rebuilt and restarted$(NC)"

rebuild-worker: ## Rebuild and restart Worker service only
	@echo "$(BLUE)Rebuilding Worker service...$(NC)"
	docker-compose up -d --build worker
	@echo "$(GREEN)✓ Worker service rebuilt and restarted$(NC)"

# --------------------------------------------------
# Logs
# --------------------------------------------------
logs: ## View logs from all services
	docker-compose logs -f

logs-api: ## View API logs
	docker-compose logs -f api

logs-worker: ## View Worker logs
	docker-compose logs -f worker

logs-postgres: ## View PostgreSQL logs
	docker-compose logs -f postgres

logs-redis: ## View Redis logs
	docker-compose logs -f redis

# --------------------------------------------------
# Status & Monitoring
# --------------------------------------------------
status: ## Show status of all services
	@echo "$(BLUE)Service Status:$(NC)"
	@docker-compose ps

health: ## Check health of all services
	@echo "$(BLUE)Health Check:$(NC)"
	@docker ps --filter "name=alftekpro" --format "table {{.Names}}\t{{.Status}}"

# --------------------------------------------------
# Shell Access
# --------------------------------------------------
shell-api: ## Access API container shell
	docker exec -it alftekpro-api sh

shell-worker: ## Access Worker container shell
	docker exec -it alftekpro-worker sh

shell-postgres: ## Access PostgreSQL shell
	docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms

shell-redis: ## Access Redis CLI
	docker exec -it alftekpro-redis redis-cli

# --------------------------------------------------
# Database Operations
# --------------------------------------------------
migrate: ## Run database migrations
	@echo "$(BLUE)Running database migrations...$(NC)"
	docker exec -it alftekpro-api dotnet ef database update
	@echo "$(GREEN)✓ Migrations completed$(NC)"

migrate-create: ## Create a new migration (usage: make migrate-create name=MigrationName)
	@if [ -z "$(name)" ]; then \
		echo "$(RED)Error: Migration name is required$(NC)"; \
		echo "Usage: make migrate-create name=MigrationName"; \
		exit 1; \
	fi
	docker exec -it alftekpro-api dotnet ef migrations add $(name)

migrate-rollback: ## Rollback last migration
	@echo "$(YELLOW)Rolling back last migration...$(NC)"
	docker exec -it alftekpro-api dotnet ef database update 0
	@echo "$(GREEN)✓ Rollback completed$(NC)"

seed: ## Seed database with initial data
	@echo "$(BLUE)Seeding database...$(NC)"
	docker exec -it alftekpro-api dotnet run --project /app/src/AlfTekPro.API -- seed
	@echo "$(GREEN)✓ Database seeded$(NC)"

# --------------------------------------------------
# Database Backup & Restore
# --------------------------------------------------
backup: ## Backup database to ./backups/
	@mkdir -p backups
	@echo "$(BLUE)Creating database backup...$(NC)"
	docker exec alftekpro-postgres pg_dump -U hrms_user alftekpro_hrms > backups/backup_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "$(GREEN)✓ Backup created in ./backups/$(NC)"

restore: ## Restore database from latest backup
	@if [ -z "$$(ls -t backups/*.sql 2>/dev/null | head -1)" ]; then \
		echo "$(RED)Error: No backup files found in ./backups/$(NC)"; \
		exit 1; \
	fi
	@echo "$(YELLOW)Restoring from latest backup...$(NC)"
	@latest=$$(ls -t backups/*.sql | head -1); \
	echo "$(BLUE)Restoring from: $$latest$(NC)"; \
	docker exec -i alftekpro-postgres psql -U hrms_user -d alftekpro_hrms < $$latest
	@echo "$(GREEN)✓ Database restored$(NC)"

# --------------------------------------------------
# Testing
# --------------------------------------------------
test: ## Run all tests
	@echo "$(BLUE)Running tests...$(NC)"
	docker exec -it alftekpro-api dotnet test

test-unit: ## Run unit tests only
	@echo "$(BLUE)Running unit tests...$(NC)"
	docker exec -it alftekpro-api dotnet test --filter Category=Unit

test-integration: ## Run integration tests only
	@echo "$(BLUE)Running integration tests...$(NC)"
	docker exec -it alftekpro-api dotnet test --filter Category=Integration

test-coverage: ## Run tests with code coverage
	@echo "$(BLUE)Running tests with coverage...$(NC)"
	docker exec -it alftekpro-api dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# --------------------------------------------------
# Cleanup
# --------------------------------------------------
clean: ## Remove stopped containers and dangling images
	@echo "$(BLUE)Cleaning up...$(NC)"
	docker-compose down --remove-orphans
	docker system prune -f
	@echo "$(GREEN)✓ Cleanup completed$(NC)"

clean-all: ## Remove all containers, images, and volumes (⚠️  NUCLEAR OPTION)
	@echo "$(RED)⚠️  WARNING: This will remove ALL Docker resources for this project!$(NC)"
	@read -p "Are you sure? (y/N): " confirm && [ "$$confirm" = "y" ] || exit 1
	docker-compose down -v --rmi all
	docker system prune -af --volumes
	@echo "$(GREEN)✓ All resources removed$(NC)"

# --------------------------------------------------
# Development Utilities
# --------------------------------------------------
format: ## Format code (requires .NET SDK)
	@echo "$(BLUE)Formatting code...$(NC)"
	dotnet format backend/

watch-api: ## Watch API logs with auto-reload
	docker-compose logs -f api

ports: ## Show port mappings
	@echo "$(BLUE)Port Mappings:$(NC)"
	@echo "API:       http://localhost:5001"
	@echo "Swagger:   http://localhost:5001/swagger"
	@echo "Hangfire:  http://localhost:5001/hangfire"
	@echo "pgAdmin:   http://localhost:5050"
	@echo "PostgreSQL: localhost:5432"
	@echo "Redis:     localhost:6379"

urls: ## Show all service URLs
	@make ports

# --------------------------------------------------
# Production Build
# --------------------------------------------------
prod-build: ## Build production images
	@echo "$(BLUE)Building production images...$(NC)"
	docker-compose -f docker-compose.prod.yml build
	@echo "$(GREEN)✓ Production images built$(NC)"

prod-push: ## Push images to registry (requires DOCKER_REGISTRY env var)
	@if [ -z "$(DOCKER_REGISTRY)" ]; then \
		echo "$(RED)Error: DOCKER_REGISTRY environment variable not set$(NC)"; \
		exit 1; \
	fi
	@echo "$(BLUE)Pushing images to $(DOCKER_REGISTRY)...$(NC)"
	docker-compose -f docker-compose.prod.yml push
	@echo "$(GREEN)✓ Images pushed successfully$(NC)"
