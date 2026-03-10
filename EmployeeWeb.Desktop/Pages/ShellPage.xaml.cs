using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using EmployeeWeb.Desktop.Models;
using EmployeeWeb.Desktop.Services;

namespace EmployeeWeb.Desktop.Pages
{
    public sealed partial class ShellPage : Page
    {
        private const int MaxProfilePictureConcurrency = 4;

        // canAnyOneAccess: true = all roles, false = HR only (matches React navigationItems)
        private readonly List<(string Tag, string Label, bool CanAnyOneAccess)> _navItems = new()
        {
            ("Dashboard", "Dashboard", true),
            ("Employees", "Employees", true),
            ("Departments", "Departments", true),
            ("Employee Table", "Employee Table", true),
            ("Add Employee", "Add Employee", false),
            ("Remove Employee", "Remove Employee", false),
            ("Tickets", "Tickets", false),
            ("Delete Old Data", "Delete Old Data", false),
        };

        private List<EmployeeItem> _allEmployees = new();
        private EmployeeItem? _selectedEmployee;
        private bool _isHr;
        private CancellationTokenSource? _employeeLoadCts;

        public ShellPage()
        {
            InitializeComponent();
            LoadUser();
            BuildNavMenu();
            ContentFrame.Navigate(typeof(DashboardPage));
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _employeeLoadCts?.Cancel();
            _employeeLoadCts = new CancellationTokenSource();
            _ = LoadEmployeesAsync(_employeeLoadCts.Token);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _employeeLoadCts?.Cancel();
            _employeeLoadCts = null;
        }

        private void LoadUser()
        {
            var user = AuthService.CurrentUser;
            if (user == null) return;
            WelcomeText.Text = "Welcome, " + (user.StaffName ?? "User");
            RoleText.Text = (user.Role ?? "").ToUpperInvariant() + " DASHBOARD";
            _isHr = string.Equals(user.Role, "Hr and Administration", StringComparison.OrdinalIgnoreCase);
            UserPicture.DisplayName = user.StaffName;
            // Optionally set profile picture from AuthService.ProfilePictureBase64
        }

        private void BuildNavMenu()
        {
            NavMenu.Items.Clear();
            foreach (var (tag, label, canAnyOneAccess) in _navItems)
            {
                if (canAnyOneAccess || _isHr)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                    panel.Children.Add(new SymbolIcon { Symbol = GetSymbol(tag) });
                    panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
                    var item = new ListViewItem { Content = panel, Tag = tag };
                    NavMenu.Items.Add(item);
                }
            }
            if (NavMenu.Items.Count > 0)
                NavMenu.SelectedIndex = 0;
        }

        private static Symbol GetSymbol(string tag)
        {
            return tag switch
            {
                "Dashboard" => Symbol.ShowResults,
                "Employees" => Symbol.People,
                "Departments" => Symbol.Contact2,
                "Employee Table" => Symbol.List,
                "Add Employee" => Symbol.Add,
                "Remove Employee" => Symbol.Delete,
                "Tickets" => Symbol.Help,
                "Delete Old Data" => Symbol.Delete,
                _ => Symbol.Document
            };
        }

        private async Task LoadEmployeesAsync(CancellationToken token)
        {
            try
            {
                var employees = await ApiService.GetUsersAsync();
                if (token.IsCancellationRequested) return;

                _allEmployees = employees;

                await FetchProfilePicturesAsync(employees, token);
            }
            catch { /* ignore */ }
        }

        private static async Task FetchProfilePicturesAsync(List<EmployeeItem> employees, CancellationToken token)
        {
            if (employees.Count == 0) return;

            using var semaphore = new SemaphoreSlim(MaxProfilePictureConcurrency);
            var tasks = new List<Task>(employees.Count);

            foreach (var emp in employees)
            {
                if (string.IsNullOrWhiteSpace(emp.Id)) continue;
                tasks.Add(FetchProfilePictureAsync(emp, semaphore, token));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task FetchProfilePictureAsync(EmployeeItem employee, SemaphoreSlim semaphore, CancellationToken token)
        {
            await semaphore.WaitAsync(token);
            try
            {
                employee.ProfilePicture = await ApiService.GetProfilePictureAsync(employee.Id);
            }
            catch { /* ignore */ }
            finally
            {
                semaphore.Release();
            }
        }

        private void NavMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavMenu.SelectedItem is not ListViewItem item || item.Tag is not string tag) return;
            switch (tag)
            {
                case "Dashboard":
                    ContentFrame.Navigate(typeof(DashboardPage));
                    break;
                case "Employees":
                    ContentFrame.Navigate(typeof(EmployeesPage), _selectedEmployee?.Id);
                    break;
                case "Departments":
                    ContentFrame.Navigate(typeof(DepartmentsPage));
                    break;
                case "Employee Table":
                    ContentFrame.Navigate(typeof(EmployeeTablePage));
                    break;
                case "Add Employee":
                    ContentFrame.Navigate(typeof(AddEmployeePage));
                    break;
                case "Remove Employee":
                    ContentFrame.Navigate(typeof(RemoveEmployeePage));
                    break;
                case "Tickets":
                    ContentFrame.Navigate(typeof(TicketsPage));
                    break;
                case "Delete Old Data":
                    ContentFrame.Navigate(typeof(DeleteOldDataPage));
                    break;
            }
        }

        private void EmployeeSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            var q = (sender.Text ?? "").Trim().ToLowerInvariant();
            if (q.Length < 2)
            {
                sender.ItemsSource = null;
                return;
            }
            var list = _allEmployees
                .Where(emp => (emp.StaffName ?? "").ToLowerInvariant().Contains(q))
                .Take(50)
                .ToList();
            sender.ItemsSource = list;
        }

        private void EmployeeSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is EmployeeItem emp)
                sender.Text = emp.StaffName ?? "";
        }

        private void EmployeeSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is EmployeeItem emp)
            {
                _selectedEmployee = emp;
                ContentFrame.Navigate(typeof(EmployeesPage), emp.Id);
                // Select Employees in nav
                for (int i = 0; i < NavMenu.Items.Count; i++)
                {
                    if (NavMenu.Items[i] is ListViewItem li && li.Tag is string t && t == "Employees")
                    {
                        NavMenu.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void WelcomeButton_Click(object sender, RoutedEventArgs e)
        {
            var user = AuthService.CurrentUser;
            if (user == null) return;
            _selectedEmployee = new EmployeeItem
            {
                Id = user.Id,
                StaffName = user.StaffName ?? "",
                Role = user.Role ?? "",
                StaffID = user.StaffID ?? ""
            };
            ContentFrame.Navigate(typeof(EmployeesPage), user.Id);
            for (int i = 0; i < NavMenu.Items.Count; i++)
            {
                if (NavMenu.Items[i] is ListViewItem li && li.Tag is string t && t == "Employees")
                {
                    NavMenu.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var flyout = new MenuFlyout();
            var logout = new MenuFlyoutItem { Text = "Logout", Icon = new SymbolIcon(Symbol.Remove) };
            logout.Click += (_, _) =>
            {
                AuthService.ClearUser();
                NavigationService.NavigateToLogin();
            };
            flyout.Items.Add(logout);
            flyout.ShowAt(sender as FrameworkElement);
        }
    }
}
