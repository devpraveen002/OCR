using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Core.Interfaces
{
    public interface IDocumentRepository
    {
        Task<int> SaveDocumentAsync(ProcessedDocument document);
        Task<ProcessedDocument> GetDocumentAsync(int id);
        Task<IEnumerable<DocumentSummary>> GetAllDocumentsAsync();
        Task<IEnumerable<DocumentSummary>> SearchDocumentsAsync(string searchTerm);
        Task<bool> DeleteDocumentAsync(int id);
    }

    public class DocumentSummary
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public System.DateTime UploadDate { get; set; }
        public System.DateTime ProcessedDate { get; set; }
        public bool IsProcessed { get; set; }
    }
}