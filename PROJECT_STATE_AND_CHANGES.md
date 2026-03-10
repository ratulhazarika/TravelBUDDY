# EmployeeWeb.Desktop – Project State & Changes (Do Not Modify Structure)

**Last saved:** As requested. Do not duplicate file structure or redo these changes.

---

## Solution layout (three projects)

| Project | Role | Entry / API |
|--------|------|-------------|
| **EmployeeWeb.Api** | Backend API (.NET 8, minimal API, SQLite, JWT) | Runs at `http://localhost:5000/` |
| **EmployeeWeb.Desktop** | HR dashboard (WinUI 3, C#, XAML) | HR/admin login, dashboard, employees, etc. |
| **EmployeeDesktop** | Employee app (WinUI 3, C#, XAML) | Employee login/signup, dashboard, profile, “Travel Buddy” |

- **One repo/solution** – no separate duplicate repos or duplicate app structures.
- **One backend** – both HR dashboard and employee app use the same **EmployeeWeb.Api** at `http://localhost:5000/`.

---

## EmployeeDesktop (employee app) – what’s there

- **App flow:** Login/Signup → MainPage (shell with sidebar).
- **MainPage:** Left sidebar (Travel Buddy brand, Dashboard / Profile / Settings / Logout), top “Welcome, {name}” and “GO OFFLINE”, content area toggles between **Dashboard** and **Profile** (no separate duplicate pages for the same thing).
- **Dashboard:** Cards for Today’s Activity (first/last login, total worked, total break), Productivity %, Shift Overview (start/end, % done, time left), compact profile block, Today’s Activity Timeline. Data from `GET /api/user/{id}` and `POST /api/login/date/{id}`.
- **Profile:** Editable fields (name, email, phone, DOB, staff ID, role, DOJ), “Save Profile” → `PUT /api/user/{id}`, and “Change Password” section → `POST /api/auth/change-password`.
- **Auth:** `AuthService` + `ApiService` in **EmployeeDesktop/Services**. Token stored and sent via `ApiService.SetToken`. Login uses `POST /api/auth/login`; signup uses `POST /api/auth/signup`. Activity: `POST /api/activity/login` and `POST /api/activity/logout` with header `X-Employee-Id`.
- **Important fix applied:** All `Shadow="{ThemeResource ShadowElevation2}"` (and any other Shadow theme) were **removed** from **MainPage.xaml** because that theme key doesn’t exist in this WinUI 3 setup. Borders use only Background/CornerRadius/Padding. Do not re-add Shadow theme resources here.
- **Nav highlighting:** Done in code-behind with `ColorHelper.FromArgb` and `Colors.Transparent` (Microsoft.UI), not with extra XAML or duplicate shells.

---

## EmployeeWeb.Desktop (HR dashboard) – what’s there

- HR login (`/api/authenticate/admin`), Shell with NavigationView, Dashboard (company overview), stub pages (Employees, Departments, etc.). Uses same API base URL; different entry point and role from employee app.

---

## EmployeeWeb.Api – endpoints used

- **Auth:** `POST /api/auth/signup`, `POST /api/auth/login`, `POST /api/authenticate/admin`, `POST /api/auth/change-password` (body: employeeId, oldPassword, newPassword).
- **User:** `GET /api/user`, `GET /api/user/{id}`, `POST /api/user`, `PUT /api/user/{id}` (profile update), `DELETE /api/user/{id}`.
- **Activity:** `POST /api/activity/login`, `POST /api/activity/logout` (header `X-Employee-Id`).
- **Other:** `GET /api/dp/{id}`, `POST /api/login/date/{id}` (body `{ date }`), tickets/screenshot stubs.

---

## File locations (no duplicates)

- **Employee app UI:** `EmployeeDesktop/Views/LoginPage.xaml(.cs)`, `SignupPage.xaml(.cs)`, `MainPage.xaml(.cs)` (single main shell; Dashboard and Profile are views on MainPage, not separate duplicate pages).
- **Employee app services:** `EmployeeDesktop/Services/ApiConfiguration.cs` (BaseUrl `http://localhost:5000/`), `ApiService.cs`, `AuthService.cs`.
- **Employee app models:** `EmployeeDesktop/Models/UserInfo.cs`, `EmployeeDashboardModels.cs` (e.g. EmployeeProfile, EmployeeDailySummary, LoginLogEntry).
- **API:** `EmployeeWeb.Api/Program.cs` (all endpoints), `EmployeeWeb.Api/Models/`, `EmployeeWeb.Api/Data/AppDbContext.cs`, etc.
- **HR dashboard:** `EmployeeWeb.Desktop/` (separate project; do not merge or duplicate with EmployeeDesktop).

---

## What to avoid when continuing later

- Do **not** add a second employee app or duplicate shell (e.g. another MainPage or another “Employee” project with the same flow).
- Do **not** re-add `Shadow="{ThemeResource ShadowElevation2}"` (or similar) to EmployeeDesktop XAML.
- Do **not** create duplicate API projects or duplicate base URLs for the same backend.
- Do **not** duplicate AuthService/ApiService under a different folder or namespace for the same app.

---

## Run instructions (unchanged)

1. Start API: run **EmployeeWeb.Api** (e.g. `dotnet run` in that folder).
2. HR dashboard: run **EmployeeWeb.Desktop** (startup project = app project, not Package).
3. Employee app: run **EmployeeDesktop** (after closing any previous instance, rebuild then run so the Shadow-less XAML is used).

This file is the single source of truth for “what was built and what was changed.” Use it when returning to the project so no duplicate structure or re-applied changes are introduced.
