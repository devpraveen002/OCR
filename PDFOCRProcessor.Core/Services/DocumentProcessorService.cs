using Microsoft.Extensions.Logging;
using PDFOCRProcessor.Core.Interfaces;

namespace PDFOCRProcessor.Core.Services
{
    public class DocumentProcessorService : IDocumentProcessorService
    {
        private readonly IOcrService _ocrService;
        private readonly ITextProcessor _textProcessor;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentFormatter _documentFormatter;
        private readonly ILogger<DocumentProcessorService> _logger;

        public DocumentProcessorService(
            IOcrService ocrService,
            ITextProcessor textProcessor,
            IDocumentRepository documentRepository,
            IDocumentFormatter documentFormatter,
            ILogger<DocumentProcessorService> logger)
        {
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _textProcessor = textProcessor ?? throw new ArgumentNullException(nameof(textProcessor));
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _documentFormatter = documentFormatter ?? throw new ArgumentNullException(nameof(documentFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProcessingResult> ProcessDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                // Start processing
                _logger.LogInformation("Starting document processing for {FileName}", fileName);
                var startTime = DateTime.UtcNow;

                // Step 1: OCR Processing
                _logger.LogInformation("Step 1: OCR processing for {FileName}", fileName);
                var ocrResult = await _ocrService.ProcessPdfAsync(documentStream, fileName);

                if (!ocrResult.IsSuccessful)
                {
                    _logger.LogWarning("OCR processing failed for {FileName}: {ErrorMessage}",
                        fileName, ocrResult.ErrorMessage);

                    return new ProcessingResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = $"OCR processing failed: {ocrResult.ErrorMessage}",
                        FileName = fileName
                    };
                }

                // Step 2: Text processing
                _logger.LogInformation("Step 2: Text processing for {FileName}", fileName);
                var processedDocument = _textProcessor.ProcessText(ocrResult);

                // Step 3: Save to database
                _logger.LogInformation("Step 3: Saving to database for {FileName}", fileName);
                int documentId = await _documentRepository.SaveDocumentAsync(processedDocument);

                // Step 4: Format results
                _logger.LogInformation("Step 4: Formatting results for {FileName}", fileName);
                string jsonResult = _documentFormatter.FormatAsJson(processedDocument);

                // Complete processing
                var endTime = DateTime.UtcNow;
                var processingTime = endTime - startTime;

                _logger.LogInformation(
                    "Document processing completed for {FileName}. Time taken: {ProcessingTime}ms",
                    fileName, processingTime.TotalMilliseconds);

                return new ProcessingResult
                {
                    IsSuccessful = true,
                    DocumentId = documentId,
                    FileName = fileName,
                    DocumentType = processedDocument.DocumentType,
                    JsonData = jsonResult,
                    ProcessingTimeMs = processingTime.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {FileName}", fileName);

                return new ProcessingResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Document processing failed: {ex.Message}",
                    FileName = fileName
                };
            }
        }
    }
}