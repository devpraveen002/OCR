using Microsoft.AspNetCore.Mvc;
using PDFOCRProcessor.Core.Interfaces;

namespace PDFOCRProcessor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentProcessorService _documentProcessorService;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentFormatter _documentFormatter;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentProcessorService documentProcessorService,
            IDocumentRepository documentRepository,
            IDocumentFormatter documentFormatter,
            ILogger<DocumentsController> logger)
        {
            _documentProcessorService = documentProcessorService ?? throw new ArgumentNullException(nameof(documentProcessorService));
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _documentFormatter = documentFormatter ?? throw new ArgumentNullException(nameof(documentFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded");
            }

            // Validate file type
            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only PDF files are supported");
            }

            try
            {
                _logger.LogInformation("Processing uploaded file: {FileName}, Size: {FileSize} bytes",
                    file.FileName, file.Length);

                using (var stream = file.OpenReadStream())
                {
                    var result = await _documentProcessorService.ProcessDocumentAsync(stream, file.FileName);

                    if (!result.IsSuccessful)
                    {
                        _logger.LogWarning("Document processing failed: {FileName}, Error: {ErrorMessage}",
                            file.FileName, result.ErrorMessage);

                        return BadRequest(new
                        {
                            success = false,
                            error = result.ErrorMessage
                        });
                    }

                    _logger.LogInformation("Document processed successfully: {FileName}, DocumentId: {DocumentId}",
                        file.FileName, result.DocumentId);

                    return Ok(new
                    {
                        success = true,
                        documentId = result.DocumentId,
                        fileName = result.FileName,
                        documentType = result.DocumentType,
                        processingTimeMs = result.ProcessingTimeMs
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded file: {FileName}", file.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while processing the document"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _documentRepository.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while retrieving documents"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            try
            {
                var document = await _documentRepository.GetDocumentAsync(id);

                if (document == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    fileName = document.FileName,
                    documentType = document.DocumentType,
                    processedDate = document.ProcessedDate,
                    isSuccessful = document.IsSuccessful,
                    fields = document.Fields.Select(f => new
                    {
                        name = f.Name,
                        value = f.Value,
                        confidence = f.Confidence
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while retrieving the document"
                });
            }
        }

        [HttpGet("{id}/json")]
        public async Task<IActionResult> GetDocumentAsJson(int id)
        {
            try
            {
                var document = await _documentRepository.GetDocumentAsync(id);

                if (document == null)
                {
                    return NotFound();
                }

                string json = _documentFormatter.FormatAsJson(document);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document as JSON: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while retrieving the document"
                });
            }
        }

        [HttpGet("{id}/csv")]
        public async Task<IActionResult> GetDocumentAsCsv(int id)
        {
            try
            {
                var document = await _documentRepository.GetDocumentAsync(id);

                if (document == null)
                {
                    return NotFound();
                }

                string csv = _documentFormatter.FormatAsCsv(document);
                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{document.FileName}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document as CSV: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while retrieving the document"
                });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] string query)
        {
            try
            {
                var documents = await _documentRepository.SearchDocumentsAsync(query);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents: {Query}", query);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while searching documents"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                bool result = await _documentRepository.DeleteDocumentAsync(id);

                if (!result)
                {
                    return NotFound();
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while deleting the document"
                });
            }
        }
    }
}