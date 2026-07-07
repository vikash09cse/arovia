# Arovia — Uro Care HMS (SaaS Phase 1)

Multi-tenant Hospital Management System platform.

**Phase 1 scale:** ~10 tenants — single **WebApi** serves both Angular apps directly (no API Gateway).

## Repository layout

```
Arovia/
├── Arovia-Backend.sln          # .NET 10 backend solution
├── SuperAdmin/                 # Angular — Platform Admin (tenant & backoffice user management)
├── TenantLogin/                # Angular — Tenant portal (login + HMS operations)
├── src/
│   ├── databases/
│   │   └── HmsDB/              # SQL Server migrations (DbUp)
│   └── services/
│       ├── SharedKernel/       # Cross-cutting: auth, middleware, helpers, entities
│       └── WebApi/             # Module-based REST API (direct entry point)
│           └── Features/       # Vertical slices per domain module
└── Uro_Care_HMS_FRD_SaaS_Phase1.md
```

## Backend architecture

Module-based API pattern (reference: **NexSchedBackend** WebApi + SharedKernel):

| Project | Purpose |
|---------|---------|
| **WebApi** | Feature modules under `Features/` (Controller → Service → Repository) |
| **SharedKernel** | JWT, middleware, Dapper/EF helpers, shared entities & DTOs |
| **HmsDB** | SQL Server schema, seed, functions, and migration scripts |

**Database:** SQL Server, shared-database multi-tenant model with `tenant_id` on tenant-scoped tables.

> **Gateway:** Not included for Phase 1. At ~10 tenants, frontends call WebApi directly. A YARP gateway can be added later if routing, rate limiting, or multiple backend services are needed.

## WebApi modules (Phase 1)

| Module | Route prefix | Description |
|--------|--------------|-------------|
| `Auth` | `/api/auth` | Platform & tenant login |
| `PlatformAdmin` | `/api/platform` | Tenant onboarding, backoffice users |
| `Patients` | `/api/patients` | Patient registration & profiles |
| `Visits` | `/api/visits` | Visit tracking & fee logic |
| `Payments` | `/api/payments` | Payment collection |
| `LabTests` | `/api/lab-tests` | Lab test orders & results |
| `Reports` | `/api/reports` | Daily dashboards |

Each module folder:

```
Features/{Module}/
├── {Module}Controller.cs
├── {Module}Service.cs
├── DTOs.cs
└── Infrastructure/
    └── {Module}Repository.cs
```

## Frontend apps

| App | Port (dev) | API base URL (dev) | Users |
|-----|------------|--------------------|-------|
| **SuperAdmin** | `4200` | `https://localhost:7150/api` | Platform Admin |
| **TenantLogin** | `4201` | `https://localhost:7150/api` | Tenant Super Admin, Staff, Doctors |

## Default credentials (after running HmsDB migration)

| User | Email | Password |
|------|-------|----------|
| Platform Admin | `admin@arovia.com` | `Admin@123` |

## API endpoints (implemented)

### Auth
- `POST /api/auth/platform-login`
- `POST /api/auth/tenant-login`
- `GET /api/auth/tenant-by-subdomain/{subdomain}`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

### Platform Admin (JWT: PlatformAdmin)
- `GET /api/platform/tenants`
- `GET /api/platform/tenants/dashboard`
- `POST /api/platform/tenants`
- `PUT /api/platform/tenants/{id}`
- `PATCH /api/platform/tenants/{id}/suspend`
- `PATCH /api/platform/tenants/{id}/reactivate`
- `GET /api/platform/backoffice-users`
- `POST /api/platform/backoffice-users`

### Tenant Users (JWT: TenantSuperAdmin)
- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}`
- `PATCH /api/users/{id}/status`

## Getting started

### Backend

```bash
cd Arovia
dotnet restore Arovia-Backend.sln
dotnet build Arovia-Backend.sln
dotnet run --project src/services/WebApi
dotnet run --project src/databases/HmsDB    # after SQL Server is configured
```

### SuperAdmin

```bash
cd SuperAdmin
npm install
ng serve --port 4200
```

### TenantLogin

```bash
cd TenantLogin
npm install
ng serve --port 4201
```

## Reference

- Functional requirements: `Uro_Care_HMS_FRD_SaaS_Phase1.md`
- Backend pattern reference: `../NexSchedBackend`
