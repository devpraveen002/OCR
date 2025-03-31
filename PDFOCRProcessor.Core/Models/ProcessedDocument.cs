namespace PDFOCRProcessor.Core.Models;

public class ProcessedDocument
{
    public string FileName { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime ProcessedDate { get; set; }
    public string DocumentType { get; set; }
    public string NormalizedText { get; set; }
    public List<DocumentField> Fields { get; set; } = new List<DocumentField>();
}
