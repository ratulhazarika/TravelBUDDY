using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EmployeeWeb.Desktop.Services;

namespace EmployeeWeb.Desktop.Pages
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text?.Trim() ?? "";
            var password = PasswordBox.Password ?? "";

            ErrorText.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Please enter email and password.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            SignInButton.IsEnabled = false;
            LoginProgress.Visibility = Visibility.Visible;
            LoginProgress.IsActive = true;

            try
            {
                var (success, user, message, issueWith) = await ApiService.LoginAsync(email, password);

                if (success && user != null)
                {
                    user.Email = email;
                    var dp = await ApiService.GetProfilePictureAsync(user.Id);
                    AuthService.SetUser(user, dp);
                    NavigationService.NavigateToShell();
                    return;
                }

                if (issueWith == "email")
                {
                    ErrorText.Text = message ?? "Invalid email.";
                }
                else
                {
                    ErrorText.Text = message ?? "Invalid password.";
                }
                ErrorText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                SignInButton.IsEnabled = true;
                LoginProgress.Visibility = Visibility.Collapsed;
                LoginProgress.IsActive = false;
            }
        }

        private async void DownloadWindows_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://s3.ap-south-1.amazonaws.com/travel.buddy.desktop.app/TravelBuddy.exe");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
