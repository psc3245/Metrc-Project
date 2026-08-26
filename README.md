# Metrc-Project

A lightweight Jira-style task management API built for the Metrc backend
engineering take-home assignment. .NET 8, PostgreSQL, JWT auth, participant-based
authorization, and a three-layer testing strategy (unit, integration, and shell
smoke tests).

## Tech Stack

- **.NET 8** / ASP.NET Core Web API
- **PostgreSQL** via Npgsql + EF Core
- **JWT Bearer authentication**
- **xUnit** + **Moq** (unit tests) + `Microsoft.AspNetCore.Mvc.Testing` (integration tests)
- **Docker Compose** for local Postgres

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local Postgres)
- `dotnet-ef` CLI tool: `dotnet tool install --global dotnet-ef`

## Getting Started

```bash
# 1. Start the docker container
docker-compose up -d
docker-compose ps   # confirm it's healthy

# 2. Apply migrations
cd API/API
dotnet ef database update

# 3. Run the API
dotnet run
```

The API listens on `http://localhost:5224` by default. Swagger UI is available at
`http://localhost:5224/swagger` and includes an **Authorize** button for testing
protected endpoints - sign up or log in via `/api/Auth`, paste the returned token
in (no `Bearer ` prefix needed), and you can exercise every endpoint from the
Swagger page directly.

> **Note on the committed JWT signing key:** `appsettings.Development.json`
> contains a plaintext `Jwt:Key` value. This is intentional for grading
> convenience - in a real deployment this would come from environment
> variables or a secrets manager (e.g. `dotnet user-secrets` locally, a cloud
> secrets manager in production), never from source control.

## Running the Tests

```bash
# Unit + integration tests (xUnit)
cd API
dotnet test
```

## API Overview

All endpoints except `/api/Auth/*` require a `Bearer` token. All mutating
endpoints additionally require the caller to be a **participant** of the
relevant project.

| Resource | Endpoints |
|---|---|
| Auth | `POST /api/Auth/signup`, `POST /api/Auth/login` |
| User | `GET /api/User`, `GET /api/User/by-username`, `GET /api/User/all`, `DELETE /api/User` |
| Project | `POST/GET/PUT/DELETE /api/Project`, `GET /api/Project/all`, `POST/DELETE /api/Project/participants` |
| Ticket | `POST/GET/PUT/DELETE /api/Ticket`, `GET /api/Ticket/all`, `GET /api/Ticket/by-project`, `PUT/DELETE /api/Ticket/assign`, `POST/DELETE /api/Ticket/tags` |
| Comment | `POST /api/Comment`, `GET /api/Comment/by-ticket`, `DELETE /api/Comment` |

## Project Structure

```
API/
  API/                    - main project
    Entities/             - EF entities, DTOs, request/response records, exceptions
    Data/                 - ApplicationDbContext
    Repositories/         - data access layer (interfaces + implementations)
    Service/              - business logic layer (interfaces + implementations)
    Controllers/          - thin HTTP layer
    Services/             - infrastructure services (JWT token generation)
  API.Tests/
    Repositories/         - repository tests (EF Core InMemory)
    Services/             - service tests (mocked repositories)
    Controllers/          - controller tests (mocked services)
    Integration/           - full-stack tests via WebApplicationFactory
docker-compose.yml
test-integration.sh
```