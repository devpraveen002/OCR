using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Core.Interfaces
{
    public interface IDocumentFormatter
    {
        string FormatAsJson(ProcessedDocument document);
        string FormatAsCsv(ProcessedDocument document);
    }
}