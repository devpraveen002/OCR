using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Core.Interfaces
{
    public interface ITextProcessor
    {
        ProcessedDocument ProcessText(OcrResult ocrResult);
    }
}