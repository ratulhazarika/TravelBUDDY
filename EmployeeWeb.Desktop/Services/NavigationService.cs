using Microsoft.UI.Xaml.Controls;

namespace EmployeeWeb.Desktop.Services
{
    /// <summary>
    /// Holds the app's root Frame so Login/Shell can navigate. Set from MainWindow.
    /// </summary>
    public static class NavigationService
    {
        public static Frame? RootFrame { get; set; }

        public static void NavigateToLogin()
        {
            RootFrame?.Navigate(typeof(EmployeeWeb.Desktop.Pages.LoginPage));
        }

        public static void NavigateToShell()
        {
            RootFrame?.Navigate(typeof(EmployeeWeb.Desktop.Pages.ShellPage));
        }
    }
}
