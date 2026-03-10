using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EmployeeWeb.Desktop.Models;
using EmployeeWeb.Desktop.Services;

namespace EmployeeWeb.Desktop.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private const long MinMediumBreakMs = 45 * 60 * 1000;
        private const long MaxMediumBreakMs = 90 * 60 * 1000;
        private const int MaxLogFetchConcurrency = 6;
        private static readonly string[] LogTimeFormats = { "yyyy-MM-dd HH:mm:ss" };
        private CancellationTokenSource? _loadCts;

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            await FetchOverviewAsync(_loadCts.Token);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _loadCts?.Cancel();
            _loadCts = null;
        }

        private async Task FetchOverviewAsync(CancellationToken token)
        {
            try
            {
                LoadingRing.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;
                ContentGrid.Visibility = Visibility.Collapsed;

                var users = await ApiService.GetUsersAsync();
                if (token.IsCancellationRequested) return;

                var now = DateTime.Now;
                var dateStr = now.ToString("yyyy-MM-dd");

                var logResponses = await FetchLoginLogsAsync(users, dateStr, token);
                if (token.IsCancellationRequested) return;

                var computed = await Task.Run(() => ComputeOverview(users, logResponses), token);
                if (token.IsCancellationRequested) return;

                ActiveTotalText.Text = $"{computed.Active} / {computed.Total}";
                ActivityProgress.Value = computed.Progress;
                OnBreakText.Text = computed.OnBreak.ToString();
                MediumBreakList.ItemsSource = computed.MediumBreakEmployees.Count > 0
                    ? computed.MediumBreakEmployees
                    : new List<string> { "No employees found." };
                LongBreakList.ItemsSource = computed.LongBreakEmployees.Count > 0
                    ? computed.LongBreakEmployees
                    : new List<string> { "No employees found." };

                LoadingRing.Visibility = Visibility.Collapsed;
                LoadingRing.IsActive = false;
                ContentGrid.Visibility = Visibility.Visible;
            }
            catch
            {
                LoadingRing.Visibility = Visibility.Collapsed;
                LoadingRing.IsActive = false;
                ContentGrid.Visibility = Visibility.Visible;
                ActiveTotalText.Text = "0 / 0";
                OnBreakText.Text = "0";
                MediumBreakList.ItemsSource = new List<string> { "No employees found." };
                LongBreakList.ItemsSource = new List<string> { "No employees found." };
            }
        }

        private void BreakEmployee_Click(object sender, ItemClickEventArgs e)
        {
            // Optional: open detail dialog like React
        }

        private static async Task<List<LoginLogEntry>[]> FetchLoginLogsAsync(
            List<EmployeeItem> users,
            string dateStr,
            CancellationToken token)
        {
            var results = new List<LoginLogEntry>[users.Count];
            if (users.Count == 0) return results;

            using var semaphore = new SemaphoreSlim(MaxLogFetchConcurrency);
            var tasks = new Task[users.Count];

            for (int i = 0; i < users.Count; i++)
            {
                var index = i;
                var userId = users[i].Id;
                tasks[i] = FetchLoginLogsForUserAsync(index, userId, dateStr, results, semaphore, token);
            }

            await Task.WhenAll(tasks);
            return results;
        }

        private static async Task FetchLoginLogsForUserAsync(
            int index,
            string userId,
            string dateStr,
            List<LoginLogEntry>[] results,
            SemaphoreSlim semaphore,
            CancellationToken token)
        {
            await semaphore.WaitAsync(token);
            try
            {
                results[index] = await ApiService.GetLoginLogAsync(userId, dateStr);
            }
            catch
            {
                results[index] = new List<LoginLogEntry>();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static OverviewResult ComputeOverview(List<EmployeeItem> users, List<LoginLogEntry>[] logResponses)
        {
            int active = 0;
            var mediumBreakEmployees = new List<string>();
            var longBreakEmployees = new List<string>();

            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                if (string.Equals(user.Login, "true", StringComparison.OrdinalIgnoreCase))
                {
                    active++;
                }

                var logs = i < logResponses.Length ? logResponses[i] : null;
                if (logs == null || logs.Count < 2) continue;

                bool hasMedium = false, hasLong = false;

                for (int j = 0; j < logs.Count - 1; j++)
                {
                    var logoutStr = logs[j].Logout;
                    var loginStr = logs[j + 1].Login;
                    if (string.IsNullOrEmpty(logoutStr) || string.IsNullOrEmpty(loginStr)) continue;
                    if (!TryParseLogTime(logoutStr, out var logout) || !TryParseLogTime(loginStr, out var login))
                        continue;

                    var breakMs = (long)(login - logout).TotalMilliseconds;
                    if (breakMs >= MinMediumBreakMs && breakMs <= MaxMediumBreakMs)
                        hasMedium = true;
                    else if (breakMs > MaxMediumBreakMs)
                        hasLong = true;

                    if (hasMedium && hasLong) break;
                }

                var name = string.IsNullOrWhiteSpace(user.StaffName) ? user.Id : user.StaffName;
                if (hasMedium) mediumBreakEmployees.Add(name);
                if (hasLong) longBreakEmployees.Add(name);
            }

            int total = users.Count;
            int onBreak = total - active;
            double progress = total > 0 ? (active * 100.0 / total) : 0;

            return new OverviewResult
            {
                Active = active,
                Total = total,
                OnBreak = onBreak,
                Progress = progress,
                MediumBreakEmployees = mediumBreakEmployees,
                LongBreakEmployees = longBreakEmployees
            };
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

        private sealed class OverviewResult
        {
            public int Active { get; init; }
            public int Total { get; init; }
            public int OnBreak { get; init; }
            public double Progress { get; init; }
            public List<string> MediumBreakEmployees { get; init; } = new();
            public List<string> LongBreakEmployees { get; init; } = new();
        }
    }
}
