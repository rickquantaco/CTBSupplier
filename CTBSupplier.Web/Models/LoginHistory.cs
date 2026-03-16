using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("LoginHistory")]
public class LoginHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LoginHistoryId { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Login Name")]
    public string LoginName { get; set; } = string.Empty;

    [Display(Name = "Attempted At")]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Success")]
    public bool IsSuccess { get; set; }

    [MaxLength(50)]
    [Display(Name = "IP Address")]
    public string? IpAddress { get; set; }
}
