using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeWeb.Api.Models.Entities;

/// <summary>
/// Employee/user entity stored in the database. Used for both HR users and regular employees.
/// </summary>
[Table("Employees")]
public class EmployeeEntity
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string StaffID { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string StaffName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string StaffEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(32)]
    public string StaffPhone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Role { get; set; } = "Employee";

    /// <summary>Currently online or not. "true" / "false"</summary>
    [MaxLength(16)]
    public string Login { get; set; } = "false";

    [MaxLength(32)]
    public string Dob { get; set; } = "1990-01-01";

    [MaxLength(32)]
    public string Doj { get; set; } = "2020-01-01";

    [MaxLength(1024)]
    public string? ProfilePicture { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<LoginLogEntity> LoginLogs { get; set; } = new List<LoginLogEntity>();
}
