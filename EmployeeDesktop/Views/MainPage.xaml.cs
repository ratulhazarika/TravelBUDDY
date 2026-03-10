using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using EmployeeDesktop.Models;
using EmployeeDesktop.Services;

namespace EmployeeDesktop.Views;

public sealed partial class MainPage : Page
{
    private static readonly string[] LogTimeFormats = { "yyyy-MM-dd HH:mm:ss" };
    private CancellationTokenSource? _loadCts;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        var user = AuthService.CurrentUser;
        if (user == null)
        {
            WelcomeText.Text = "Not logged in";
            StatusText.Text = "Please sign in.";
            ClearUi();
            return;
        }

        WelcomeText.Text = $"Welcome, {user.StaffName}!";
        StatusText.Text = "You are logged in. Your session is being tracked.";
        ProfileHeaderNameText.Text = user.StaffName;

        try
        {
            var today = DateTime.Now.Date;
            var dateStr = today.ToString("yyyy-MM-dd");

            var profileTask = ApiService.GetEmployeeProfileAsync(user.Id);
            var logsTask = ApiService.GetLoginLogAsync(user.Id, dateStr);
            await Task.WhenAll(profileTask, logsTask);
            if (token.IsCancellationRequested) return;

            var profile = await profileTask;
            if (profile != null)
            {
                BindProfile(profile);
            }

            var now = DateTime.Now;
            var logs = await logsTask;
            var computed = await Task.Run(() =>
            {
                BuildSummaryAndTimeline(logs, now, out var summary, out var items);
                return (summary, items);
            }, token);

            if (token.IsCancellationRequested) return;

            BindSummary(computed.summary);
            TimeLogList.ItemsSource = computed.items;
        }
        catch
        {
            ClearUi();
        }

        ShowDashboard();
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

