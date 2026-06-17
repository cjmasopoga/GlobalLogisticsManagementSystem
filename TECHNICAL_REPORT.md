# Technical Report
## Global Logistics Management System (GLMS)
### Service-Oriented Architecture, Cloud-Native Containerisation & Automated Testing

---

| Field | Detail |
|---|---|
| **Student** | Jan Masopoga |
| **Repository** | https://github.com/cjmasopoga/GlobalLogisticsManagementSystem |
| **Platform** | .NET 10 · ASP.NET Core · Entity Framework Core 10 |
| **Environment** | Visual Studio Community 2026 · Docker Desktop · SQL Server 2022 |
| **Branch** | `main` |

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture Design](#2-architecture-design)
3. [Backend API — GLMS.API](#3-backend-api--glmsapi)
4. [Frontend Web App — GLMS.Web](#4-frontend-web-app--glmsweb)
5. [Authentication & Security](#5-authentication--security)
6. [Containerisation with Docker](#6-containerisation-with-docker)
7. [Automated Testing](#7-automated-testing)
8. [DevOps & CI/CD Reflection](#8-devops--cicd-reflection)
9. [Challenges & Solutions](#9-challenges--solutions)
10. [Conclusion](#10-conclusion)

---

## 1. Project Overview

The Global Logistics Management System (GLMS) is a web-based platform built for **TechMove Logistics** to manage clients, shipping contracts, and service requests across international operations. The system exposes live USD-to-ZAR currency conversion for service request costing using the Open Exchange Rates public API.

The project was delivered in two phases:

| Phase | Description |
|---|---|
| **Part 1 – Prototype** | Monolithic ASP.NET Core MVC application with direct Entity Framework database access and unit tests. |
| **Part 2 – SOA Refactor** | Decoupled into a Service-Oriented Architecture: a Web API backend, an MVC frontend client, Docker containerisation, and automated API integration tests. |

This report covers Part 2 in full detail.

---

## 2. Architecture Design

### 2.1 Architectural Style

The system was refactored from a **monolith** into a **Service-Oriented Architecture (SOA)** following a strict client-server separation:

```
┌─────────────────────────────────────────────────────────┐
│                    Docker Network                        │
│                                                         │
│  ┌──────────────────┐     HTTP/JSON    ┌─────────────┐  │
│  │  glms-frontend-  │ ──────────────► │ glms-backend│  │
│  │  web             │   (port 8080)    │ -api        │  │
│  │  ASP.NET MVC     │                 │ ASP.NET API  │  │
│  │  :5000 (host)    │                 │ :5001 (host) │  │
│  └──────────────────┘                 └──────┬──────┘  │
│                                              │          │
│                                         EF Core         │
│                                              │          │
│                                       ┌──────▼──────┐  │
│                                       │  sql-server │  │
│                                       │  -db        │  │
│                                       │  MSSQL 2022 │  │
│                                       │  :1433       │  │
│                                       └─────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Solution Structure

```
GlobalLogisticsManagementSystem/
├── Global Logistics Management System/   ← MVC Frontend (thin client)
│   ├── Controllers/                      ← Presentation controllers
│   ├── Views/                            ← Razor views (DTO-bound)
│   ├── Services/ApiService.cs            ← HttpClient wrapper for API
│   ├── Models/Api/ApiDtos.cs             ← MVC-side DTO definitions
│   ├── Filters/SessionAuthFilter.cs      ← JWT session guard
│   ├── appsettings.json                  ← Local config (localhost:5001)
│   └── appsettings.Docker.json           ← Container config (glms-backend-api)
│
├── GLMS.API/                             ← Web API Backend
│   ├── Controllers/                      ← Auth, Clients, Contracts, ServiceRequests
│   ├── Data/ApiDbContext.cs              ← EF Core DbContext
│   ├── DTOs/Dtos.cs                      ← API request/response DTOs
│   ├── Models/                           ← EF entity models
│   ├── Migrations/                       ← EF Core database migrations
│   ├── Services/                         ← TokenService, CurrencyService
│   ├── appsettings.json                  ← Local config
│   └── appsettings.Docker.json           ← Container config (sql-server-db)
│
├── GLMS.Tests/                           ← Test project
│   ├── UnitTest1.cs                      ← 28 unit tests
│   └── IntegrationTests.cs               ← 16 API integration tests
│
├── Dockerfile.api                        ← Multi-stage image for API
├── Dockerfile.web                        ← Multi-stage image for MVC
├── docker-compose.yml                    ← Three-container orchestration
└── .dockerignore                         ← Build context filter
```

### 2.3 Design Principles Applied

- **Separation of Concerns** — The API owns all business logic and database access; the MVC app is a pure presentation layer.
- **DTO Pattern** — Data is transferred between layers using immutable C# records, preventing EF entity leakage into the frontend.
- **Single Responsibility** — Each controller handles one domain (Clients, Contracts, ServiceRequests, Auth).
- **Dependency Injection** — All services (`ApiDbContext`, `TokenService`, `CurrencyService`, `ApiService`) are registered with ASP.NET Core's DI container.

---

## 3. Backend API — GLMS.API

### 3.1 Technology Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 with SQL Server provider |
| Auth | JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer 10.0.8`) |
| API Documentation | Swagger / OpenAPI (`Swashbuckle.AspNetCore 7.3.1`) |
| Currency | Open Exchange Rates REST API (`https://open.er-api.com/v6/latest/USD`) |

### 3.2 API Endpoints

| Method | Route | Description | Auth |
|---|---|---|---|
| `POST` | `/api/auth/login` | Returns a JWT token | None |
| `GET` | `/api/clients` | List all clients | ✅ Bearer |
| `GET` | `/api/clients/{id}` | Get client by ID | ✅ Bearer |
| `POST` | `/api/clients` | Create a client | ✅ Bearer |
| `PUT` | `/api/clients/{id}` | Update a client | ✅ Bearer |
| `DELETE` | `/api/clients/{id}` | Delete a client | ✅ Bearer |
| `GET` | `/api/contracts` | List contracts (filterable) | ✅ Bearer |
| `GET` | `/api/contracts/{id}` | Get contract by ID | ✅ Bearer |
| `POST` | `/api/contracts` | Create a contract | ✅ Bearer |
| `PUT` | `/api/contracts/{id}` | Update a contract | ✅ Bearer |
| `PATCH` | `/api/contracts/{id}/status` | Update contract status | ✅ Bearer |
| `DELETE` | `/api/contracts/{id}` | Delete a contract | ✅ Bearer |
| `GET` | `/api/servicerequests` | List service requests | ✅ Bearer |
| `GET` | `/api/servicerequests/{id}` | Get service request by ID | ✅ Bearer |
| `POST` | `/api/servicerequests` | Create service request | ✅ Bearer |
| `PATCH` | `/api/servicerequests/{id}/status` | Update SR status | ✅ Bearer |
| `DELETE` | `/api/servicerequests/{id}` | Delete service request | ✅ Bearer |
| `GET` | `/api/servicerequests/exchange-rate` | Live USD/ZAR rate | ✅ Bearer |

### 3.3 Data Model

```
Client
├── Id (PK)
├── Name
├── ContactDetails
├── Region
└── Contracts → [Contract]

Contract
├── Id (PK)
├── ClientId (FK → Client)
├── StartDate / EndDate
├── Status  (Draft | Active | OnHold | Expired)
├── ServiceLevel
├── SignedAgreementPath
└── ServiceRequests → [ServiceRequest]

ServiceRequest
├── Id (PK)
├── ContractId (FK → Contract)
├── Description
├── CostUsd / CostZar / ExchangeRateUsed
├── Status  (Pending | InProgress | Completed | Cancelled)
└── CreatedAt
```

### 3.4 Business Rules

- Service requests can only be created for contracts with status `Draft` or `Active`. Attempts against `OnHold` or `Expired` contracts return `400 Bad Request`.
- Contract status transitions are validated against the `ContractStatus` enum.
- `CostZar` is calculated at the time of service request creation using the live USD/ZAR exchange rate fetched from the external API, with a fallback of `18.50` if the external call fails.

### 3.5 Database Migrations

Entity Framework Core Code-First migrations are used. On startup in non-Testing environments, the API applies pending migrations automatically:

```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
	db.Database.Migrate();
}
```

---

## 4. Frontend Web App — GLMS.Web

### 4.1 Refactoring from Monolith to Thin Client

The original MVC application accessed the database directly through EF Core. In the SOA refactor, **all database access was removed** from the MVC project. The controllers now call the API exclusively through the `ApiService` typed `HttpClient`.

**Before (monolith):**
```csharp
// Controller directly queried EF Core
var clients = await _context.Clients.ToListAsync();
```

**After (thin client):**
```csharp
// Controller calls the API via ApiService
var clients = await _api.GetClientsAsync();
```

### 4.2 ApiService

`Services/ApiService.cs` is a typed `HttpClient` wrapper that:

1. Attaches the JWT token from the session to every request via `Authorization: Bearer <token>`.
2. Serialises/deserialises JSON with `System.Text.Json` (case-insensitive, enum-as-string).
3. Provides strongly-typed async methods for every API endpoint.

```csharp
private void AttachToken()
{
	var token = _ctx.HttpContext?.Session.GetString("JwtToken");
	if (!string.IsNullOrEmpty(token))
		_http.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", token);
}
```

### 4.3 Session-Based Authentication

The MVC app manages authentication via a server-side session:

- `AccountController.Login` → POSTs credentials to `POST /api/auth/login` → stores the returned JWT in `HttpContext.Session`.
- `SessionAuthFilter` (global `IActionFilter`) → on every request, checks for `JwtToken` in session. If absent, redirects to `/Account/Login?returnUrl=...`.
- `AccountController.Logout` → calls `HttpContext.Session.Clear()` and redirects to login.

### 4.4 Environment-Aware Configuration

ASP.NET Core automatically loads `appsettings.{ASPNETCORE_ENVIRONMENT}.json`. This is used to switch the API base URL:

| Environment | File | `ApiSettings:BaseUrl` |
|---|---|---|
| `Development` (F5) | `appsettings.json` | `http://localhost:5001/` |
| `Docker` (container) | `appsettings.Docker.json` | `http://glms-backend-api:8080/` |

---

## 5. Authentication & Security

### 5.1 JWT Token Flow

```
Browser          MVC App                   API
  │                │                        │
  │  POST /Login   │                        │
  │───────────────►│                        │
  │                │  POST /api/auth/login  │
  │                │───────────────────────►│
  │                │    { token, username } │
  │                │◄───────────────────────│
  │                │  Session["JwtToken"]   │
  │                │  = token               │
  │  Redirect /    │                        │
  │◄───────────────│                        │
  │                │                        │
  │  GET /clients  │                        │
  │───────────────►│                        │
  │                │  GET /api/clients      │
  │                │  Authorization: Bearer │
  │                │───────────────────────►│
  │                │  200 OK + JSON         │
  │                │◄───────────────────────│
  │  HTML page     │                        │
  │◄───────────────│                        │
```

### 5.2 Token Configuration

| Setting | Value |
|---|---|
| Algorithm | HMAC-SHA256 |
| Expiry | 8 hours |
| Issuer | `GLMS.API` |
| Audience | `GLMS.Web` |
| Key | 256-bit symmetric key (stored in `appsettings.json`) |

### 5.3 API Security

All API controllers carry `[Authorize]` except `AuthController`. Swagger is configured with a Bearer security scheme, allowing manual token testing via the Swagger UI at `http://localhost:5001/swagger`.

---

## 6. Containerisation with Docker

### 6.1 Docker Compose Services

```yaml
services:
  sql-server-db          # Container 1 — Microsoft SQL Server 2022
  glms-backend-api       # Container 2 — ASP.NET Core Web API
  glms-frontend-web      # Container 3 — ASP.NET Core MVC Frontend
```

### 6.2 Service Details

| Service | Image / Build | Port (host→container) | Depends On |
|---|---|---|---|
| `sql-server-db` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433:1433` | — |
| `glms-backend-api` | `Dockerfile.api` | `5001:8080` | `sql-server-db` (healthy) |
| `glms-frontend-web` | `Dockerfile.web` | `5000:8080` | `glms-backend-api` |

### 6.3 Multi-Stage Dockerfiles

Both Dockerfiles use a **two-stage build** to minimise the runtime image size:

```
Stage 1: mcr.microsoft.com/dotnet/sdk:10.0      ← build & publish
Stage 2: mcr.microsoft.com/dotnet/aspnet:10.0   ← runtime only
```

This results in images that contain only the published application binaries — the full SDK (~800 MB) is discarded.

### 6.4 Internal Networking

Docker Compose automatically creates a bridge network. Containers resolve each other by **service name** as DNS hostnames:

- API → SQL Server: `Server=sql-server-db,1433`
- Web → API: `http://glms-backend-api:8080`

No hardcoded IP addresses are used anywhere.

### 6.5 Startup Ordering & Health Checks

```yaml
sql-server-db:
  healthcheck:
	test: sqlcmd -S localhost -U sa -P '...' -Q 'SELECT 1' -No
	interval: 10s
	retries: 10

glms-backend-api:
  depends_on:
	sql-server-db:
	  condition: service_healthy   # waits for SQL Server to accept connections

glms-frontend-web:
  depends_on:
	- glms-backend-api             # waits for API container to start
```

### 6.6 Running the Stack

```powershell
# First run (or after code changes)
docker compose up --build

# After deleting stale cached images
docker rmi $(docker images --filter "reference=*glms*" -q)
docker compose up --build
```

| URL | Description |
|---|---|
| `http://localhost:5000` | MVC Frontend (login required) |
| `http://localhost:5001/swagger` | API Swagger UI |

---

## 7. Automated Testing

### 7.1 Test Summary

| Category | Count | Framework |
|---|---|---|
| Unit Tests | 28 | xUnit |
| Integration Tests | 16 | xUnit + `Microsoft.AspNetCore.Mvc.Testing` |
| **Total** | **44** | **44/44 passing ✅** |

### 7.2 Unit Tests (`UnitTest1.cs`)

Unit tests are fully self-contained — no external dependencies, no database, no HTTP calls. They test the core business logic using local test doubles.

| Test Class | Coverage |
|---|---|
| `CurrencyCalculationTests` | USD→ZAR conversion accuracy, rounding, edge cases (zero, negative, fractional) |
| `FileValidationTests` | PDF validation: null file, empty file, wrong extension, uppercase `.PDF` |
| `ContractWorkflowTests` | Contract status rules, service request eligibility by status, default status values |
| `ModelTests` | Entity default values, collection initialisation, date UTC compliance |

### 7.3 Integration Tests (`IntegrationTests.cs`)

Integration tests boot the **real API** in-process using `WebApplicationFactory<Program>` and replace SQL Server with an EF Core InMemory database.

**Key design decisions:**

1. **`InMemoryDatabaseRoot` shared across all requests** — A single static `InMemoryDatabaseRoot` is passed to `UseInMemoryDatabase`, ensuring all HTTP requests within a test run read and write to the same data store. Without this, each `DbContext` scope created a new empty database.

2. **Service descriptor removal** — All EF Core service descriptors that reference `ApiDbContext` are removed before registering the InMemory replacement, preventing the `SqlServer + InMemory providers both registered` runtime error.

3. **`EnableServiceProviderCaching(false)`** — Prevents EF from reusing a cached internal service provider that retains SQL Server internals.

4. **Testing environment guard** — `ASPNETCORE_ENVIRONMENT=Testing` in the factory causes the API to skip `db.Database.Migrate()` on startup, which would fail against an InMemory store.

```csharp
public class GlmsApiFactory : WebApplicationFactory<Program>
{
	private static readonly InMemoryDatabaseRoot _dbRoot = new();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureServices(services =>
		{
			// Remove all ApiDbContext-related service registrations
			var toRemove = services
				.Where(d =>
					d.ServiceType == typeof(ApiDbContext) ||
					d.ServiceType == typeof(DbContextOptions<ApiDbContext>) ||
					(d.ServiceType.IsGenericType &&
					 d.ServiceType.GetGenericArguments().Any(t => t == typeof(ApiDbContext))))
				.ToList();
			foreach (var d in toRemove) services.Remove(d);

			// Register InMemory with shared root
			services.AddDbContext<ApiDbContext>(options =>
			{
				options.UseInMemoryDatabase("GlmsTestDb", _dbRoot);
				options.EnableServiceProviderCaching(false);
			});
		});
	}
}
```

| Integration Test Class | Scenarios Covered |
|---|---|
| `AuthIntegrationTests` | Valid login returns 200 + token; wrong password returns 401; unknown user returns 401 |
| `ClientsIntegrationTests` | List clients; get by ID (found/not found); create returns 201; delete returns 204 |
| `ContractsIntegrationTests` | List contracts; get by ID (found/not found); create returns 201; PATCH status returns 204 |
| `ServiceRequestsIntegrationTests` | List SRs; get by ID (not found); create on active contract returns 201; create on non-existent contract returns 400/404 |

---

## 8. DevOps & CI/CD Reflection

### 8.1 Why Automated Testing is Critical in a CI/CD Pipeline

A CI/CD (Continuous Integration / Continuous Deployment) pipeline automates the process of building, testing, and deploying software every time code is pushed. Automated tests are the safety net that makes this process reliable.

**Without automated tests**, a CI/CD pipeline would:
- Deploy broken code to production automatically.
- Have no way to distinguish a safe change from a regression.
- Require manual testing before every deployment, defeating the purpose of automation.

**With automated tests**, the pipeline can:

| Stage | What Tests Provide |
|---|---|
| **Build** | Compilation errors are caught immediately on every push. |
| **Unit Test** | Business logic regressions (e.g. wrong currency calculation) are caught within seconds, before any deployment. |
| **Integration Test** | API contract violations (wrong status codes, missing fields, broken auth) are caught before the image reaches staging. |
| **Deploy** | Only code that has passed all test gates reaches production. |

**In this project specifically:**

- The `CurrencyCalculationTests` would catch a developer accidentally changing the rounding logic.
- The `AuthIntegrationTests` would catch a misconfigured JWT key breaking login.
- The `ContractsIntegrationTests` would catch a breaking change to the contract creation endpoint before the MVC frontend is affected.

### 8.2 CI/CD Pipeline Design (GitHub Actions)

A production CI/CD pipeline for GLMS would follow these stages:

```
Push to main
	 │
	 ▼
┌─────────────────┐
│  1. Build       │  dotnet build --configuration Release
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  2. Unit Tests  │  dotnet test --filter "Category=Unit"
└────────┬────────┘
		 │
		 ▼
┌─────────────────────┐
│  3. Integration     │  dotnet test --filter "Category=Integration"
│     Tests           │  (API tested in-process with InMemory DB)
└────────┬────────────┘
		 │
		 ▼
┌─────────────────┐
│  4. Docker Build│  docker compose build
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  5. Push Images │  docker push to registry (ACR / Docker Hub)
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  6. Deploy      │  docker compose up (staging → production)
└─────────────────┘
```

Each stage acts as a gate — if any stage fails, the pipeline stops and no code is deployed.

---

## 9. Challenges & Solutions

### Challenge 1 — SDK-Style Project Glob Pollution

**Problem:** When the solution placed `GLMS.API`, `GLMS.Tests`, and `GLMS.Shared` as subfolders inside the MVC project directory, the MVC `.csproj` automatically compiled all C# files in those subfolders (SDK-style globbing), causing thousands of duplicate symbol errors.

**Solution:** Added explicit `<Compile Remove="...">`, `<Content Remove="...">`, `<EmbeddedResource Remove="...">` entries for each nested project folder in the MVC `.csproj`.

---

### Challenge 2 — EF Core Provider Conflict in Integration Tests

**Problem:** `WebApplicationFactory` booted the API with its SQL Server EF registration intact. Adding InMemory alongside it caused: *"Services for database providers 'SqlServer', 'InMemory' have been registered. Only a single provider can be registered."*

**Solution:**
1. Remove all service descriptors whose `ServiceType` involves `ApiDbContext` before registering InMemory.
2. Call `EnableServiceProviderCaching(false)` to prevent EF's internal provider cache from reusing SQL Server internals.

---

### Challenge 3 — InMemory Database Isolation Between Requests

**Problem:** Using `Guid.NewGuid()` inside the `UseInMemoryDatabase` options lambda caused every `DbContext` construction (i.e. every HTTP request) to get a brand-new empty database, making cross-request test data invisible.

**Solution:** Declared a `static readonly InMemoryDatabaseRoot _dbRoot` on the factory and passed it as the second argument to `UseInMemoryDatabase("GlmsTestDb", _dbRoot)`. All scopes within one factory lifetime now share the same store.

---

### Challenge 4 — Docker Image Caching Stale Files

**Problem:** After fixing a malformed `appsettings.Docker.json` (missing comma), running `docker compose up --build` still crashed because `--build` reuses existing image layers. The broken file was baked into a cached layer.

**Solution:** Deleted the stale images with `docker rmi` before rebuilding, forcing Docker to copy the corrected file into a fresh image layer.

---

### Challenge 5 — `appsettings.Docker.json` Loaded in Local Debug

**Problem:** Manually calling `AddJsonFile("appsettings.Docker.json")` unconditionally caused the local F5 debug session to try connecting to `glms-backend-api:8080` — a hostname that only exists inside Docker networking — resulting in `HttpRequestException`.

**Solution:** Removed the manual `AddJsonFile` call entirely. ASP.NET Core automatically loads `appsettings.{ASPNETCORE_ENVIRONMENT}.json`, so setting `ASPNETCORE_ENVIRONMENT=Docker` in the container is sufficient.

---

### Challenge 6 — No Login Page (401 on Every Request)

**Problem:** The MVC frontend had no `AccountController`, no login view, and no session guard. Every API call was made without a token, receiving `401 Unauthorized`.

**Solution:**
- Created `AccountController` with `GET/POST Login` and `POST Logout`.
- Created `Views/Account/Login.cshtml` as a standalone Bootstrap form.
- Created `SessionAuthFilter` as a global `IActionFilter` that redirects to login when no `JwtToken` is found in session.
- Registered the filter globally via `options.Filters.Add<SessionAuthFilter>()`.

---

## 10. Conclusion

The GLMS project successfully demonstrates a complete transition from a monolithic prototype to a production-ready, cloud-native service-oriented architecture. The key achievements are:

| Deliverable | Status |
|---|---|
| Web API backend (`GLMS.API`) with JWT auth, Swagger, EF Core | ✅ Complete |
| MVC frontend refactored to thin HTTP client | ✅ Complete |
| JWT session authentication and login/logout flow | ✅ Complete |
| Multi-stage Dockerfiles for API and Web | ✅ Complete |
| Docker Compose orchestration (3 containers, internal networking) | ✅ Complete |
| Environment-specific configuration (local vs Docker) | ✅ Complete |
| Automated test suite: 44 tests, 44 passing | ✅ Complete |
| All source code, Dockerfiles, and Compose file on GitHub | ✅ Complete |

The architecture cleanly separates concerns, the API is independently testable and documented via Swagger, the full stack can be spun up with a single `docker compose up --build` command, and every code push is backed by a passing test suite.

---

*Report generated from source code at commit `main` — https://github.com/cjmasopoga/GlobalLogisticsManagementSystem*
