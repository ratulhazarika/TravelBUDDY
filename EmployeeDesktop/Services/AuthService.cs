using System.Text.Json;
using EmployeeDesktop.Models;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace EmployeeDesktop.Services;

/// <summary>
/// Stores and loads user + token from Windows local settings (like localStorage).
/// </summary>
public static class AuthService
{
    private const string KeyUser = "EmployeeUser";
    private const string KeyToken = "EmployeeToken";

    public static UserInfo? CurrentUser { get; private set; }
    public static string? Token { get; private set; }
    public static bool IsLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(Token);

    private static IPropertySet? TryGetSettings()
    {
        try
        {
            return ApplicationData.Current?.LocalSettings?.Values;
        }
        catch
        {
            return null;
        }
    }

    public static void SetUser(UserInfo user, string? token = null)
    {
        CurrentUser = user;
        Token = token ?? user.Token;

        var settings = TryGetSettings();
        if (settings != null)
        {
            settings[KeyUser] = JsonSerializer.Serialize(user);
            settings[KeyToken] = Token ?? "";
        }

        if (!string.IsNullOrEmpty(Token))
            ApiService.SetToken(Token);
    }

    public static void ClearUser()
    {
        CurrentUser = null;
        Token = null;
        ApiService.SetToken(null);

        var settings = TryGetSettings();
        if (settings != null)
        {
            settings.Remove(KeyUser);
            settings.Remove(KeyToken);
        }
    }

    /// <summary>
    /// Restore session from local settings. Call on app start.
    /// </summary>
    public static void LoadSavedSession()
    {
        var settings = TryGetSettings();
        if (settings == null) return;

        if (settings.TryGetValue(KeyUser, out object? saved) && saved is string json)
        {
            try
            {
                var user = JsonSerializer.Deserialize<UserInfo>(json);
                Token = settings.TryGetValue(KeyToken, out object? t) ? t as string : user?.Token;
                if (user != null && !string.IsNullOrEmpty(Token))
                {
                    CurrentUser = user;
                    ApiService.SetToken(Token);
                    return;
                }
            }
            catch { }
        }

        CurrentUser = null;
        Token = null;
    }
}