        ProfileHeaderNameText.Text = string.IsNullOrWhiteSpace(profile.StaffName) ? "Employee" : profile.StaffName;
        EditNameBox.Text = profile.StaffName ?? "";
        EditEmailBox.Text = profile.StaffEmail ?? "";
        EditPhoneBox.Text = profile.StaffPhone ?? "";
        EditDobBox.Text = profile.Dob ?? "";
        EditStaffIdBox.Text = profile.StaffID ?? "";
        EditRoleBox.Text = profile.Role ?? "";
        EditDojBox.Text = profile.Doj ?? "";
    }

    private void BindSummary(EmployeeDailySummary summary)
    {
        ShiftStartText.Text = summary.ShiftStartTime;
        ShiftRemainingText.Text = summary.ShiftRemaining;
        TotalWorkText.Text = summary.TotalWorkTime;
        TotalBreakText.Text = summary.TotalBreakTime;
        FirstLoginText.Text = summary.FirstLogin;
        LastLogoutText.Text = summary.LastLogoutOrStatus;

        ProductivityText.Text = $"{summary.ProductivityPercent}%";
        ProductivityMessageText.Text = summary.ProductivityPercent >= 80 ? "Good job!" : "Keep going";

        ShiftProgressBar.Value = summary.ShiftDonePercent;
        ShiftDoneText.Text = $"{summary.ShiftDonePercent}%";

        // Simple derived shift end time based on start + 8.5 hours
        if (DateTime.TryParse(summary.ShiftStartTime, out var startTimeOnly))
        {
            var today = DateTime.Now.Date;
            var start = today.Add(startTimeOnly.TimeOfDay);
            var end = start.AddHours(8.5);
            ShiftEndText.Text = end.ToString("hh:mm tt");
        }
        else
        {
            ShiftEndText.Text = "--";
        }
    }

    private void ClearUi()
    {
        ProfileNameText.Text = "No employee";
        ProfileRoleText.Text = "";
        ProfileStaffIdText.Text = "-";
        ProfileEmailText.Text = "-";
        ProfilePhoneText.Text = "-";
        ProfileStatusText.Text = "-";
        ProfileTypeText.Text = "-";

        var emptySummary = new EmployeeDailySummary();
        BindSummary(emptySummary);
        TimeLogList.ItemsSource = Array.Empty<EmployeeTimeLogItem>();
    }

    private static void BuildSummaryAndTimeline(
        List<LoginLogEntry> logs,
        DateTime now,
        out EmployeeDailySummary summary,
        out List<EmployeeTimeLogItem> items)
    {
        summary = new EmployeeDailySummary();
        items = new List<EmployeeTimeLogItem>();

        if (logs == null || logs.Count == 0)
        {
            summary.LastLogoutOrStatus = "No activity";
            ComputeShift(summary, now);
            return;
        }

        // Ensure logs are sorted by login time (only sort if needed)
        if (!IsSortedByLoginTime(logs))
        {
            logs = logs
                .Where(l => !string.IsNullOrEmpty(l.Login))
                .OrderBy(l => TryParseLogTime(l.Login, out var t) ? t : DateTime.MaxValue)
                .ToList();
        }

        if (logs.Count == 0)
        {
            summary.LastLogoutOrStatus = "No activity";
            ComputeShift(summary, now);
            return;
        }

        DateTime? firstLogin = null;
        DateTime? lastLogout = null;
        long totalFocusMs = 0;
        long totalBreakMs = 0;

        items = new List<EmployeeTimeLogItem>(logs.Count * 2);

        for (int i = 0; i < logs.Count; i++)
        {
            if (!TryParseLogTime(logs[i].Login, out var loginTime))
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
                logoutTime = now;
                summary.LastLogoutOrStatus = "Online";
            }
            else if (!TryParseLogTime(logs[i].Logout, out logoutTime))
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
                if (TryParseLogTime(logs[i + 1].Login, out var nextLogin))
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

        var totalMs = totalFocusMs + totalBreakMs;
        summary.ProductivityPercent = totalMs > 0
            ? (int)Math.Round(100.0 * totalFocusMs / totalMs)
            : 0;
        summary.FirstLogin = firstLogin?.ToString("HH:mm") ?? "N/A";

        if (summary.LastLogoutOrStatus != "Online")
        {
            summary.LastLogoutOrStatus = lastLogout?.ToString("HH:mm") ?? "Offline";
        }

        ComputeShift(summary, now);
    }

    private static string ToHoursMinutes(long totalMs)
    {
        if (totalMs <= 0) return "0h 0m";
        var ts = TimeSpan.FromMilliseconds(totalMs);
        return $"{(int)ts.TotalHours}h {ts.Minutes}m";
    }

    /// <summary>
    /// Compute shift remaining assuming an 8.5 hour workday starting at ShiftStartTime.
    /// </summary>
    private static void ComputeShift(EmployeeDailySummary summary, DateTime now)
    {
        summary.ShiftStartTime = string.IsNullOrWhiteSpace(summary.ShiftStartTime)
            ? "09:30 AM"
            : summary.ShiftStartTime;

        if (!DateTime.TryParse(summary.ShiftStartTime, out var startTimeOnly))
        {
            summary.ShiftRemaining = "N/A";
            summary.ShiftDonePercent = 0;
            return;
        }

        var today = now.Date;
        var start = today.Add(startTimeOnly.TimeOfDay);
        var end = start.AddHours(8.5); // typical full day

        if (now >= end)
        {
            summary.ShiftRemaining = "Shift over";
            summary.ShiftDonePercent = 100;
        }
        else if (now <= start)
        {
            var untilStart = start - now;
            summary.ShiftRemaining = $"Starts in {(int)untilStart.TotalHours}h {untilStart.Minutes}m";
            summary.ShiftDonePercent = 0;
        }
        else
        {
            var remaining = end - now;
            summary.ShiftRemaining = $"{(int)remaining.TotalHours}h {remaining.Minutes}m left";
            var elapsed = TimeSpan.FromHours(8.5) - remaining;
            var percent = elapsed.TotalMilliseconds / TimeSpan.FromHours(8.5).TotalMilliseconds * 100.0;
            summary.ShiftDonePercent = (int)Math.Clamp(percent, 0, 100);
        }
    }

    private static bool TryParseLogTime(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return DateTime.TryParseExact(
                   value,
                   LogTimeFormats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeLocal,
                   out result)
               || DateTime.TryParse(value, out result);
    }

    private static bool IsSortedByLoginTime(List<LoginLogEntry> logs)
    {
        DateTime last = DateTime.MinValue;
        bool hasLast = false;

        for (int i = 0; i < logs.Count; i++)
        {
            if (!TryParseLogTime(logs[i].Login, out var loginTime))
                continue;

            if (hasLast && loginTime < last)
                return false;

            last = loginTime;
            hasLast = true;
        }

        return true;
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var user = AuthService.CurrentUser;
        if (user != null)
        {
            _ = ApiService.RecordActivityLogoutAsync(user.Id);
        }
        AuthService.ClearUser();
        Frame?.Navigate(typeof(LoginPage));
    }

    private void GoOfflineButton_Click(object sender, RoutedEventArgs e)
    {
        // For now, treat "Go Offline" the same as Logout to end the tracked session.
        LogoutButton_Click(sender, e);
    }

    private void ShowDashboard()
    {
        DashboardView.Visibility = Visibility.Visible;
        ProfileView.Visibility = Visibility.Collapsed;

        DashboardNavButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 248, 187, 208));
        ProfileNavButton.Background = new SolidColorBrush(Colors.Transparent);
        SettingsNavButton.Background = new SolidColorBrush(Colors.Transparent);
    }

    private void ShowProfile()
    {
        DashboardView.Visibility = Visibility.Collapsed;
        ProfileView.Visibility = Visibility.Visible;

        DashboardNavButton.Background = new SolidColorBrush(Colors.Transparent);
        ProfileNavButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 248, 187, 208));
        SettingsNavButton.Background = new SolidColorBrush(Colors.Transparent);
    }

    private void DashboardNavButton_Click(object sender, RoutedEventArgs e)
    {
        ShowDashboard();
    }

    private void ProfileNavButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProfile();
    }

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
    {
        // For now, Settings focuses the profile / change password area.
        ShowProfile();
    }

    private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var user = AuthService.CurrentUser;
        if (user == null)
            return;

        ProfileErrorText.Visibility = Visibility.Collapsed;

        var request = new EmployeeProfile
        {
            Id = user.Id,
            StaffID = EditStaffIdBox.Text ?? string.Empty,
            StaffName = EditNameBox.Text ?? string.Empty,
            StaffEmail = EditEmailBox.Text ?? string.Empty,
            StaffPhone = EditPhoneBox.Text ?? string.Empty,
            Dob = EditDobBox.Text ?? string.Empty,
            Doj = EditDojBox.Text ?? string.Empty,
            Role = EditRoleBox.Text ?? string.Empty
        };

        try
        {
            var response = await ApiService.UpdateEmployeeProfileAsync(user.Id, request);
            if (!response.Status || response.Data == null)
            {
                ProfileErrorText.Text = response.Message ?? "Unable to save profile.";
                ProfileErrorText.Visibility = Visibility.Visible;
                return;
            }

            // Refresh UI with saved values
            BindProfile(response.Data);
        }
        catch (Exception ex)
        {
            ProfileErrorText.Text = ex.Message;
            ProfileErrorText.Visibility = Visibility.Visible;
        }
    }

    private async void UpdatePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var user = AuthService.CurrentUser;
        if (user == null)
            return;

        PasswordErrorText.Visibility = Visibility.Collapsed;

        var oldPwd = OldPasswordBox.Password ?? string.Empty;
        var newPwd = NewPasswordBox.Password ?? string.Empty;
        var confirmPwd = ConfirmPasswordBox.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(oldPwd) || string.IsNullOrWhiteSpace(newPwd) || string.IsNullOrWhiteSpace(confirmPwd))
        {
            PasswordErrorText.Text = "All password fields are required.";
            PasswordErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (!string.Equals(newPwd, confirmPwd, StringComparison.Ordinal))
        {
            PasswordErrorText.Text = "New password and confirmation do not match.";
            PasswordErrorText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var response = await ApiService.ChangePasswordAsync(user.Id, oldPwd, newPwd);
            if (!response.Status)
            {
                PasswordErrorText.Text = response.Message ?? "Password update failed.";
                PasswordErrorText.Visibility = Visibility.Visible;
                return;
            }

            // Clear boxes on success
            OldPasswordBox.Password = string.Empty;
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;
        }
        catch (Exception ex)
        {
            PasswordErrorText.Text = ex.Message;
            PasswordErrorText.Visibility = Visibility.Visible;
        }
    }

    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProfile();
    }
}
