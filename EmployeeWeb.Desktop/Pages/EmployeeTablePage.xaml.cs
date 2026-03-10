using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeWeb.Desktop.Models;
using EmployeeWeb.Desktop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EmployeeWeb.Desktop.Pages
{
    public sealed partial class EmployeeTablePage : Page
    {
        public EmployeeTablePage()
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTimeOffset.Now;
            _ = LoadTableAsync();
        }

        private async void DatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            await LoadTableAsync();
        }

        private async Task LoadTableAsync()
        {
            try
            {
                var users = await ApiService.GetUsersAsync();
                if (users.Count == 0)
                {
                    EmployeeTable.ItemsSource = Array.Empty<EmployeeTableRow>();
                    return;
                }

                var date = DatePicker.SelectedDate?.Date ?? DatePicker.Date.Date;
                var dateStr = date.ToString("yyyy-MM-dd");

                // Fetch logs for all employees in parallel
                var logTasks = users.Select(u => ApiService.GetLoginLogAsync(u.Id, dateStr));
                var logResults = await Task.WhenAll(logTasks);

                var rows = new List<EmployeeTableRow>();

                for (int i = 0; i < users.Count; i++)
                {
                    var user = users[i];
                    var logs = logResults[i] ?? new List<LoginLogEntry>();

                    BuildSummary(logs, out var summary);

                    rows.Add(new EmployeeTableRow
                    {
                        StaffName = user.StaffName,
                        StaffID = user.StaffID,
                        FirstLogin = summary.FirstLogin,
                        LastLogout = summary.LastLogoutOrStatus,
                        TotalWork = summary.TotalWorkTime,
                        TotalBreak = summary.TotalBreakTime,
                        Status = summary.LastLogoutOrStatus
                    });
                }

                // Order by name for consistent view
                EmployeeTable.ItemsSource = rows.OrderBy(r => r.StaffName).ToList();
            }
            catch
            {
                EmployeeTable.ItemsSource = Array.Empty<EmployeeTableRow>();
            }
        }

        private static void BuildSummary(
            List<LoginLogEntry> logs,
            out EmployeeDailySummary summary)
        {
            summary = new EmployeeDailySummary();

            if (logs == null || logs.Count == 0)
            {
                summary.LastLogoutOrStatus = "No activity";
                return;
            }

            // Ensure logs are sorted by login time
            logs = logs
                .Where(l => !string.IsNullOrEmpty(l.Login))
                .OrderBy(l => DateTime.TryParse(l.Login, out var t) ? t : DateTime.MaxValue)
                .ToList();

            if (logs.Count == 0)
            {
                summary.LastLogoutOrStatus = "No activity";
                return;
            }

            DateTime? firstLogin = null;
            DateTime? lastLogout = null;
            long totalFocusMs = 0;
            long totalBreakMs = 0;

            for (int i = 0; i < logs.Count; i++)
            {
                if (!DateTime.TryParse(logs[i].Login, out var loginTime))
                    continue;

                firstLogin ??= loginTime;

                DateTime logoutTime;
                if (string.IsNullOrEmpty(logs[i].Logout))
                {
                    // Still online; use now as logout for focus calculations
                    logoutTime = DateTime.Now;
                    summary.LastLogoutOrStatus = "Online";
                }
                else if (!DateTime.TryParse(logs[i].Logout, out logoutTime))
                {
                    continue;
                }
                else
                {
                    lastLogout = logoutTime;
                }

                var focusSpan = logoutTime - loginTime;
                if (focusSpan > TimeSpan.Zero)
                    totalFocusMs += (long)focusSpan.TotalMilliseconds;

                // Break between this logout and next login
                if (i < logs.Count - 1 && !string.IsNullOrEmpty(logs[i + 1].Login))
                {
                    if (DateTime.TryParse(logs[i + 1].Login, out var nextLogin))
                    {
                        var breakSpan = nextLogin - logoutTime;
                        if (breakSpan > TimeSpan.Zero)
                        {
                            totalBreakMs += (long)breakSpan.TotalMilliseconds;
                        }
                    }
                }
            }

            summary.TotalFocusTime = ToHoursMinutes(totalFocusMs);
            summary.TotalBreakTime = ToHoursMinutes(totalBreakMs);
            summary.TotalWorkTime = ToHoursMinutes(totalFocusMs + totalBreakMs);
            summary.FirstLogin = firstLogin?.ToString("HH:mm") ?? "N/A";

            if (summary.LastLogoutOrStatus != "Online")
            {
                summary.LastLogoutOrStatus = lastLogout?.ToString("HH:mm") ?? "Offline";
            }
        }

        private static string ToHoursMinutes(long totalMs)
        {
            if (totalMs <= 0) return "0h 0m";
            var ts = TimeSpan.FromMilliseconds(totalMs);
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
    }
}
