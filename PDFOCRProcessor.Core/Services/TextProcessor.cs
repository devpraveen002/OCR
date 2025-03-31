using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PDFOCRProcessor.Core.Interfaces;
using PDFOCRProcessor.Core.Models;

namespace PDFOCRProcessor.Core.Services
{
    public class TextProcessor : ITextProcessor
    {
        private readonly ILogger<TextProcessor> _logger;

        public TextProcessor(ILogger<TextProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ProcessedDocument ProcessText(OcrResult ocrResult)
        {
            if (!ocrResult.IsSuccessful)
            {
                _logger.LogWarning("Cannot process text, OCR was not successful: {FileName}", ocrResult.FileName);
                return new ProcessedDocument
                {
                    FileName = ocrResult.FileName,
                    IsSuccessful = false,
                    ErrorMessage = "OCR processing failed, cannot extract structured data"
                };
            }

            try
            {
                _logger.LogInformation("Starting text processing for {FileName}", ocrResult.FileName);

                // Normalize the text
                string normalizedText = NormalizeText(ocrResult.RawText);

                // Try to determine document type (invoice, receipt, etc.)
                string documentType = DetectDocumentType(normalizedText);

                // Extract fields based on document type
                var extractedFields = ExtractFields(normalizedText, documentType);

                // Create structured document
                var document = new ProcessedDocument
                {
                    FileName = ocrResult.FileName,
                    IsSuccessful = true,
                    ProcessedDate = DateTime.UtcNow,
                    DocumentType = documentType,
                    NormalizedText = normalizedText,
                    Fields = extractedFields,
                };

                _logger.LogInformation("Successfully processed document {FileName} as {DocumentType} with {FieldCount} fields",
                    ocrResult.FileName, documentType, extractedFields.Count);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text for {FileName}", ocrResult.FileName);
                return new ProcessedDocument
                {
                    FileName = ocrResult.FileName,
                    IsSuccessful = false,
                    ErrorMessage = $"Text processing failed: {ex.Message}"
                };
            }
        }

        private string NormalizeText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return string.Empty;

            StringBuilder normalized = new StringBuilder(rawText);

            // Replace multiple spaces with a single space
            normalized = new StringBuilder(Regex.Replace(normalized.ToString(), @"\s+", " "));

            // Remove non-printable characters
            normalized = new StringBuilder(Regex.Replace(normalized.ToString(), @"[^\x20-\x7E\r\n]", ""));

            // Normalize line endings
            normalized = new StringBuilder(normalized.ToString().Replace("\r\n", "\n").Replace("\r", "\n"));

            return normalized.ToString().Trim();
        }

        private string DetectDocumentType(string text)
        {
            // Simple rule-based detection
            text = text.ToLower();

            if (text.Contains("invoice") || text.Contains("bill to"))
                return "Invoice";
            else if (text.Contains("receipt") || text.Contains("payment received"))
                return "Receipt";
            else if (text.Contains("statement") || text.Contains("account summary"))
                return "Statement";
            else if (text.Contains("order") && (text.Contains("purchase") || text.Contains("confirmation")))
                return "PurchaseOrder";

            return "Unknown";
        }

        private List<DocumentField> ExtractFields(string text, string documentType)
        {
            var fields = new List<DocumentField>();

            // Common fields across document types
            ExtractDates(text, fields);
            ExtractAmounts(text, fields);

            // Document-specific extraction
            switch (documentType)
            {
                case "Invoice":
                    ExtractInvoiceFields(text, fields);
                    break;
                case "Receipt":
                    ExtractReceiptFields(text, fields);
                    break;
                case "Statement":
                    ExtractStatementFields(text, fields);
                    break;
                case "PurchaseOrder":
                    ExtractPurchaseOrderFields(text, fields);
                    break;
            }

            return fields;
        }

        private void ExtractDates(string text, List<DocumentField> fields)
        {
            // Various date formats: MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD, Month DD, YYYY
            var datePatterns = new List<(string pattern, string format)>
            {
                (@"(\d{1,2})[/\.-](\d{1,2})[/\.-](\d{2,4})", "Date"),
                (@"(\d{4})[/\.-](\d{1,2})[/\.-](\d{1,2})", "Date"),
                (@"(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]* (\d{1,2}),? (\d{4})", "Date")
            };

            foreach (var (pattern, fieldName) in datePatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    fields.Add(new DocumentField
                    {
                        Name = fieldName,
                        Value = match.Value,
                        Confidence = 0.8f // Arbitrary confidence for regex matches
                    });
                }
            }
        }

