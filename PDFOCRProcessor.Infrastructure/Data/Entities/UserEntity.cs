using System.ComponentModel.DataAnnotations;

namespace PDFOCRProcessor.Infrastructure.Data.Entities;

public class UserEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
}
