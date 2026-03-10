using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeWeb.Desktop.Models;
using EmployeeWeb.Desktop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace EmployeeWeb.Desktop.Pages
{
    public sealed partial class EmployeesPage : Page
    {
        private string? _employeeId;

        public EmployeesPage()
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTimeOffset.Now;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _employeeId = e.Parameter as string;
            if (string.IsNullOrEmpty(_employeeId))
            {
                SubtitleText.Text = "Select an employee from the search bar.";
                ClearUi();
                return;
            }

            SubtitleText.Text = "Employee ID: " + _employeeId;
            await LoadEmployeeAsync();
        }

        private async void DatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(_employeeId))
            {
                await LoadEmployeeAsync();
            }
        }

        private async Task LoadEmployeeAsync()
        {
            if (string.IsNullOrEmpty(_employeeId))
                return;

            try
            {
                var profile = await ApiService.GetEmployeeProfileAsync(_employeeId);
                if (profile != null)
                {
                    BindProfile(profile);
                }
                else
                {
                    ClearProfile();
                }

                // Use SelectedDate (nullable) if available, otherwise fall back to Date (non-nullable)
                var date = DatePicker.SelectedDate?.Date ?? DatePicker.Date.Date;
                var dateStr = date.ToString("yyyy-MM-dd");
                var logs = await ApiService.GetLoginLogAsync(_employeeId, dateStr);

                BuildSummaryAndTimeline(logs, out var summary, out var items);
                BindSummary(summary);
                TimeLogList.ItemsSource = items;
            }
            catch
            {
                ClearUi();
            }
        }

        private void BindProfile(EmployeeProfile profile)
        {
            ProfileNameText.Text = string.IsNullOrWhiteSpace(profile.StaffName)
                ? "Unknown employee"
                : profile.StaffName;
            ProfileRoleText.Text = string.IsNullOrWhiteSpace(profile.Role) ? "" : profile.Role;
            ProfileStaffIdText.Text = string.IsNullOrWhiteSpace(profile.StaffID) ? "-" : profile.StaffID;
            ProfileEmailText.Text = string.IsNullOrWhiteSpace(profile.StaffEmail) ? "-" : profile.StaffEmail;
            ProfilePhoneText.Text = string.IsNullOrWhiteSpace(profile.StaffPhone) ? "-" : profile.StaffPhone;
            ProfileStatusText.Text = string.IsNullOrWhiteSpace(profile.StaffStatus) ? "-" : profile.StaffStatus;
            ProfileTypeText.Text = string.IsNullOrWhiteSpace(profile.StaffType) ? "-" : profile.StaffType;
        }

        private void BindSummary(EmployeeDailySummary summary)
        {
            TotalWorkText.Text = summary.TotalWorkTime;
            TotalBreakText.Text = summary.TotalBreakTime;
            StatusText.Text = summary.LastLogoutOrStatus;
            FirstLoginText.Text = summary.FirstLogin;
            LastLogoutText.Text = summary.LastLogoutOrStatus;
        }

        private void ClearProfile()
        {
            ProfileNameText.Text = "No employee selected";
            ProfileRoleText.Text = "";
            ProfileStaffIdText.Text = "-";
            ProfileEmailText.Text = "-";
            ProfilePhoneText.Text = "-";
            ProfileStatusText.Text = "-";
            ProfileTypeText.Text = "-";
        }

        private void ClearUi()
        {
            ClearProfile();
            var emptySummary = new EmployeeDailySummary();
            BindSummary(emptySummary);
            TimeLogList.ItemsSource = Array.Empty<EmployeeTimeLogItem>();
        }

        private static void BuildSummaryAndTimeline(
            List<LoginLogEntry> logs,
            out EmployeeDailySummary summary,
            out List<EmployeeTimeLogItem> items)
        {
            summary = new EmployeeDailySummary();
            items = new List<EmployeeTimeLogItem>();

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

                // Login event
                items.Add(new EmployeeTimeLogItem
                {
                    Time = loginTime,
                    Label = "Login",
                    Kind = "login"
                });

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

                // Logout event
                items.Add(new EmployeeTimeLogItem
                {
                    Time = logoutTime,
                    Label = "Logout",
                    Kind = "logout"
                });

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
                            items.Add(new EmployeeTimeLogItem
                            {
                                Time = logoutTime.AddMilliseconds(breakSpan.TotalMilliseconds / 2),
                                Label = $"Break ({(int)breakSpan.TotalMinutes}m)",
                                Kind = "break"
                            });
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

            // Sort events by time
            items = items.OrderBy(i => i.Time).ToList();
        }

        private static string ToHoursMinutes(long totalMs)
        {
            if (totalMs <= 0) return "0h 0m";
            var ts = TimeSpan.FromMilliseconds(totalMs);
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
    }
}
