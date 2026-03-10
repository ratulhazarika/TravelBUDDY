using System.Text.Json.Serialization;

namespace EmployeeWeb.Desktop.Models
{
    /// <summary>
    /// Employee list item matching React landing page state (id, staffName, role, staffID, profilePicture).
    /// </summary>
    public class EmployeeItem
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("staffName")]
        public string StaffName { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("staffID")]
        public string StaffID { get; set; } = string.Empty;

        /// <summary>
        /// Online status string from api/user (\"true\" / \"false\").
        /// </summary>
        [JsonPropertyName("login")]
        public string Login { get; set; } = "false";

        /// <summary>
        /// Base64 or URL from api/dp/{id} - not from api/user.
        /// </summary>
        [JsonIgnore]
        public string? ProfilePicture { get; set; }
    }
}
