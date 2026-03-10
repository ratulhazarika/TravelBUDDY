using Microsoft.UI.Xaml.Navigation;
using EmployeeDesktop.Services;
using EmployeeDesktop.Views;

namespace EmployeeDesktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();
            window.Title = "Employee Desktop - Travelogy";

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            AuthService.LoadSavedSession();

            if (AuthService.IsLoggedIn)
            {
                var user = AuthService.CurrentUser!;
                _ = ApiService.RecordActivityLoginAsync(user.Id);
                _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            else
            {
                _ = rootFrame.Navigate(typeof(LoginPage), e.Arguments);
            }

            window.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
