using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("ApiKey")]
public class ApiKey
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ApiKeyId { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Key Name")]
    public string KeyName { get; set; } = string.Empty;

    /// <summary>SHA-256 hex hash of the actual key — never store or display plaintext.</summary>
    [Required]
    [MaxLength(128)]
    public string KeyHash { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
