using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EmployeeDesktop.Models;
using EmployeeDesktop.Services;

namespace EmployeeDesktop.Views;

public sealed partial class SignupPage : Page
{
    public SignupPage()
    {
        InitializeComponent();
    }

    private async void SignupButton_Click(object sender, RoutedEventArgs e)
    {
        var email = (EmailBox.Text ?? "").Trim();
        var password = PasswordBox.Password ?? "";
        var name = (NameBox.Text ?? "").Trim();

        ErrorText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
        {
            ErrorText.Text = "Email, password, and name are required.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        SignupButton.IsEnabled = false;
        ProgressRing.IsActive = true;
        ProgressRing.Visibility = Visibility.Visible;

        try
        {
            var response = await ApiService.SignupAsync(
                email,
                password,
                name,
                (PhoneBox.Text ?? "").Trim().Length > 0 ? PhoneBox.Text?.Trim() : null,
                (StaffIdBox.Text ?? "").Trim().Length > 0 ? StaffIdBox.Text?.Trim() : null);

            if (response.Status && response.Data != null)
            {
                AuthService.SetUser(response.Data, response.Data.Token);
                Frame?.Navigate(typeof(MainPage));
                return;
            }

            ErrorText.Text = response.Message ?? "Sign up failed.";
            ErrorText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            SignupButton.IsEnabled = true;
            ProgressRing.IsActive = false;
            ProgressRing.Visibility = Visibility.Collapsed;
        }
    }

    private void LoginLink_Click(object sender, RoutedEventArgs e)
    {
        Frame?.Navigate(typeof(LoginPage));
    }
}
