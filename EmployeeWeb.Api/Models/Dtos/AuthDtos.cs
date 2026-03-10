using System.Text.Json.Serialization;

namespace EmployeeWeb.Api.Models.Dtos;

public class ApiResponse<T>
{
    public bool Status { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? IssueWith { get; set; }
}

public class SignupRequest
{
    [JsonPropertyName("staffEmail")]
    public string StaffEmail { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("staffName")]
    public string StaffName { get; set; } = string.Empty;

    [JsonPropertyName("staffPhone")]
    public string? StaffPhone { get; set; }

    [JsonPropertyName("staffID")]
    public string? StaffID { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class LoginRequest
{
    [JsonPropertyName("staffEmail")]
    public string StaffEmail { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class AdminUserDto
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string StaffID { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Token { get; set; }
}

public class ChangePasswordRequest
{
    [JsonPropertyName("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [JsonPropertyName("oldPassword")]
    public string OldPassword { get; set; } = string.Empty;

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}
