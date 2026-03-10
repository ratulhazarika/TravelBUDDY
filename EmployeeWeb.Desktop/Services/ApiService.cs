using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EmployeeWeb.Desktop.Models;

namespace EmployeeWeb.Desktop.Services
{
    public class ApiResponse<T>
    {
        public bool Status { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? IssueWith { get; set; }
    }

    /// <summary>Concrete type for login API so generics resolve correctly.</summary>
    public class LoginApiResponse
    {
        public bool Status { get; set; }
        public UserInfo? Data { get; set; }
        public string? Message { get; set; }
        public string? IssueWith { get; set; }
    }

    public class ApiService
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiConfiguration.BaseUrl),
            DefaultRequestHeaders = { { "Accept", "application/json" } }
        };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// GET api/user - list all employees (same as React BASE_API_URL + 'api/user')
        /// </summary>
        public static async Task<List<EmployeeItem>> GetUsersAsync()
        {
            var response = await HttpClient.GetAsync("api/user");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<EmployeeItem>>>(json, JsonOptions);
            return wrapper?.Data ?? new List<EmployeeItem>();
        }

        /// <summary>
        /// GET api/dp/{id} - profile picture. Returns first item's profilePicture (base64).
        /// </summary>
        public static async Task<string?> GetProfilePictureAsync(string userId)
        {
            try
            {
                var response = await HttpClient.GetAsync($"api/dp/{userId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var first = data[0];
                    if (first.TryGetProperty("profilePicture", out var dp))
                        return dp.GetString();
                }
            }
            catch { /* ignore */ }
            return null;
        }

        /// <summary>
        /// GET api/user/{id} - full profile for a single employee.
        /// </summary>
        public static async Task<EmployeeProfile?> GetEmployeeProfileAsync(string userId)
        {
            var response = await HttpClient.GetAsync($"api/user/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponse<EmployeeProfile>>(json, JsonOptions);
            return wrapper?.Data;
        }

        /// <summary>
        /// POST api/login/date/{userId} with body { date: "YYYY-MM-DD" } - returns login/logout sessions.
        /// </summary>
        public static async Task<List<LoginLogEntry>> GetLoginLogAsync(string userId, string dateStr)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync($"api/login/date/{userId}", new { date = dateStr }, JsonOptions);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<ApiResponse<List<LoginLogEntry>>>(json, JsonOptions);
                return wrapper?.Data ?? new List<LoginLogEntry>();
            }
            catch { return new List<LoginLogEntry>(); }
        }

        /// <summary>
        /// POST api/authenticate/admin - payload: { staffEmail, password }. Returns user on success.
        /// </summary>
        public static async Task<(bool Success, UserInfo? User, string? Message, string? IssueWith)> LoginAsync(string email, string password)
        {
            try
            {
                var payload = new { staffEmail = email, password };
                var response = await HttpClient.PostAsJsonAsync("api/authenticate/admin", payload, JsonOptions);
                var json = await response.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<LoginApiResponse>(json, JsonOptions);
                if (wrapper == null)
                    return (false, null, "Invalid response", null);
                if (!wrapper.Status)
                    return (false, null, wrapper.Message ?? "Login failed", wrapper.IssueWith);
                return (true, wrapper.Data, null, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message, null);
            }
        }
    }

    public class LoginLogEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("login")]
        public string? Login { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("logout")]
        public string? Logout { get; set; }
    }
}
