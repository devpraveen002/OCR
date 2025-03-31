using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PDFOCRProcessor.Infrastructure.Data.Entities;

public class DocumentFieldEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public string Value { get; set; }

    public float Confidence { get; set; }

    // Navigation property
    [ForeignKey("DocumentId")]
    public virtual DocumentEntity Document { get; set; }
}
