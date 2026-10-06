# Wraps the dotnet-ef invocations so nobody has to retype four flags.
DB_PROJECT      := src/ChefLan.DB/ChefLan.DB.csproj
STARTUP_PROJECT := src/ChefLan.Courses.API/ChefLan.Courses.API.csproj
CONTEXT         := ChefLan.DB.Contexts.CoursesDbContext
MIGRATIONS_DIR  := Migrations/Courses
EF              := dotnet dotnet-ef

.PHONY: help up down logs build run migration migrate migration-remove script psql reset

help:
	@grep -E '^[a-zA-Z-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-18s\033[0m %s\n", $$1, $$2}'

up: ## Start Postgres and pgAdmin
	docker compose up -d

down: ## Stop the containers
	docker compose down

logs: ## Tail the Postgres log
	docker compose logs -f postgres

build: ## Build the whole solution
	dotnet build

run: ## Run the Courses API
	dotnet run --project $(STARTUP_PROJECT)

migration: ## Add a migration: make migration NAME=AddSomething
	@test -n "$(NAME)" || (echo "NAME is required, e.g. make migration NAME=AddPayments" && exit 1)
	$(EF) migrations add $(NAME) --project $(DB_PROJECT) --startup-project $(STARTUP_PROJECT) --context $(CONTEXT) --output-dir $(MIGRATIONS_DIR)

migrate: ## Apply pending migrations to the local database
	$(EF) database update --project $(DB_PROJECT) --startup-project $(STARTUP_PROJECT) --context $(CONTEXT)

migration-remove: ## Remove the last (unapplied) migration
	$(EF) migrations remove --project $(DB_PROJECT) --startup-project $(STARTUP_PROJECT) --context $(CONTEXT)

script: ## Produce the idempotent SQL deploy artifact for review
	@mkdir -p artifacts
	$(EF) migrations script --idempotent --output artifacts/cheflan-courses.sql --project $(DB_PROJECT) --startup-project $(STARTUP_PROJECT) --context $(CONTEXT)

psql: ## Open a psql shell on the local database
	docker exec -it cheflan-postgres psql -U cheflan -d cheflan

reset: ## Drop and recreate the local database (destroys all local data)
	$(EF) database drop --force --project $(DB_PROJECT) --startup-project $(STARTUP_PROJECT) --context $(CONTEXT)
	$(MAKE) migrate
