using System;

namespace EmployeeDesktop.Models;
public class EmployeeProfile
{
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

/// <summary>
/// Single timeline entry combining login/logout/break events for the day.
/// </summary>
public class EmployeeTimeLogItem
{
    public DateTime Time { get; set; }
    /// <summary>Pre-formatted time string for XAML binding.</summary>
    public string TimeFormatted => Time.ToString("HH:mm");
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty; // "login", "logout", "break"
}

/// <summary>
/// Aggregated daily activity snapshot (total focus/break, first/last activity).
/// </summary>
public class EmployeeDailySummary
{
    public string TotalFocusTime { get; set; } = "0h 0m";
    public string TotalBreakTime { get; set; } = "0h 0m";
    public string TotalWorkTime { get; set; } = "0h 0m";

    /// <summary>Percentage of focus time vs (focus + break) for today.</summary>
    public int ProductivityPercent { get; set; } = 0;

    public string FirstLogin { get; set; } = "N/A";
    public string LastLogoutOrStatus { get; set; } = "N/A";

    public string ShiftStartTime { get; set; } = "09:30 AM";
    public string ShiftRemaining { get; set; } = "N/A";

    /// <summary>How much of the shift is done (0-100%).</summary>
    public int ShiftDonePercent { get; set; } = 0;
}

/// <summary>
/// Raw login/logout entries returned by the API.
/// </summary>
public class LoginLogEntry
{
    public string? Login { get; set; }
    public string? Logout { get; set; }
}

