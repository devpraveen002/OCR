namespace PDFOCRProcessor.Core.Models;

public class DocumentField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public float Confidence { get; set; }
}
