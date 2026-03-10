using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace EmployeeWeb.Desktop.Services
{
    public static class AuthService
    {
        // In-memory: who is logged in right now
        public static UserInfo? CurrentUser { get; private set; }
        public static string? ProfilePictureBase64 { get; private set; }

        // Keys for saving in Windows settings (like localStorage keys)
        const string KeyUser = "SavedUser";
        const string KeyDp = "SavedDp";

        public static bool IsLoggedIn => CurrentUser != null;

        // Safe accessor: returns null if Windows LocalSettings isn't available
        private static global::Windows.Foundation.Collections.IPropertySet? TryGetLocalValues()
        {
            try
            {
                return global::Windows.Storage.ApplicationData.Current?.LocalSettings?.Values;
            }
            catch
            {
                // Accessing ApplicationData may throw in some unpackaged scenarios; treat as unavailable.
                return null;
            }
        }

        /// <summary>
        /// Call this when login succeeds. Saves user to memory and to disk.
        /// </summary>
        public static void SetUser(UserInfo user, string? profilePictureBase64 = null)
        {
            CurrentUser = user;
            ProfilePictureBase64 = profilePictureBase64;

            var values = TryGetLocalValues();
            if (values == null) return; // fallback: no persistent store available

            values[KeyUser] = System.Text.Json.JsonSerializer.Serialize(user);
            values[KeyDp] = profilePictureBase64 ?? string.Empty;
        }

        /// <summary>
        /// Call this on logout. Clears user from memory and disk.
        /// </summary>
        public static void ClearUser()
        {
            CurrentUser = null;
            ProfilePictureBase64 = null;

            var values = TryGetLocalValues();
            if (values == null) return;

            values.Remove(KeyUser);
            values.Remove(KeyDp);
        }

        /// <summary>
        /// Call this once when the app starts (e.g. in App.xaml.cs). Restores user from disk.
        /// </summary>
        public static void LoadSavedSession()
        {
            var values = TryGetLocalValues();
            if (values == null)
            {
                CurrentUser = null;
                ProfilePictureBase64 = null;
                return;
            }

            if (values.TryGetValue(KeyUser, out object? saved) && saved is string json)
            {
                try
                {
                    CurrentUser = System.Text.Json.JsonSerializer.Deserialize<UserInfo>(json);
                    ProfilePictureBase64 = values.TryGetValue(KeyDp, out object? dp) ? dp as string : null;
                }
                catch
                {
                    CurrentUser = null;
                    ProfilePictureBase64 = null;
                }
            }
            else
            {
                CurrentUser = null;
                ProfilePictureBase64 = null;
            }
        }
    }

    // User model matching your API (api/authenticate/admin returns _id, staffName, role, staffID, etc.)
    public class UserInfo
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string StaffID { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;   // e.g. "Hr and Administration"
        public string Email { get; set; } = string.Empty;  // staffEmail for login

        public UserInfo() { }
    }
}