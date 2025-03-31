using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.Extensions.Logging;
using PDFOCRProcessor.Core.Interfaces;
using PDFOCRProcessor.Core.Models;
using iText.Kernel.Pdf; // Add this import

namespace PDFOCRProcessor.Infrastructure.Services
{
    public class TextractOcrService : IOcrService
    {
        private readonly AmazonTextractClient _textractClient;
        private readonly ILogger<TextractOcrService> _logger;

        public TextractOcrService(AmazonTextractClient textractClient, ILogger<TextractOcrService> logger)
        {
            _textractClient = textractClient ?? throw new ArgumentNullException(nameof(textractClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //public async Task<OcrResult> ProcessPdfAsync(Stream pdfStream, string fileName)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Starting OCR processing for file: {FileName}", fileName);

        //        // Preprocess the PDF file to ensure it's in a format Textract accepts
        //        using var processedPdfStream = PreprocessPdf(pdfStream);

        //        // Read the PDF file into a byte array
        //        using var memoryStream = new MemoryStream();
        //        await processedPdfStream.CopyToAsync(memoryStream);
        //        byte[] fileBytes = memoryStream.ToArray();

        //        // Create request for Textract
        //        var request = new DetectDocumentTextRequest
        //        {
        //            Document = new Document
        //            {
        //                Bytes = new MemoryStream(fileBytes)
        //            }
        //        };

        //        // Process with Textract
        //        var response = await _textractClient.DetectDocumentTextAsync(request);

        //        // Process the response
        //        var result = new OcrResult
        //        {
        //            FileName = fileName,
        //            IsSuccessful = true,
        //            ProcessedDate = DateTime.UtcNow,
        //            RawText = ExtractRawText(response.Blocks),
        //            TextBlocks = MapTextBlocks(response.Blocks)
        //        };

        //        _logger.LogInformation("Successfully processed file {FileName}, extracted {BlockCount} text blocks",
        //            fileName, result.TextBlocks.Count);

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing PDF with Textract: {FileName}", fileName);
        //        return new OcrResult
        //        {
        //            FileName = fileName,
        //            IsSuccessful = false,
        //            ErrorMessage = $"OCR processing failed: {ex.Message}",
        //            ProcessedDate = DateTime.UtcNow
        //        };
        //    }
        //}
        public async Task<OcrResult> ProcessPdfAsync(Stream pdfStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting OCR processing for file: {FileName}", fileName);

                // Check if it's a multi-page PDF (just for logging)
                bool isMultiPage = false;
                try
                {
                    if (pdfStream.CanSeek)
                        pdfStream.Position = 0;

                    using (var reader = new PdfReader(pdfStream))
                    using (var document = new PdfDocument(reader))
                    {
                        int pageCount = document.GetNumberOfPages();
                        isMultiPage = pageCount > 1;

                        if (isMultiPage)
                        {
                            _logger.LogWarning("Multi-page PDF detected ({PageCount} pages). Note: Only the first page may be processed fully.", pageCount);
                        }
                    }

                    if (pdfStream.CanSeek)
                        pdfStream.Position = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error checking PDF page count: {Error}", ex.Message);
                }

                // Proceed with normal processing - Textract's DetectDocumentText API can handle
                // some multi-page PDFs, but might only process the first page effectively

                // Preprocess the PDF file to ensure it's in a format Textract accepts
                using var processedPdfStream = PreprocessPdf(pdfStream);

                // Read the PDF file into a byte array
                using var memoryStream = new MemoryStream();
                await processedPdfStream.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();

                // Create request for Textract
                var request = new DetectDocumentTextRequest
                {
                    Document = new Document
                    {
                        Bytes = new MemoryStream(fileBytes)
                    }
                };

                // Process with Textract
                var response = await _textractClient.DetectDocumentTextAsync(request);

                // Process the response
                var result = new OcrResult
                {
                    FileName = fileName,
                    IsSuccessful = true,
                    ProcessedDate = DateTime.UtcNow,
                    RawText = ExtractRawText(response.Blocks),
                    TextBlocks = MapTextBlocks(response.Blocks)
                };

                _logger.LogInformation("Successfully processed file {FileName}, extracted {BlockCount} text blocks",
                    fileName, result.TextBlocks.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF with Textract: {FileName}", fileName);

                // For your interview, a simple approach is to return a failed result
                // Later you could add a fallback to mock data if desired
                return new OcrResult
                {
                    FileName = fileName,
                    IsSuccessful = false,
                    ErrorMessage = $"OCR processing failed: {ex.Message}",
                    ProcessedDate = DateTime.UtcNow
                };
            }
        }

        private Stream PreprocessPdf(Stream pdfStream)
        {
            try
            {
                // Reset stream position
                if (pdfStream.CanSeek)
                    pdfStream.Position = 0;

                // Create output stream
                var outputStream = new MemoryStream();

                // Use iText7 to process the PDF
                using (var reader = new PdfReader(pdfStream))
                using (var writer = new PdfWriter(outputStream))
                {
                    using (var srcDocument = new PdfDocument(reader))
                    {
                        // Check if it's multi-page
                        int pageCount = srcDocument.GetNumberOfPages();

                        if (pageCount > 1)
                        {
                            // For multi-page documents, extract just the first page
                            _logger.LogInformation("Extracting only the first page from a {PageCount}-page document", pageCount);

                            using (var destDocument = new PdfDocument(writer))
                            {
                                // Copy just the first page
                                srcDocument.CopyPagesTo(1, 1, destDocument);
                            }
                        }
                        else
                        {
                            // For single-page documents, just rewrite the PDF
                            using (var _ = new PdfDocument(reader, writer))
                            {
                                // The constructor alone copies the content
                            }
                        }
                    }
                }

                outputStream.Position = 0;
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error preprocessing PDF: {Error}", ex.Message);
                // Return original stream if preprocessing fails
                if (pdfStream.CanSeek)
                    pdfStream.Position = 0;
                return pdfStream;
            }
        }

        private string ExtractRawText(List<Block> blocks)
        {
            var textBuilder = new System.Text.StringBuilder();

            foreach (var block in blocks)
            {
                if (block.BlockType == BlockType.LINE)
                {
                    textBuilder.AppendLine(block.Text);
                }
            }

            return textBuilder.ToString();
        }

        private List<TextBlock> MapTextBlocks(List<Block> blocks)
        {
            var textBlocks = new List<TextBlock>();

            foreach (var block in blocks)
            {
                if (block.BlockType == BlockType.LINE || block.BlockType == BlockType.WORD)
                {
                    // Default to page 1 if Page property is 0 or not set
                    int pageNumber = 1;

                    // Check if Page property exists and is greater than 0
                    if (block.Page > 0)
                    {
                        pageNumber = block.Page;
                    }

                    textBlocks.Add(new TextBlock
                    {
                        Id = block.Id,
                        Text = block.Text,
                        Confidence = block.Confidence,
                        Type = block.BlockType.Value,
                        Page = pageNumber,
                        BoundingBox = new Core.Models.BoundingBox
                        {
                            Left = block.Geometry.BoundingBox.Left,
                            Top = block.Geometry.BoundingBox.Top,
                            Width = block.Geometry.BoundingBox.Width,
                            Height = block.Geometry.BoundingBox.Height
                        }
                    });
                }
            }

            return textBlocks;
        }
    }
}