# Salesman & Marketing Team Management API (.NET 8 Web API)

A clean, beginner-friendly RESTful Web API built with **.NET 8**, **Entity Framework Core (Npgsql)**, and **PostgreSQL** according to the technical specification.

---

## 🏗️ Architecture & Project Structure (Fresher-Friendly Guide)

The application follows the **Repository & Service Pattern** with clear separation of concerns:

```
MarketingApi/
│
├── Controllers/               # Handles HTTP requests, calls Services, returns HTTP status codes
│   ├── SalesmanMasterController.cs        # Endpoints for Salesman CRUD and lookups
│   ├── MarketingTeamMasterController.cs   # Endpoints for Team CRUD with row merge
│   └── UsersController.cs                 # Endpoints for system Roles and Users
│
├── Service/                   # Business Logic & Validation Layer (Rules R1 to R11)
│   ├── ISalesmanService.cs / SalesmanService.cs
│   ├── IMarketingTeamService.cs / MarketingTeamService.cs
│   └── IUsersService.cs / UsersService.cs
│
├── Repository/                # Database Access Layer (EF Core queries, transactions)
│   ├── ISalesmanRepository.cs / SalesmanRepository.cs
│   ├── IMarketingTeamRepository.cs / MarketingTeamRepository.cs
│   └── IUsersRepository.cs / UsersRepository.cs
│
├── DTOs/                      # Data Transfer Objects (Contracts sent to/from frontend)
│   ├── Common/                # PagedResultDto, SalesmanLookupDto, EnumOptionDto
│   ├── Salesman/              # SalesmanListDto, SalesmanDto, CreateSalesmanDto, UpdateSalesmanDto
│   └── MarketingTeam/         # MarketingTeamListDto, MarketingTeamDto, Create/Update DTOs
│
├── Enums/                     # Strongly-typed Enums matching the database smallint
│   ├── RecordStatus.cs        # 1 = Active, 2 = Inactive
│   └── SalesmanRole.cs        # 1 = TeamLeader, 2 = SeniorSalesExecutive, 3 = SalesExecutive, 4 = Trainee
│
├── Models/                    # Entity Framework Core Entity classes matching PostgreSQL tables
│   ├── Salesman.cs
│   ├── Marketingteam.cs
│   ├── Marketingteamdetail.cs
│   ├── Role.cs
│   └── User.cs
│
├── Data/                      # DbContext configuration
│   └── SalesMarketingDbContext.cs
│
└── Program.cs                 # Dependency Injection, Middleware, Swagger & CORS configuration
```

---

## 🚦 How a Request Flows (Trace from Request to Database)

```
[HTTP Request from Angular / Swagger]
               │
               ▼
   [Controller] (e.g. SalesmanMasterController)
       - Validates input format (ModelState)
       - Forwards DTO to the Service
               │
               ▼
     [Service] (e.g. SalesmanService)
       - Enforces business rules (R1 to R11)
       - Handles validation (dates, uniqueness, relationships)
       - Returns ServiceResult<T> with status code (200, 201, 400, 404, 409)
               │
               ▼
   [Repository] (e.g. SalesmanRepository)
       - Uses DbContext and EF Core to query / persist data
       - Manages transactions (BeginTransactionAsync / CommitAsync)
               │
               ▼
  [PostgreSQL Database] (marketingdb)
```

---

## 📋 Enforced Business Rules (Section 6)

| Rule | Description | Enforced In | API Response |
| :--- | :--- | :--- | :--- |
| **R1** | Salesman code is required and unique | UI, Service, DB | `409 Conflict`: "Salesman code {code} already exists." |
| **R2** | A salesman assigned to any team cannot be deleted | Service, DB Foreign Key | `400 Bad Request`: "Salesman is assigned to a team. Make them Inactive instead." |
| **R3** | Team code is required and unique | UI, Service, DB | `409 Conflict`: "Team code {code} already exists." |
| **R4** | Team must have at least one member row | UI, Service | `400 Bad Request`: "Add at least one salesman to the team." |
| **R5** | Same salesman cannot repeat in one team | UI, Service, DB Unique | `400 Bad Request`: "Salesman {name} is already in this team." |
| **R6** | Exactly one row has role Team Leader, matching header Team Leader | Service | `400 Bad Request`: "The Team Leader in the header must be the only member with role Team Leader." |
| **R7** | Only Active salesmen can be assigned to a team | UI, Service | `400 Bad Request`: "Salesman {name} is inactive." |
| **R8** | A salesman can be an active member of only one active team | Service | `400 Bad Request`: "{name} is already in active team {teamcode}." |
| **R9** | Target amount must be $\ge$ 0 | UI, Service, DB Check | `400 Bad Request`: "Target amount cannot be negative." |
| **R10**| Header and detail rows save in one transaction | Repository (EF Transaction) | Rollback on failure |
| **R11**| Deleting a team cascades to remove its detail rows | DB Cascade + Repository | Successful deletion leaves 0 orphan rows |

---

## 📡 Complete API Endpoints List

### 1. Salesman Master (`/api/salesman` or `/api/SalesmanMaster`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/SalesmanMaster` | Paginated list with search (`?search=john`), status filter (`?status=1`), `pageNumber`, `pageSize` |
| `GET` | `/api/SalesmanMaster/{id}` | Get single salesman details by ID |
| `GET` | `/api/SalesmanMaster/lookup` | Get list of **Active** salesmen for dropdowns (ID, Code, Name) |
| `POST` | `/api/SalesmanMaster` | Create new salesman (checks R1, date in past/today) |
| `PUT` | `/api/SalesmanMaster/{id}` | Update existing salesman details (Name, Mobile, Email, Date). **Status is excluded** |
| `PUT` | `/api/SalesmanMaster/{id}/deactivate` | Deactivates salesman by ID (sets `status = 2`, Inactive) |
| `PUT` | `/api/SalesmanMaster/{id}/activate` | Activates salesman by ID (sets `status = 1`, Active) |

### 2. Marketing Team Master (`/api/marketing-team` or `/api/MarketingTeamMaster`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/marketing-team` | Paginated team list with Team Leader name and Salesmen Count (Section 3.5 join query) |
| `GET` | `/api/marketing-team/{id}` | Get team with all member rows and total target amount |
| `GET` | `/api/marketing-team/role-options`| Get role enum options (Team Leader, Senior Sales Executive, Sales Executive, Trainee) |
| `POST` | `/api/marketing-team` | Create team header and members in one transaction (checks R3 to R10) |
| `PUT` | `/api/marketing-team/{id}` | Update team with row merge (inserts new, updates edited, deletes removed rows) |
| `DELETE` | `/api/marketing-team/{id}` | Delete team and cascade delete detail rows (R11) |

### 3. Users & Roles (`/api/users`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/users/roles` | Returns list of roles (`Superviser Master`, `Marketing Master`) |

---

## ⚙️ Setup & Running

1. **Database Connection**:
   Check [appsettings.json](file:///c:/Users/RamalingamSJ/Downloads/project/MarketingApi/MarketingApi/appsettings.json):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=marketingdb;Username=postgres;Password=401277"
   }
   ```

2. **Run the Project**:
   ```powershell
   dotnet run --project MarketingApi
   ```

3. **Open Swagger UI**:
   Navigate to [http://localhost:5153/](http://localhost:5153/) to view and test all endpoints interactively in your browser.
