using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PDFOCRProcessor.Core.Interfaces;
using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Infrastructure.Services
{
    public class MockOcrService : IOcrService
    {
        private readonly ILogger<MockOcrService> _logger;

        public MockOcrService(ILogger<MockOcrService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OcrResult> ProcessPdfAsync(Stream pdfStream, string fileName)
        {
            _logger.LogInformation("[MOCK] Processing OCR for file: {FileName}", fileName);

            // Try to determine if PDF has multiple pages
            int pageCount = 1;
            try
            {
                if (pdfStream.CanSeek)
                    pdfStream.Position = 0;

                // Check if we can use iText to determine page count
                try
                {
                    using (var reader = new iText.Kernel.Pdf.PdfReader(pdfStream))
                    using (var document = new iText.Kernel.Pdf.PdfDocument(reader))
                    {
                        pageCount = document.GetNumberOfPages();
                        _logger.LogInformation("[MOCK] Detected {PageCount} pages in PDF", pageCount);
                    }
                }
                catch
                {
                    // If we can't determine page count, assume it's one page
                    pageCount = 1;
                }

                if (pdfStream.CanSeek)
                    pdfStream.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[MOCK] Error checking PDF page count: {Error}", ex.Message);
                pageCount = 1;
            }

            // Simulate processing delay
            await Task.Delay(1500);

            // Create mock OCR result
            var result = new OcrResult
            {
                FileName = fileName,
                IsSuccessful = true,
                ProcessedDate = DateTime.UtcNow,
                RawText = GenerateMockText(fileName, pageCount)
            };

            // Add mock text blocks
            result.TextBlocks = GenerateMockTextBlocks(fileName, pageCount);

            _logger.LogInformation("[MOCK] Successfully processed file {FileName}, created {BlockCount} mock text blocks for {PageCount} pages",
                fileName, result.TextBlocks.Count, pageCount);

            return result;
        }

        private List<TextBlock> GenerateMockTextBlocks(string fileName, int pageCount)
        {
            var textBlocks = new List<TextBlock>();

            for (int page = 1; page <= pageCount; page++)
            {
                // Add mocked table data similar to what's in your real PDF
                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_1",
                    Text = "InvoiceNumber InvoiceDate TotalAmount VendorName",
                    Confidence = 0.99f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_2",
                    Text = $"NV-100{page} 2024-03-{30 + page} {1500 + page * 100}.75 ABC.LTD",
                    Confidence = 0.97f,
                    Type = "LINE",
                    Page = page
                });

                // Add some empty space
                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_3",
                    Text = " ",
                    Confidence = 0.99f,
                    Type = "LINE",
                    Page = page
                });

                // Add tabular data (headers)
                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_4",
                    Text = "InvoiceNumber",
                    Confidence = 0.98f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_5",
                    Text = $"NV-100{page}",
                    Confidence = 0.98f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_6",
                    Text = "InvoiceDate",
                    Confidence = 0.98f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_7",
                    Text = $"2024-03-{30 + page}",
                    Confidence = 0.97f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_8",
                    Text = "TotalAmount",
                    Confidence = 0.98f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_9",
                    Text = $"{1500 + page * 100}.75",
                    Confidence = 0.96f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_10",
                    Text = "VendorName",
                    Confidence = 0.98f,
                    Type = "LINE",
                    Page = page
                });

                textBlocks.Add(new TextBlock
                {
                    Id = $"p{page}_11",
                    Text = $"ABC{page}.LTD",
                    Confidence = 0.95f,
                    Type = "LINE",
                    Page = page
                });
            }

            return textBlocks;
        }

        private string GenerateMockText(string fileName, int pageCount)
        {
            StringBuilder mockText = new StringBuilder();

            for (int page = 1; page <= pageCount; page++)
            {
                mockText.AppendLine($"InvoiceNumber InvoiceDate TotalAmount VendorName");
                mockText.AppendLine($"NV-100{page} 2024-03-{30 + page} {1500 + page * 100}.75 ABC{page}.LTD");
                mockText.AppendLine();
                mockText.AppendLine("InvoiceNumber");
                mockText.AppendLine($"NV-100{page}");
                mockText.AppendLine("InvoiceDate");
                mockText.AppendLine($"2024-03-{30 + page}");
                mockText.AppendLine("TotalAmount");
                mockText.AppendLine($"{1500 + page * 100}.75");
                mockText.AppendLine("VendorName");
                mockText.AppendLine($"ABC{page}.LTD");

                // Add page separator if not the last page
                if (page < pageCount)
                {
                    mockText.AppendLine("\n--- Page Break ---\n");
                }
            }

            return mockText.ToString();
        }
    }
}