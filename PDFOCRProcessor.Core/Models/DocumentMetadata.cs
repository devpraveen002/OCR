namespace PDFOCRProcessor.Core.Models;

public class DocumentMetadata
{
    public int PageCount { get; set; }
    public string DocumentType { get; set; }
    public DateTime? ProcessingStartTime { get; set; }
    public DateTime? ProcessingEndTime { get; set; }
    public TimeSpan? ProcessingDuration =>
        ProcessingStartTime.HasValue && ProcessingEndTime.HasValue
            ? ProcessingEndTime.Value - ProcessingStartTime.Value
            : null;
}
