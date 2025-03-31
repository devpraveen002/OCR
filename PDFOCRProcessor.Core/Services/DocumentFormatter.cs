using System.Text.Json;
using Microsoft.Extensions.Logging;
using PDFOCRProcessor.Core.Interfaces;
using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Core.Services
{
    public class DocumentFormatter : IDocumentFormatter
    {
        private readonly ILogger<DocumentFormatter> _logger;

        public DocumentFormatter(ILogger<DocumentFormatter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string FormatAsJson(ProcessedDocument document)
        {
            if (!document.IsSuccessful)
            {
                _logger.LogWarning("Cannot format document as JSON, processing was not successful: {FileName}", document.FileName);
                return JsonSerializer.Serialize(new
                {
                    fileName = document.FileName,
                    success = false,
                    error = document.ErrorMessage
                });
            }

            try
            {
                _logger.LogInformation("Formatting document as JSON: {FileName}", document.FileName);

                // Group fields by name
                var groupedFields = document.Fields
                    .GroupBy(f => f.Name)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(f => f.Confidence).First().Value);

                // Create structured document
                var result = new
                {
                    fileName = document.FileName,
                    success = true,
                    documentType = document.DocumentType,
                    processedDate = document.ProcessedDate,
                    fields = groupedFields
                };

                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                return JsonSerializer.Serialize(result, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting document as JSON: {FileName}", document.FileName);
                return JsonSerializer.Serialize(new
                {
                    fileName = document.FileName,
                    success = false,
                    error = $"JSON formatting failed: {ex.Message}"
                });
            }
        }

        public string FormatAsCsv(ProcessedDocument document)
        {
            if (!document.IsSuccessful)
            {
                _logger.LogWarning("Cannot format document as CSV, processing was not successful: {FileName}", document.FileName);
                return $"FileName,Success,Error\n{document.FileName},false,\"{document.ErrorMessage}\"";
            }

            try
            {
                _logger.LogInformation("Formatting document as CSV: {FileName}", document.FileName);

                // Group fields by name and take highest confidence
                var groupedFields = document.Fields
                    .GroupBy(f => f.Name)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(f => f.Confidence).First().Value);

                // Create CSV header
                var csvBuilder = new System.Text.StringBuilder();
                csvBuilder.Append("FileName,DocumentType,ProcessedDate");

                foreach (var field in groupedFields.Keys)
                {
                    csvBuilder.Append($",{field}");
                }

                csvBuilder.AppendLine();

                // Create CSV data row
                csvBuilder.Append($"{document.FileName},{document.DocumentType},{document.ProcessedDate}");

                foreach (var field in groupedFields.Keys)
                {
                    // Escape quotes and commas in CSV
                    string value = groupedFields[field]?.Replace("\"", "\"\"") ?? "";
                    csvBuilder.Append($",\"{value}\"");
                }

                return csvBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting document as CSV: {FileName}", document.FileName);
                return $"FileName,Success,Error\n{document.FileName},false,\"CSV formatting failed: {ex.Message}\"";
            }
        }
    }
}