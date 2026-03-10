using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using EmployeeWeb.Api.Data;
using EmployeeWeb.Api.Models.Entities;
using EmployeeWeb.Api.Models.Dtos;
using EmployeeWeb.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EmployeeWebApi",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EmployeeWeb",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ---- SIGNUP (Employee self-registration) ----
app.MapPost("/api/auth/signup", async (SignupRequest req, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.StaffEmail) || string.IsNullOrWhiteSpace(req.Password))
        return Results.Json(new ApiResponse<AdminUserDto> { Status = false, IssueWith = "email", Message = "Email and password required." });

    var emailLower = req.StaffEmail.Trim().ToLowerInvariant();
    var all = await db.Employees.ToListAsync();
    var existing = all.FirstOrDefault(e => (e.StaffEmail ?? "").ToLowerInvariant() == emailLower);
    if (existing != null)
        return Results.Json(new ApiResponse<AdminUserDto> { Status = false, IssueWith = "email", Message = "Email already registered." });

    var id = Guid.NewGuid().ToString("N");
    var emp = new EmployeeEntity
    {
        Id = id,
        StaffID = req.StaffID?.Trim() ?? $"EMP{id[..6].ToUpperInvariant()}",
        StaffName = req.StaffName?.Trim() ?? "New Employee",
        StaffEmail = req.StaffEmail.Trim(),
        PasswordHash = AuthService.HashPassword(req.Password),
        StaffPhone = req.StaffPhone ?? "",
        Role = req.Role ?? "Employee",
        Login = "false",
        Dob = "1990-01-01",
        Doj = DateTime.UtcNow.ToString("yyyy-MM-dd")
    };
    db.Employees.Add(emp);
    await db.SaveChangesAsync();

    var token = AuthService.GenerateJwt(emp, app.Configuration);
    var dto = new AdminUserDto
    {
        Id = emp.Id,
        StaffName = emp.StaffName,
        StaffID = emp.StaffID,
        Role = emp.Role,
        Token = token
    };
    return Results.Json(new ApiResponse<AdminUserDto> { Status = true, Data = dto, Message = "Account created." });
});

// ---- EMPLOYEE LOGIN (returns JWT) ----
app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db, IConfiguration config) =>
{
    var emailLower = (req.StaffEmail ?? "").Trim().ToLowerInvariant();
    var list = await db.Employees.ToListAsync();
    var emp = list.FirstOrDefault(e => (e.StaffEmail ?? "").ToLowerInvariant() == emailLower);
    if (emp == null)
        return Results.Json(new ApiResponse<AdminUserDto> { Status = false, IssueWith = "email", Message = "Invalid email or password." });
    if (!AuthService.VerifyPassword(req.Password ?? "", emp.PasswordHash))
        return Results.Json(new ApiResponse<AdminUserDto> { Status = false, IssueWith = "password", Message = "Invalid email or password." });

    var token = AuthService.GenerateJwt(emp, config);
    var dto = new AdminUserDto
    {
        Id = emp.Id,
        StaffName = emp.StaffName,
        StaffID = emp.StaffID,
        Role = emp.Role,
        Token = token
    };
    return Results.Json(new ApiResponse<AdminUserDto> { Status = true, Data = dto });
});

// ---- HR/ADMIN LOGIN (returns same shape as before; can optionally return token) ----
app.MapPost("/api/authenticate/admin", async (LoginRequest req, AppDbContext db, IConfiguration config) =>
{
    var emailLower = (req.StaffEmail ?? "").Trim().ToLowerInvariant();
    var list = await db.Employees.ToListAsync();
    var emp = list.FirstOrDefault(e => (e.StaffEmail ?? "").ToLowerInvariant() == emailLower);
    if (emp == null)
        return Results.Json(new ApiResponse<AdminUserDto> { Status = false, IssueWith = "email", Message = "Invalid email or password." });
    if (!AuthService.VerifyPassword(req.Password ?? "", emp.PasswordHash))
        return Results.Json(new ApiResponse<AdminUserDto> { Status = false, IssueWith = "password", Message = "Invalid email or password." });

    var token = AuthService.GenerateJwt(emp, config);
    var dto = new AdminUserDto
    {
        Id = emp.Id,
        StaffName = emp.StaffName,
        StaffID = emp.StaffID,
        Role = emp.Role,
        Token = token
    };
    return Results.Json(new ApiResponse<AdminUserDto> { Status = true, Data = dto });
});

