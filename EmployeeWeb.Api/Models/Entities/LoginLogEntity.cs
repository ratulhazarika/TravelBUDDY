using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeWeb.Api.Models.Entities;

/// <summary>
/// Login/logout session record for an employee on a given date.
/// </summary>
[Table("LoginLogs")]
public class LoginLogEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD

    [Required]
    public DateTime LoginTime { get; set; }

    public DateTime? LogoutTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public virtual EmployeeEntity? Employee { get; set; }
}
