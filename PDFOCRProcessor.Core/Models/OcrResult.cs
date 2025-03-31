namespace PDFOCRProcessor.Core.Models;

public class OcrResult
{
    public string FileName { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime ProcessedDate { get; set; }
    public string RawText { get; set; }
    public List<TextBlock> TextBlocks { get; set; } = new List<TextBlock>();
    public DocumentMetadata Metadata { get; set; } = new DocumentMetadata();
}