// ---- CHANGE PASSWORD (employee or HR) ----
app.MapPost("/api/auth/change-password", async (ChangePasswordRequest req, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.EmployeeId) ||
        string.IsNullOrWhiteSpace(req.OldPassword) ||
        string.IsNullOrWhiteSpace(req.NewPassword))
    {
        return Results.Json(new ApiResponse<object>
        {
            Status = false,
            Message = "EmployeeId, oldPassword and newPassword are required."
        });
    }

    var emp = await db.Employees.FindAsync(req.EmployeeId);
    if (emp == null)
    {
        return Results.Json(new ApiResponse<object>
        {
            Status = false,
            Message = "Employee not found."
        });
    }

    if (!AuthService.VerifyPassword(req.OldPassword, emp.PasswordHash))
    {
        return Results.Json(new ApiResponse<object>
        {
            Status = false,
            IssueWith = "oldPassword",
            Message = "Current password is incorrect."
        });
    }

    emp.PasswordHash = AuthService.HashPassword(req.NewPassword);
    emp.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Json(new ApiResponse<object>
    {
        Status = true,
        Message = "Password updated successfully."
    });
});

// ---- ACTIVITY: Employee app records login (start of session) ----
app.MapPost("/api/activity/login", async (HttpRequest request, AppDbContext db) =>
{
    var empId = request.Headers["X-Employee-Id"].FirstOrDefault()
        ?? request.Query["employeeId"].FirstOrDefault();
    if (string.IsNullOrEmpty(empId))
        return Results.Json(new ApiResponse<object> { Status = false, Message = "X-Employee-Id or employeeId required." });

    var emp = await db.Employees.FindAsync(empId);
    if (emp == null)
        return Results.Json(new ApiResponse<object> { Status = false, Message = "Employee not found." });

    emp.Login = "true";
    emp.UpdatedAt = DateTime.UtcNow;

    var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var log = new LoginLogEntity
    {
        EmployeeId = empId,
        Date = dateStr,
        LoginTime = DateTime.UtcNow,
        LogoutTime = null
    };
    db.LoginLogs.Add(log);
    await db.SaveChangesAsync();

    return Results.Json(new ApiResponse<object> { Status = true, Message = "Session started." });
});

