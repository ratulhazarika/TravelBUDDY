# Explaining Your Work to HR / Senior + HLD & LLD Q&A

Use this when you present what you’ve built and when they ask about High Level Design (HLD) or Low Level Design (LLD).

---

## Part 1: What You’ve Done (Short Explanation)

### For HR / non-technical

**One sentence:**  
“I’ve built an **employee monitoring system** with a **backend API**, an **HR desktop app** for admins to see all employees and activity, and an **employee desktop app** (Travel Buddy) where staff log in daily, see their dashboard (login times, work/break time, shift progress), update their profile, and change password—all data is stored in one place and shown to HR.”

**Slightly longer:**  
- **Backend (API):** Central server that stores employee data, handles login/signup, records when employees start/end their day, and serves data to both apps.  
- **HR app:** Desktop app for HR/admin. They log in, see company overview (who’s active, on break, etc.) and can manage employees (the rest of the screens are in progress).  
- **Employee app:** Desktop app for employees. They sign up or log in, see their own dashboard (first/last login today, total work time, break time, productivity, shift progress), edit their profile, and change password. When they “Go offline” or logout, that is recorded so HR sees updated status.

---

## Part 2: How to Show It (Demo Script)

Do this in order so nothing breaks.

### Before the meeting

1. **Close** any already running EmployeeDesktop or EmployeeWeb.Desktop windows.  
2. **Open the solution** in Visual Studio: `EmployeeWeb.Desktop` folder (the one that has the `.sln` or `.slnx`).

### Step 1: Start the backend

