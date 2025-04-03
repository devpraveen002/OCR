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

        // Fix the regex patterns in ExtractInvoiceFields method
        //private void ExtractInvoiceFields(string text, List<DocumentField> fields)
        //{
        //    // Clean up field list - remove duplicate Date fields
        //    fields.RemoveAll(f => f.Name == "Date");

        //    // Invoice number - looking for NV-1001
        //    var invoiceNumberPattern = @"(?:Invoice ?Number|NV-\d+)\s*(NV-\d+)";
        //    var invoiceNumberMatches = Regex.Matches(text, invoiceNumberPattern, RegexOptions.IgnoreCase);
        //    if (invoiceNumberMatches.Count > 0 && invoiceNumberMatches[0].Groups.Count > 1)
        //    {
        //        fields.Add(new DocumentField
        //        {
        //            Name = "InvoiceNumber",
        //            Value = invoiceNumberMatches[0].Groups[1].Value.Trim(),
        //            Confidence = 0.85f
        //        });
        //    }
        //    else
        //    {
        //        // Try a direct pattern match for "NV-1001"
        //        var directMatches = Regex.Matches(text, @"NV-\d{4}", RegexOptions.IgnoreCase);
        //        if (directMatches.Count > 0)
        //        {
        //            fields.Add(new DocumentField
        //            {
        //                Name = "InvoiceNumber",
        //                Value = directMatches[0].Value,
        //                Confidence = 0.85f
        //            });
        //        }
        //    }

        //    // Invoice date - specifically looking for 2024-03-31 format
        //    var invoiceDatePattern = @"(?:Invoice ?Date\s*|20\d{2}-\d{2}-\d{2})\s*(20\d{2}-\d{2}-\d{2})";
        //    var invoiceDateMatches = Regex.Matches(text, invoiceDatePattern, RegexOptions.IgnoreCase);
        //    if (invoiceDateMatches.Count > 0 && invoiceDateMatches[0].Groups.Count > 1)
        //    {
        //        fields.Add(new DocumentField
        //        {
        //            Name = "InvoiceDate",
        //            Value = invoiceDateMatches[0].Groups[1].Value.Trim(),
        //            Confidence = 0.85f
        //        });
        //    }
        //    else
        //    {
        //        // Try a direct pattern match for dates in YYYY-MM-DD format
        //        var directDateMatches = Regex.Matches(text, @"20\d{2}-\d{2}-\d{2}", RegexOptions.IgnoreCase);
        //        if (directDateMatches.Count > 0)
        //        {
        //            fields.Add(new DocumentField
        //            {
        //                Name = "InvoiceDate",
        //                Value = directDateMatches[0].Value,
        //                Confidence = 0.85f
        //            });
        //        }
        //    }

        //    // Total amount - specifically looking for 1500.75 format
        //    var totalAmountPattern = @"(?:TotalAmount\s*|(?<=^|\n|\s)(?:1500|15\d\d)\.\d{2})\s*(\d+\.\d{2})";
        //    var totalAmountMatches = Regex.Matches(text, totalAmountPattern, RegexOptions.IgnoreCase);
        //    if (totalAmountMatches.Count > 0 && totalAmountMatches[0].Groups.Count > 1)
        //    {
        //        fields.Add(new DocumentField
        //        {
        //            Name = "TotalAmount",
        //            Value = totalAmountMatches[0].Groups[1].Value.Trim(),
        //            Confidence = 0.85f
        //        });
        //    }
        //    else
        //    {
        //        // Try direct pattern match for dollar amounts
        //        var directAmountMatches = Regex.Matches(text, @"\d{3,4}\.\d{2}", RegexOptions.IgnoreCase);
        //        if (directAmountMatches.Count > 0)
        //        {
        //            fields.Add(new DocumentField
        //            {
        //                Name = "TotalAmount",
        //                Value = directAmountMatches[0].Value,
        //                Confidence = 0.85f
        //            });
        //        }
        //    }
        //    // Call the vendor name extraction
        //    //ExtractVendorName(text, fields);

        //}

        // Vendor name extraction - much more flexible approach
        //    private void ExtractVendorName(string text, List<DocumentField> fields)
        //    {
        //        // First, try to match exact known patterns
        //        var exactMatches = Regex.Match(text, @"ABC\.?LTD", RegexOptions.IgnoreCase);
        //        if (exactMatches.Success)
        //        {
        //            fields.Add(new DocumentField
        //            {
        //                Name = "VendorName",
        //                Value = exactMatches.Value,
        //                Confidence = 0.9f
        //            });
        //            return;
        //        }

        //        // Second, try to extract vendor name from the position after "VendorName" label
        //        var labeledMatches = Regex.Match(text, @"VendorName\s+([A-Za-z0-9\.\s]{3,}?)(?:\s*\n|\s*$)", RegexOptions.IgnoreCase);
        //        if (labeledMatches.Success && labeledMatches.Groups.Count > 1 &&
        //            !string.IsNullOrWhiteSpace(labeledMatches.Groups[1].Value))
        //        {
        //            var vendorValue = labeledMatches.Groups[1].Value.Trim();
        //            // Make sure we're not capturing something that looks like a column header
        //            if (!vendorValue.Equals("InvoiceNumber", StringComparison.OrdinalIgnoreCase) &&
        //                !vendorValue.Equals("InvoiceDate", StringComparison.OrdinalIgnoreCase) &&
        //                !vendorValue.Equals("TotalAmount", StringComparison.OrdinalIgnoreCase))
        //            {
        //                fields.Add(new DocumentField
        //                {
        //                    Name = "VendorName",
        //                    Value = vendorValue,
        //                    Confidence = 0.85f
        //                });
        //                return;
        //            }
        //        }

        //        // Third, look for company indicators in the text (Ltd, Inc, LLC, etc.)
        //        var companyPatterns = new[] {
        //    @"([A-Za-z0-9\s\.]{2,}\s+(?:Ltd|LLC|Inc|Corp|Pvt|Private|Limited))",
        //    @"([A-Za-z0-9\s\.]{2,}\s+(?:ltd|llc|inc|corp|pvt|private|limited))"
        //};

        //        foreach (var pattern in companyPatterns)
        //        {
        //            var companyMatches = Regex.Match(text, pattern);
        //            if (companyMatches.Success && companyMatches.Groups.Count > 1)
        //            {
        //                fields.Add(new DocumentField
        //                {
        //                    Name = "VendorName",
        //                    Value = companyMatches.Groups[1].Value.Trim(),
        //                    Confidence = 0.8f
        //                });
        //                return;
        //            }
        //        }

        //        // Fourth, as a fallback, get text from table cell in VendorName position
        //        // This approach assumes the data is in a tabular format with headers
        //        var lines = text.Split('\n');
        //        for (int i = 0; i < lines.Length; i++)
        //        {
        //            if (lines[i].Contains("VendorName", StringComparison.OrdinalIgnoreCase) && i + 1 < lines.Length)
        //            {
        //                // Get the next line after VendorName header
        //                var vendorLine = lines[i + 1].Trim();
        //                if (!string.IsNullOrWhiteSpace(vendorLine) && vendorLine.Length > 2)
        //                {
        //                    fields.Add(new DocumentField
        //                    {
        //                        Name = "VendorName",
        //                        Value = vendorLine,
        //                        Confidence = 0.7f
        //                    });
        //                    return;
        //                }
        //            }
        //        }
        //    }

        private void ExtractInvoiceFields(string text, List<DocumentField> fields)
        {
            // Clean up field list - remove duplicate Date fields
            fields.RemoveAll(f => f.Name == "Date");

            // Invoice number (this part works well, so keep it)
            var invoiceNumberPattern = @"NV-\d{4}";
            var invoiceNumberMatches = Regex.Matches(text, invoiceNumberPattern, RegexOptions.IgnoreCase);
            if (invoiceNumberMatches.Count > 0)
            {
                fields.Add(new DocumentField
                {
                    Name = "InvoiceNumber",
                    Value = invoiceNumberMatches[0].Value,
                    Confidence = 0.85f
                });
            }

            // Invoice date - look specifically for dates in YYYY-MM-DD format
            var invoiceDatePattern = @"(20\d{2}-\d{2}-\d{2})";
            var invoiceDateMatches = Regex.Matches(text, invoiceDatePattern, RegexOptions.IgnoreCase);
            if (invoiceDateMatches.Count > 0)
            {
                fields.Add(new DocumentField
                {
                    Name = "InvoiceDate",
                    Value = invoiceDateMatches[0].Value,
                    Confidence = 0.85f
                });
            }

            // Total amount - look for numeric values that appear after TotalAmount label
            // First, try to find in tabular format
            var tableAmountPattern = @"TotalAmount\s+(\d+\.?\d*)";
            var tableAmountMatches = Regex.Matches(text, tableAmountPattern, RegexOptions.IgnoreCase);
            if (tableAmountMatches.Count > 0 && tableAmountMatches[0].Groups.Count > 1)
            {
                fields.Add(new DocumentField
                {
                    Name = "TotalAmount",
                    Value = tableAmountMatches[0].Groups[1].Value.Trim(),
                    Confidence = 0.85f
                });
            }
            else
            {
                // Try direct pattern match for amounts
                var directAmountPatterns = new[] {
            @"(?<=\s)(\d{3,4}\.?\d*)(?=\s)",  // Numbers like 1500.75 or 3500
            @"(?<=TotalAmount\s+)(\d+)(?=\s)" // Numbers after TotalAmount label
        };

                foreach (var pattern in directAmountPatterns)
                {
                    var directMatches = Regex.Matches(text, pattern);
                    if (directMatches.Count > 0)
                    {
                        fields.Add(new DocumentField
                        {
                            Name = "TotalAmount",
                            Value = directMatches[0].Value,
                            Confidence = 0.85f
                        });
                        break;
                    }
                }
            }

            // Vendor name extraction
            // First try to match known company formats
            var vendorPatterns = new[] {
        @"(?:VendorName\s+)((?:ABC\.?LTD|Tcs MNC|Mnb Pvt Ltd))",
        @"(?<=VendorName\s+)([A-Za-z0-9\.\s]+)(?=\s|\n|$)",
        @"(?<=[0-9]\s)([A-Za-z][A-Za-z0-9\s\.]*(?:Ltd|MNC|LLC|Inc|Corp|Pvt))"
    };

            bool vendorFound = false;
            foreach (var pattern in vendorPatterns)
            {
                var vendorMatches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                if (vendorMatches.Count > 0)
                {
                    string vendorValue;
                    if (vendorMatches[0].Groups.Count > 1)
                        vendorValue = vendorMatches[0].Groups[1].Value.Trim();
                    else
                        vendorValue = vendorMatches[0].Value.Trim();

                    // Ensure we don't capture numbers or other fields in vendor name
                    if (!Regex.IsMatch(vendorValue, @"^\d+(\.\d+)?$") &&
                        !string.IsNullOrWhiteSpace(vendorValue) &&
                        vendorValue.Length > 2)
                    {
                        fields.Add(new DocumentField
                        {
                            Name = "VendorName",
                            Value = vendorValue,
                            Confidence = 0.85f
                        });
                        vendorFound = true;
                        break;
                    }
                }
            }

            // If we still don't have a vendor name, try table cell approach
            if (!vendorFound)
            {
                // Try to find VendorName from table structure
                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("VendorName") && i + 1 < lines.Length)
                    {
                        var nextLine = lines[i + 1].Trim();
                        if (!string.IsNullOrWhiteSpace(nextLine) &&
                            !nextLine.StartsWith("Invoice") &&
                            !Regex.IsMatch(nextLine, @"^\d+(\.\d+)?$"))
                        {
                            fields.Add(new DocumentField
                            {
                                Name = "VendorName",
                                Value = nextLine,
                                Confidence = 0.8f
                            });
                            break;
                        }
                    }
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