// ---- ACTIVITY: Employee app records logout (end of session) ----
app.MapPost("/api/activity/logout", async (HttpRequest request, AppDbContext db) =>
{
    var empId = request.Headers["X-Employee-Id"].FirstOrDefault()
        ?? request.Query["employeeId"].FirstOrDefault();
    if (string.IsNullOrEmpty(empId))
        return Results.Json(new ApiResponse<object> { Status = false, Message = "X-Employee-Id or employeeId required." });

    var emp = await db.Employees.FindAsync(empId);
    if (emp != null)
    {
        emp.Login = "false";
        emp.UpdatedAt = DateTime.UtcNow;
    }

    var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var openLog = await db.LoginLogs
        .Where(x => x.EmployeeId == empId && x.Date == dateStr && x.LogoutTime == null)
        .OrderByDescending(x => x.LoginTime)
        .FirstOrDefaultAsync();
    if (openLog != null)
    {
        openLog.LogoutTime = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Json(new ApiResponse<object> { Status = true, Message = "Session ended." });
});

// ---- USERS / EMPLOYEES ----
app.MapGet("/api/user", async (AppDbContext db) =>
{
    var list = await db.Employees
        .Select(e => new EmployeeListItemDto
        {
            Id = e.Id,
            StaffName = e.StaffName,
            StaffID = e.StaffID,
            Role = e.Role,
            Login = e.Login
        })
        .ToListAsync();
    return Results.Json(new ApiResponse<List<EmployeeListItemDto>> { Status = true, Data = list });
});

app.MapGet("/api/user/{id}", async (string id, AppDbContext db) =>
{
    var emp = await db.Employees.FindAsync(id);
    if (emp == null)
        return Results.Json(new ApiResponse<EmployeeProfileDto> { Status = false, Message = "Employee not found." });

    var profile = new EmployeeProfileDto
    {
        Id = emp.Id,
        StaffID = emp.StaffID,
        StaffName = emp.StaffName,
        StaffEmail = emp.StaffEmail,
        StaffPhone = emp.StaffPhone,
        Login = emp.Login,
        Role = emp.Role,
        Dob = emp.Dob,
        Doj = emp.Doj
    };
    return Results.Json(new ApiResponse<EmployeeProfileDto> { Status = true, Data = profile });
});

app.MapPost("/api/user", async (CreateEmployeeRequest req, AppDbContext db, IConfiguration config) =>
{
    var id = Guid.NewGuid().ToString("N");
    var emp = new EmployeeEntity
    {
        Id = id,
        StaffID = req.StaffID?.Trim() ?? $"EMP{id[..6].ToUpperInvariant()}",
        StaffName = req.StaffName?.Trim() ?? "New Employee",
        StaffEmail = req.StaffEmail?.Trim() ?? $"user{id[..6]}@example.com",
        PasswordHash = AuthService.HashPassword("changeme123"),
        StaffPhone = req.StaffPhone ?? "",
        Role = req.Role ?? "Employee",
        Login = "false",
        Dob = req.Dob ?? "1990-01-01",
        Doj = req.Doj ?? DateTime.UtcNow.ToString("yyyy-MM-dd")
    };
    db.Employees.Add(emp);
    await db.SaveChangesAsync();

    var dto = new EmployeeProfileDto
    {
        Id = emp.Id,
        StaffID = emp.StaffID,
        StaffName = emp.StaffName,
        StaffEmail = emp.StaffEmail,
        StaffPhone = emp.StaffPhone,
        Role = emp.Role,
        Dob = emp.Dob,
        Doj = emp.Doj
    };
    return Results.Json(new ApiResponse<EmployeeProfileDto> { Status = true, Data = dto, Message = "Employee created." });
});

// ---- UPDATE EMPLOYEE PROFILE ----
app.MapPut("/api/user/{id}", async (string id, CreateEmployeeRequest req, AppDbContext db) =>
{
    var emp = await db.Employees.FindAsync(id);
    if (emp == null)
        return Results.Json(new ApiResponse<EmployeeProfileDto> { Status = false, Message = "Employee not found." });

    if (!string.IsNullOrWhiteSpace(req.StaffName))
        emp.StaffName = req.StaffName.Trim();
    if (!string.IsNullOrWhiteSpace(req.StaffEmail))
        emp.StaffEmail = req.StaffEmail.Trim();
    if (!string.IsNullOrWhiteSpace(req.StaffPhone))
        emp.StaffPhone = req.StaffPhone.Trim();
    if (!string.IsNullOrWhiteSpace(req.Role))
        emp.Role = req.Role.Trim();
    if (!string.IsNullOrWhiteSpace(req.Dob))
        emp.Dob = req.Dob.Trim();
    if (!string.IsNullOrWhiteSpace(req.Doj))
        emp.Doj = req.Doj.Trim();

    emp.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var profile = new EmployeeProfileDto
    {
        Id = emp.Id,
        StaffID = emp.StaffID,
        StaffName = emp.StaffName,
        StaffEmail = emp.StaffEmail,
        StaffPhone = emp.StaffPhone,
        Login = emp.Login,
        Role = emp.Role,
        Dob = emp.Dob,
        Doj = emp.Doj
    };
    return Results.Json(new ApiResponse<EmployeeProfileDto> { Status = true, Data = profile, Message = "Profile updated." });
});

app.MapDelete("/api/user/{id}", async (string id, AppDbContext db) =>
{
    var emp = await db.Employees.FindAsync(id);
    if (emp == null)
        return Results.Json(new ApiResponse<object> { Status = false, Message = "Employee not found." });

    db.Employees.Remove(emp);
    await db.SaveChangesAsync();
    return Results.Json(new ApiResponse<object> { Status = true, Message = "Employee removed." });
});

// ---- DISPLAY PICTURE ----
app.MapGet("/api/dp/{id}", async (string id, AppDbContext db) =>
{
    var emp = await db.Employees.FindAsync(id);
    var list = new List<EmployeeDpDto>();
    if (emp != null && !string.IsNullOrEmpty(emp.ProfilePicture))
        list.Add(new EmployeeDpDto { ProfilePicture = emp.ProfilePicture });
    return Results.Json(new ApiResponse<List<EmployeeDpDto>> { Status = true, Data = list });
});

// ---- LOGIN LOGS / ATTENDANCE ----
app.MapPost("/api/login/date/{id}", async (string id, LoginLogRequestDto body, AppDbContext db) =>
{
    var dateKey = body.Date?.Trim() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
    var logs = await db.LoginLogs
        .Where(x => x.EmployeeId == id && x.Date == dateKey)
        .OrderBy(x => x.LoginTime)
        .ToListAsync();

    var sessions = logs.Select(l => new LoginSessionDto
    {
        Login = l.LoginTime.ToString("yyyy-MM-dd HH:mm:ss"),
        Logout = l.LogoutTime?.ToString("yyyy-MM-dd HH:mm:ss")
    }).ToList();

    return Results.Json(new ApiResponse<List<LoginSessionDto>> { Status = true, Data = sessions });
});

// ---- TICKETS (stubbed) ----
app.MapGet("/api/ticket/pending", () =>
    Results.Json(new ApiResponse<List<TicketDto>> { Status = true, Data = new List<TicketDto>() }));
app.MapGet("/api/ticket/completed", () =>
    Results.Json(new ApiResponse<List<TicketDto>> { Status = true, Data = new List<TicketDto>() }));

// ---- DELETE OLD DATA (stubbed) ----
app.MapPost("/api/screenshot", () =>
    Results.Json(new ApiResponse<object> { Status = true, Message = "Old data deletion simulated (no-op)." }));

app.Run();
