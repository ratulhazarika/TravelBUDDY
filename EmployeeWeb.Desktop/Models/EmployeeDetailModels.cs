using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EmployeeWeb.Desktop.Models
{
    /// <summary>
    /// Full employee profile returned from GET api/user/{id}.
    /// This mirrors the minimal API's EmployeeProfile type and is intentionally
    /// a subset of the much larger React model. Missing fields can be added
    /// later without breaking callers.
    /// </summary>
    public class EmployeeProfile
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
        public string StaffType { get; set; } = "Full‑Time";
        public string ShiftStartTime { get; set; } = "09:30 AM";

        public string Dob { get; set; } = "1990-01-01";
        public string Doj { get; set; } = "2020-01-01";
    }

    /// <summary>
    /// Single timeline entry combining login/logout/break events for the day.
    /// </summary>
    public class EmployeeTimeLogItem
    {
        public DateTime Time { get; set; }
        /// <summary>Pre-formatted time string for XAML binding (WinUI 3 has no StringFormat on Binding).</summary>
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

        public string FirstLogin { get; set; } = "N/A";
        public string LastLogoutOrStatus { get; set; } = "N/A";
    }

    /// <summary>
    /// Row for the HR \"Employee Table\" view (per-employee daily summary).
    /// </summary>
    public class EmployeeTableRow
    {
        public string StaffName { get; set; } = string.Empty;
        public string StaffID { get; set; } = string.Empty;
        public string FirstLogin { get; set; } = "N/A";
        public string LastLogout { get; set; } = "N/A";
        public string TotalWork { get; set; } = "0h 0m";
        public string TotalBreak { get; set; } = "0h 0m";
        public string Status { get; set; } = "No activity";
    }
}

