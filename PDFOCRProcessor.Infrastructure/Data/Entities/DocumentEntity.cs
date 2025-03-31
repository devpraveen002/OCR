using System.ComponentModel.DataAnnotations;

namespace PDFOCRProcessor.Infrastructure.Data.Entities;

public class DocumentEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; }

    [MaxLength(50)]
    public string DocumentType { get; set; }

    [Required]
    public DateTime UploadDate { get; set; }

    public DateTime ProcessedDate { get; set; }

    [MaxLength(255)]
    public string StoragePath { get; set; }

    public bool IsProcessed { get; set; }

    [MaxLength(500)]
    public string ErrorMessage { get; set; }

    // Navigation properties
    public virtual ICollection<DocumentFieldEntity> Fields { get; set; } = new List<DocumentFieldEntity>();
}
