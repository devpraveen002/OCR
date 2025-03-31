namespace PDFOCRProcessor.Core.Models;

public class TextBlock
{
    public string Id { get; set; }
    public string Text { get; set; }
    public float Confidence { get; set; }
    public string Type { get; set; }
    public int Page { get; set; }
    public BoundingBox BoundingBox { get; set; }
}