- Set **EmployeeWeb.Api** as startup project (right‑click → Set as Startup Project).  
- Run (F5) or: `dotnet run` in the `EmployeeWeb.Api` folder.  
- Confirm it’s running (e.g. “Now listening on http://localhost:5000” or open `http://localhost:5000/swagger` in browser).

### Step 2: Show the employee app

- Set **EmployeeDesktop** as startup project.  
- Run (F5).  
- **Show:**  
  - Sign up (new user) or Log in.  
  - After login: Dashboard (Today’s Activity, Productivity %, Shift Overview, profile summary, timeline).  
  - Profile tab: edit name/email/phone/DOB/role/DOJ → Save Profile.  
  - Change Password section: old password, new, confirm → Update Password.  
  - “GO OFFLINE” or Logout (session end is recorded).

### Step 3: Show the HR dashboard

- Stop the employee app (so you can change startup project).  
- Set **EmployeeWeb.Desktop** (the HR app) as startup project.  
- Run (F5).  
- **Show:**  
  - HR login (same backend; use an account that exists in the DB, e.g. one you created via employee signup or that you added in the API/DB).  
  - Dashboard / company overview and navigation (Employees, Departments, etc.).  
- Say: “Same backend and same database—what the employee updates in their app is what HR sees here.”

### If they want “one flow” only

Run API first, then run **EmployeeDesktop**, and walk: Sign up → Login → Dashboard → Profile edit → Change password → Logout. That alone shows the full employee side and that data is in the system for HR.

---

## Part 3: HLD & LLD Questions They Might Ask

### HLD (High Level Design)

**Q: What is the high-level architecture?**  
- **Answer:** Three parts: (1) **Backend API** (EmployeeWeb.Api) – .NET 8, REST API, SQLite, JWT auth; (2) **HR desktop app** (EmployeeWeb.Desktop) – WinUI 3, for HR/admin to view and manage employees; (3) **Employee desktop app** (EmployeeDesktop) – WinUI 3, for employees to log in daily, see dashboard, update profile, change password. Both apps talk to the same API and same database. No duplicate backends.

**Q: How do the two apps differ?**  
- **Answer:** Same backend and same data. **HR app** uses admin login (`/api/authenticate/admin`), sees all employees and company overview. **Employee app** uses employee login (`/api/auth/login`) and signup (`/api/auth/signup`), sees only their own dashboard and profile, and records session start/end via `/api/activity/login` and `/api/activity/logout`.

**Q: How is security handled?**  
- **Answer:** Passwords are hashed (not stored plain). Login returns a JWT; the client sends it in the `Authorization` header for protected calls. API uses JWT validation (issuer, audience, signing key). CORS is configured on the API for the clients.

**Q: Where is data stored?**  
- **Answer:** Single SQLite database used by the API (EF Core). Tables include Employees and LoginLogs (for daily in/out). Both HR and employee app read/write through the API only, not directly to the DB.

**Q: How does HR see employee activity?**  
- **Answer:** Employee app calls `/api/activity/login` when the employee logs in and `/api/activity/logout` (or when they click “Go offline”). That updates the backend and login logs. HR app loads the same data via API (e.g. company overview, employee list, login logs) so they see who is online and attendance.

---

### LLD (Low Level Design)

**Q: How is the employee app structured (main screens)?**  
- **Answer:** One main shell: **MainPage**. It has a left sidebar (Travel Buddy brand, Dashboard / Profile / Settings / Logout) and a content area that switches between two views: **Dashboard** (activity cards, productivity, shift, timeline) and **Profile** (edit form + change password). Login and Signup are separate pages; after auth we navigate to MainPage.

**Q: How does the employee app talk to the API?**  
- **Answer:** A single **HttpClient** in **ApiService** with base URL from **ApiConfiguration** (e.g. `http://localhost:5000/`). Methods like `LoginAsync`, `SignupAsync`, `GetEmployeeProfileAsync`, `GetLoginLogAsync`, `UpdateEmployeeProfileAsync`, `ChangePasswordAsync`, `RecordActivityLoginAsync`, `RecordActivityLogoutAsync` call the corresponding REST endpoints. After login we call `ApiService.SetToken(token)` so subsequent requests send the JWT.

**Q: How is login state kept in the employee app?**  
- **Answer:** **AuthService** holds current user and token in memory and persists them to **Windows ApplicationData (LocalSettings)**. On app start we call `LoadSavedSession()`; if a token exists we consider the user logged in and navigate to MainPage (and optionally call activity/login). On logout we call `ClearUser()` and remove token from memory and settings.

**Q: What key API endpoints did you implement?**  
- **Answer:** Auth: `POST /api/auth/signup`, `POST /api/auth/login`, `POST /api/authenticate/admin`, `POST /api/auth/change-password`. User: `GET /api/user`, `GET /api/user/{id}`, `POST /api/user`, `PUT /api/user/{id}`, `DELETE /api/user/{id}`. Activity: `POST /api/activity/login`, `POST /api/activity/logout`. Plus `GET /api/dp/{id}`, `POST /api/login/date/{id}` for profile picture and daily login/logout sessions. All return a consistent JSON shape (e.g. Status, Data, Message).

**Q: How is the dashboard data (work time, break, productivity) computed?**  
- **Answer:** We fetch today’s sessions from `POST /api/login/date/{id}` (list of login/logout pairs). In the client we compute: first login time, last logout (or “Online”), total focus time (sum of login→logout spans), total break time (between logout and next login). Productivity % = focus / (focus + break). Shift % and time left are derived from a configured shift start and length (e.g. 8.5 hours).

**Q: Why is there no Shadow on the cards in the employee app?**  
- **Answer:** WinUI 3 in this project doesn’t define the theme resource `ShadowElevation2`. Using it caused a runtime error, so we removed the Shadow attribute from the card borders and kept only Background, CornerRadius, and Padding so the app runs reliably.

---

## Quick Reference: Three Projects

| What to say        | Project             | Tech / Purpose                                      |
|--------------------|---------------------|-----------------------------------------------------|
| Backend / API      | EmployeeWeb.Api     | .NET 8, Minimal API, SQLite, JWT, REST              |
| HR desktop         | EmployeeWeb.Desktop | WinUI 3, C#, XAML – admin dashboard                 |
| Employee desktop   | EmployeeDesktop     | WinUI 3, C#, XAML – Travel Buddy, daily use         |

Same solution, one backend URL (`http://localhost:5000/`), one database. No duplicate apps or duplicate APIs.

Use **PROJECT_STATE_AND_CHANGES.md** for exact file locations and “do not duplicate” rules when you continue development later.
