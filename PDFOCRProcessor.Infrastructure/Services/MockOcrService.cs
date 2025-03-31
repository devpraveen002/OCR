using System;
using System.Collections.Generic;
using System.IO;
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

            // Simulate processing delay
            await Task.Delay(1500);

            // Create mock OCR result
            var result = new OcrResult
            {
                FileName = fileName,
                IsSuccessful = true,
                ProcessedDate = DateTime.UtcNow,
                RawText = GenerateMockText(fileName)
            };

            // Add mock text blocks
            result.TextBlocks = new List<TextBlock>
            {
                new TextBlock { Id = "1", Text = "INVOICE #INV-2023-001", Confidence = 0.98f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "2", Text = "Date: March 31, 2025", Confidence = 0.95f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "3", Text = "Bill To: ABC Corporation", Confidence = 0.97f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "4", Text = "123 Business Street", Confidence = 0.94f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "5", Text = "New York, NY 10001", Confidence = 0.93f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "6", Text = "Item: Software Development Services", Confidence = 0.96f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "7", Text = "Quantity: 160 hours", Confidence = 0.92f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "8", Text = "Rate: $150.00/hour", Confidence = 0.91f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "9", Text = "Subtotal: $24,000.00", Confidence = 0.94f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "10", Text = "Tax (8.875%): $2,130.00", Confidence = 0.93f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "11", Text = "Total: $26,130.00", Confidence = 0.99f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "12", Text = "Payment Due: April 30, 2025", Confidence = 0.95f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "13", Text = "Payment Method: Bank Transfer", Confidence = 0.92f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "14", Text = "Account: 123456789", Confidence = 0.90f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "15", Text = "Bank: National Bank", Confidence = 0.94f, Type = "LINE", Page = 1 },
                new TextBlock { Id = "16", Text = "Thank you for your business!", Confidence = 0.97f, Type = "LINE", Page = 1 }
            };

            _logger.LogInformation("[MOCK] Successfully processed file {FileName}, created {BlockCount} mock text blocks",
                fileName, result.TextBlocks.Count);

            return result;
        }

        private string GenerateMockText(string fileName)
        {
            return @"INVOICE #INV-2023-001
Date: March 31, 2025

Bill To:
ABC Corporation
123 Business Street
New York, NY 10001

Item: Software Development Services
Quantity: 160 hours
Rate: $150.00/hour

Subtotal: $24,000.00
Tax (8.875%): $2,130.00
Total: $26,130.00

Payment Due: April 30, 2025
Payment Method: Bank Transfer
Account: 123456789
Bank: National Bank

Thank you for your business!";
        }
    }
}