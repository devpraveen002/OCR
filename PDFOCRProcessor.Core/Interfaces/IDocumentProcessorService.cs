namespace PDFOCRProcessor.Core.Interfaces
{
    public interface IDocumentProcessorService
    {
        Task<ProcessingResult> ProcessDocumentAsync(Stream documentStream, string fileName);
    }

    public class ProcessingResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public string JsonData { get; set; }
        public double ProcessingTimeMs { get; set; }
    }
}