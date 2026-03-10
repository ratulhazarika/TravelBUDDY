using System.Text.Json.Serialization;

namespace EmployeeDesktop.Models;

/// <summary>
/// Logged-in user info stored locally and used for API calls.
/// </summary>
public class UserInfo
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    public string StaffName { get; set; } = string.Empty;
    public string StaffID { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Token { get; set; }
}
