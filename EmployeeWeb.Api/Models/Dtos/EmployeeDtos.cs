using System.Text.Json.Serialization;

namespace EmployeeWeb.Api.Models.Dtos;

public class EmployeeListItemDto
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string StaffID { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Login { get; set; } = "false";
}

public class EmployeeProfileDto
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    public string StaffID { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string StaffEmail { get; set; } = string.Empty;
    public string StaffPhone { get; set; } = string.Empty;
    public string Login { get; set; } = "false";
    public string Role { get; set; } = string.Empty;
    public string StaffStatus { get; set; } = "Active";
    public string StaffType { get; set; } = "Full-Time";
    public string ShiftStartTime { get; set; } = "09:30 AM";
    public string Dob { get; set; } = string.Empty;
    public string Doj { get; set; } = string.Empty;
}

public class CreateEmployeeRequest
{
    public string? StaffID { get; set; }
    public string? StaffName { get; set; }
    public string? StaffEmail { get; set; }
    public string? StaffPhone { get; set; }
    public string? Role { get; set; }
    public string? Dob { get; set; }
    public string? Doj { get; set; }
}

public class EmployeeDpDto
{
    [JsonPropertyName("profilePicture")]
    public string? ProfilePicture { get; set; }
}

public class LoginSessionDto
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("logout")]
    public string? Logout { get; set; }
}

public class LoginLogRequestDto
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public class TicketDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}
