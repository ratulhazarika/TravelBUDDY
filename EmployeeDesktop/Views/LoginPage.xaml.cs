using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EmployeeDesktop.Services;

namespace EmployeeDesktop.Views;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var email = (EmailBox.Text ?? "").Trim();
        var password = PasswordBox.Password ?? "";

        ErrorText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "Email and password are required.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        LoginButton.IsEnabled = false;
        ProgressRing.IsActive = true;
        ProgressRing.Visibility = Visibility.Visible;

        try
        {
            var response = await ApiService.LoginAsync(email, password);

            if (response.Status && response.Data != null)
            {
                AuthService.SetUser(response.Data, response.Data.Token);

                // Record session start
                _ = ApiService.RecordActivityLoginAsync(response.Data.Id);

                Frame?.Navigate(typeof(MainPage));
                return;
            }

            ErrorText.Text = response.Message ?? "Login failed.";
            ErrorText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            ProgressRing.IsActive = false;
            ProgressRing.Visibility = Visibility.Collapsed;
        }
    }

    private void SignupLink_Click(object sender, RoutedEventArgs e)
    {
        Frame?.Navigate(typeof(SignupPage));
    }
}