        private void ExtractAmounts(string text, List<DocumentField> fields)
        {
            // Look for currency amounts
            var amounts = Regex.Matches(text, @"(\$|€|£|USD|EUR|GBP)?\s*(\d{1,3}(,\d{3})*(\.\d{2})?)");

            // Look for labeled amounts
            var totalMatches = Regex.Matches(text, @"total\s*:?\s*(\$|€|£|USD|EUR|GBP)?\s*(\d{1,3}(,\d{3})*(\.\d{2})?)", RegexOptions.IgnoreCase);
            var subtotalMatches = Regex.Matches(text, @"subtotal\s*:?\s*(\$|€|£|USD|EUR|GBP)?\s*(\d{1,3}(,\d{3})*(\.\d{2})?)", RegexOptions.IgnoreCase);
            var taxMatches = Regex.Matches(text, @"(tax|vat|gst)\s*:?\s*(\$|€|£|USD|EUR|GBP)?\s*(\d{1,3}(,\d{3})*(\.\d{2})?)", RegexOptions.IgnoreCase);

            // Add total amount
            foreach (Match match in totalMatches)
            {
                fields.Add(new DocumentField
                {
                    Name = "TotalAmount",
                    Value = match.Value,
                    Confidence = 0.9f
                });
            }

            // Add subtotal
            foreach (Match match in subtotalMatches)
            {
                fields.Add(new DocumentField
                {
                    Name = "SubtotalAmount",
                    Value = match.Value,
                    Confidence = 0.85f
                });
            }

            // Add tax
            foreach (Match match in taxMatches)
            {
                fields.Add(new DocumentField
                {
                    Name = "TaxAmount",
                    Value = match.Value,
                    Confidence = 0.85f
                });
            }
        }

        private void ExtractInvoiceFields(string text, List<DocumentField> fields)
        {
            // Invoice number
            var invoiceNumberMatches = Regex.Matches(text, @"invoice\s*(?:no|number|#)?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
            foreach (Match match in invoiceNumberMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "InvoiceNumber",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.9f
                    });
                }
            }

            // Due date
            var dueDateMatches = Regex.Matches(text, @"due\s*date\s*:?\s*(.+?)($|\n)", RegexOptions.IgnoreCase);
            foreach (Match match in dueDateMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "DueDate",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.85f
                    });
                }
            }

            // Vendor/company name - look for "from" section
            var vendorMatches = Regex.Matches(text, @"(?:from|vendor|supplier|company)?\s*:?\s*([A-Za-z0-9\s\.,]+)(?:\n|$)", RegexOptions.IgnoreCase);
            foreach (Match match in vendorMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "VendorName",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.7f
                    });
                }
            }
        }

        private void ExtractReceiptFields(string text, List<DocumentField> fields)
        {
            // Receipt number/ID
            var receiptNumberMatches = Regex.Matches(text, @"receipt\s*(?:no|number|#|id)?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
            foreach (Match match in receiptNumberMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "ReceiptNumber",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.9f
                    });
                }
            }

            // Store/merchant name
            var merchantMatches = Regex.Matches(text, @"^([A-Za-z0-9\s\.,]+?)(?:\n|$)", RegexOptions.IgnoreCase);
            foreach (Match match in merchantMatches)
            {
                fields.Add(new DocumentField
                {
                    Name = "MerchantName",
                    Value = match.Value.Trim(),
                    Confidence = 0.6f
                });
            }

            // Payment method
            var paymentMatches = Regex.Matches(text, @"(?:paid|payment|method)\s*(?:by|via|:)?\s*([A-Za-z]+\s*card|cash|check|paypal|venmo)", RegexOptions.IgnoreCase);
            foreach (Match match in paymentMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "PaymentMethod",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.8f
                    });
                }
            }
        }

        private void ExtractStatementFields(string text, List<DocumentField> fields)
        {
            // Account number
            var accountMatches = Regex.Matches(text, @"account\s*(?:no|number|#)?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
            foreach (Match match in accountMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "AccountNumber",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.9f
                    });
                }
            }

            // Statement period
            var periodMatches = Regex.Matches(text, @"(?:statement|billing)\s*period\s*:?\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase);
            foreach (Match match in periodMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "StatementPeriod",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.85f
                    });
                }
            }
        }

        private void ExtractPurchaseOrderFields(string text, List<DocumentField> fields)
        {
            // PO number
            var poMatches = Regex.Matches(text, @"(?:purchase\s*order|p\.?o\.?)(?:\s*no|\s*number|\s*#)?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
            foreach (Match match in poMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "PurchaseOrderNumber",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.9f
                    });
                }
            }

            // Delivery date
            var deliveryMatches = Regex.Matches(text, @"(?:delivery|ship)\s*date\s*:?\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase);
            foreach (Match match in deliveryMatches)
            {
                if (match.Groups.Count > 1)
                {
                    fields.Add(new DocumentField
                    {
                        Name = "DeliveryDate",
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.85f
                    });
                }
            }
        }
    }
}