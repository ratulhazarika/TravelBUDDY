using System.Net.Http.Json;
using System.Text.Json;
using EmployeeDesktop.Models;

namespace EmployeeDesktop.Services;

/// <summary>
/// HTTP client for EmployeeWeb.Api.
/// </summary>
public static class ApiService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(ApiConfiguration.BaseUrl),
        DefaultRequestHeaders = { { "Accept", "application/json" } }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void SetToken(string? token)
    {
        Client.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(token))
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
    }

    /// <summary>
    /// POST /api/auth/signup - employee registration.
    /// </summary>
    public static async Task<ApiResponse<UserInfo>> SignupAsync(string email, string password, string name, string? phone, string? staffId)
    {
        var body = new
        {
            staffEmail = email,
            password,
            staffName = name,
            staffPhone = phone ?? "",
            staffID = staffId
        };
        var response = await Client.PostAsJsonAsync("api/auth/signup", body, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<UserInfo>>(json, JsonOptions);
        return wrapper ?? new ApiResponse<UserInfo> { Status = false, Message = "Invalid response." };
    }

    /// <summary>
    /// POST /api/auth/login - employee login (returns JWT).
    /// </summary>
    public static async Task<ApiResponse<UserInfo>> LoginAsync(string email, string password)
    {
        var body = new { staffEmail = email, password };
        var response = await Client.PostAsJsonAsync("api/auth/login", body, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<UserInfo>>(json, JsonOptions);
        return wrapper ?? new ApiResponse<UserInfo> { Status = false, Message = "Invalid response." };
    }

    /// <summary>
    /// GET /api/user/{id} - full profile for the logged-in employee.
    /// </summary>
    public static async Task<EmployeeProfile?> GetEmployeeProfileAsync(string userId)
    {
        var response = await Client.GetAsync($"api/user/{userId}");
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<EmployeeProfile>>(json, JsonOptions);
        return wrapper?.Data;
    }

    /// <summary>
    /// POST /api/login/date/{id} - daily login/logout sessions for the employee.
    /// </summary>
    public static async Task<List<LoginLogEntry>> GetLoginLogAsync(string userId, string dateStr)
    {
        try
        {
            var response = await Client.PostAsJsonAsync(
                $"api/login/date/{userId}",
                new { date = dateStr },
                JsonOptions);
            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<LoginLogEntry>>>(json, JsonOptions);
            return wrapper?.Data ?? new List<LoginLogEntry>();
        }
        catch
        {
            return new List<LoginLogEntry>();
        }
    }

    /// <summary>
    /// POST /api/activity/login - record session start (employee app).
    /// </summary>
    public static async Task<ApiResponse<object>> RecordActivityLoginAsync(string employeeId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/activity/login");
        req.Headers.Add("X-Employee-Id", employeeId);
        var response = await Client.SendAsync(req);
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<object>>(json, JsonOptions);
        return wrapper ?? new ApiResponse<object> { Status = false };
    }

    /// <summary>
    /// POST /api/activity/logout - record session end (employee app).
    /// </summary>
    public static async Task<ApiResponse<object>> RecordActivityLogoutAsync(string employeeId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/activity/logout");
        req.Headers.Add("X-Employee-Id", employeeId);
        var response = await Client.SendAsync(req);
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<object>>(json, JsonOptions);
        return wrapper ?? new ApiResponse<object> { Status = false };
    }

    /// <summary>
    /// PUT /api/user/{id} - update profile fields for the logged-in employee.
    /// </summary>
    public static async Task<ApiResponse<EmployeeProfile>> UpdateEmployeeProfileAsync(string userId, EmployeeProfile profile)
    {
        var body = new
        {
            staffID = profile.StaffID,
            staffName = profile.StaffName,
            staffEmail = profile.StaffEmail,
            staffPhone = profile.StaffPhone,
            role = profile.Role,
            dob = profile.Dob,
            doj = profile.Doj
        };

        var response = await Client.PutAsJsonAsync($"api/user/{userId}", body, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<EmployeeProfile>>(json, JsonOptions);
        return wrapper ?? new ApiResponse<EmployeeProfile> { Status = false, Message = "Invalid response." };
    }

    /// <summary>
    /// POST /api/auth/change-password - update the current user's password.
    /// </summary>
    public static async Task<ApiResponse<object>> ChangePasswordAsync(string employeeId, string oldPassword, string newPassword)
    {
        var body = new
        {
            employeeId,
            oldPassword,
            newPassword
        };

        var response = await Client.PostAsJsonAsync("api/auth/change-password", body, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<ApiResponse<object>>(json, JsonOptions);
        return wrapper ?? new ApiResponse<object> { Status = false, Message = "Invalid response." };
    }
}

public class ApiResponse<T>
{
    public bool Status { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? IssueWith { get; set; }
}
