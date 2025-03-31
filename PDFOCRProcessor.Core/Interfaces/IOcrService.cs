using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Core.Interfaces
{
    public interface IOcrService
    {
        Task<OcrResult> ProcessPdfAsync(Stream pdfStream, string fileName);
    }
}