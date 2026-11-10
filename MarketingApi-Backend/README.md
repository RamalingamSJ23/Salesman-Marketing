# Salesman & Marketing Team Management API (.NET 8 Web API)

RESTful Web API built with **.NET 8**, **Entity Framework Core (Npgsql)**, **PostgreSQL**, **FluentValidation**, and **Serilog**.

## Architecture

```
MarketingApi/
├── Common/            # ServiceResult, ProblemDetails mapping, AppRoles, GlobalExceptionHandler, CurrentUserService
├── Controllers/       # SalesmanMasterController, MarketingTeamMasterController, UsersController
├── Data/              # SalesMarketingDbContext, AuditSaveChangesInterceptor, Migrations/
├── Database/          # seed.sql for initial roles and users
├── DTOs/              # Request and response contracts
├── Enums/             # RecordStatus, SalesmanRole, DeactivationRequestStatus, TeamRoleType
├── Models/Entities/   # EF Core entities + AuditLog
├── Options/           # Strongly-typed JwtOptions
├── Repository/        # Database access and atomic transactions
├── Service/           # Business rules (R1–R10), logging, and validation
└── Validation/        # FluentValidation DTO validators
```

## Setup & Running

1. **Configure Secrets** (`dotnet user-secrets` for development, environment variables for production):
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=marketingdb;Username=postgres;Password=YOUR_DB_PASSWORD" --project MarketingApi
   dotnet user-secrets set "Jwt:Key" "YOUR_STRONG_AT_LEAST_32_CHAR_SECRET_KEY" --project MarketingApi
   ```

2. **Apply EF Core Migrations & Seed Data**:
   ```powershell
   dotnet ef database update --project MarketingApi
   psql -d marketingdb -f MarketingApi/Database/seed.sql
   dotnet run --project MarketingApi -- --hash-passwords
   ```

3. **Run the API & Tests**:
   ```powershell
   dotnet run --project MarketingApi
   dotnet test
   ```

Swagger UI is enabled in `Development` mode at `/swagger`.
