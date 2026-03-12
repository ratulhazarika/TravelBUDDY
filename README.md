# EmployeeWeb Desktop

This repo contains:
- Employee desktop app (WinUI 3)
- Admin/HR desktop app (WinUI 3)
- API service (ASP.NET Core) used by both apps

## Quick Start (Dashboards)

### 1. Prerequisites
- Visual Studio 2022 with:
  - .NET desktop development
  - Universal Windows Platform development
  - Windows App SDK
- .NET 8 SDK

### 2. Clone
```bash
git clone https://github.com/ratulhazarika/TravelBUDDY
cd TravelBUDDY
```

### 3. Run the API
```powershell
dotnet run --project "EmployeeWeb.Api/EmployeeWeb.Api.csproj"
```
API starts at:
- http://localhost:5000
- https://localhost:7007

### 4. Run the Admin/HR Dashboard
```powershell
dotnet run --project "EmployeeWeb.Desktop/EmployeeWeb.Desktop.csproj"
```

### 5. Run the Employee Dashboard (optional)
```powershell
dotnet run --project "EmployeeDesktop/EmployeeDesktop.csproj"
```

### 6. First login
- Sign up using the Employee app (or call /api/auth/signup from Swagger).
- Then log in to the Admin app using the same email/password.
- If the role is "Hr and Administration", HR-only menu items show up.

## API Base URL
If the API runs on a different host/port, update the base URL in:
- EmployeeWeb.Desktop/Services/ApiConfiguration.cs
- EmployeeDesktop/Services/ApiConfiguration.cs

