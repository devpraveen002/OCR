using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDFOCRProcessor.Core.Interfaces;
using PDFOCRProcessor.Core.Models;
using PDFOCRProcessor.Infrastructure.Data;
using PDFOCRProcessor.Infrastructure.Data.Entities;

namespace PDFOCRProcessor.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(ApplicationDbContext context, ILogger<DocumentRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> SaveDocumentAsync(ProcessedDocument document)
        {
            try
            {
                // Create document entity
                var documentEntity = new DocumentEntity
                {
                    FileName = document.FileName,
                    DocumentType = document.DocumentType,
                    UploadDate = DateTime.UtcNow,
                    ProcessedDate = document.ProcessedDate,
                    IsProcessed = document.IsSuccessful,

                    //uncomment when using aws
                    //ErrorMessage = document.ErrorMessage,

                    //remove the below line if using aws
                    ErrorMessage = document.ErrorMessage ?? string.Empty,

                    //remove the below line if using aws
                    StoragePath = document.FileName
                };

                // Add fields
                if (document.IsSuccessful && document.Fields != null)
                {
                    foreach (var field in document.Fields)
                    {
                        documentEntity.Fields.Add(new DocumentFieldEntity
                        {
                            Name = field.Name,
                            Value = field.Value,
                            Confidence = field.Confidence
                        });
                    }
                }

                // Add to database
                _context.Documents.Add(documentEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved document to database: {FileName}, Id: {Id}", document.FileName, documentEntity.Id);

                return documentEntity.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving document to database: {FileName}", document.FileName);
                throw;
            }
        }

        public async Task<ProcessedDocument> GetDocumentAsync(int id)
        {
            try
            {
                // Get document with fields
                var documentEntity = await _context.Documents
                    .Include(d => d.Fields)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (documentEntity == null)
                {
                    _logger.LogWarning("Document not found: {Id}", id);
                    return null;
                }

                // Map to domain model
                var document = new ProcessedDocument
                {
                    FileName = documentEntity.FileName,
                    IsSuccessful = documentEntity.IsProcessed,
                    ErrorMessage = documentEntity.ErrorMessage,
                    ProcessedDate = documentEntity.ProcessedDate,
                    DocumentType = documentEntity.DocumentType
                };

                // Map fields
                if (documentEntity.Fields != null)
                {
                    foreach (var field in documentEntity.Fields)
                    {
                        document.Fields.Add(new DocumentField
                        {
                            Name = field.Name,
                            Value = field.Value,
                            Confidence = field.Confidence
                        });
                    }
                }

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document from database: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DocumentSummary>> GetAllDocumentsAsync()
        {
            try
            {
                // Get all documents (excluding fields for performance)
                var documents = await _context.Documents
                    .Select(d => new DocumentSummary
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        DocumentType = d.DocumentType,
                        UploadDate = d.UploadDate,
                        ProcessedDate = d.ProcessedDate,
                        IsProcessed = d.IsProcessed
                    })
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents from database");
                throw;
            }
        }

        public async Task<IEnumerable<DocumentSummary>> SearchDocumentsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllDocumentsAsync();
                }

                // Search by filename or document type
                var documents = await _context.Documents
                    .Where(d => d.FileName.Contains(searchTerm) ||
                               d.DocumentType.Contains(searchTerm) ||
                               d.Fields.Any(f => f.Value.Contains(searchTerm)))
                    .Select(d => new DocumentSummary
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        DocumentType = d.DocumentType,
                        UploadDate = d.UploadDate,
                        ProcessedDate = d.ProcessedDate,
                        IsProcessed = d.IsProcessed
                    })
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents in database: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                // Find document
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Document not found for deletion: {Id}", id);
                    return false;
                }

                // Remove document
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted document from database: {Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document from database: {Id}", id);
                throw;
            }
        }
    }
}