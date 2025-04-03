# OCR
1.
```
# Create a new solution
dotnet new sln -n PDFOCRProcessor

# Create API project
dotnet new webapi -n PDFOCRProcessor.API
dotnet sln add PDFOCRProcessor.API

# Create Core project for business logic
dotnet new classlib -n PDFOCRProcessor.Core
dotnet sln add PDFOCRProcessor.Core

# Create Infrastructure project for external services
dotnet new classlib -n PDFOCRProcessor.Infrastructure
dotnet sln add PDFOCRProcessor.Infrastructure
```
2.
```
# For AWS Textract
dotnet add PDFOCRProcessor.Infrastructure package AWSSDK.Textract
dotnet add PDFOCRProcessor.Infrastructure package AWSSDK.S3

# For PDF handling
dotnet add PDFOCRProcessor.Infrastructure package itext7

# For logging
dotnet add PDFOCRProcessor.API package Serilog.AspNetCore
dotnet add PDFOCRProcessor.API package Serilog.Sinks.Console
dotnet add PDFOCRProcessor.API package Serilog.Sinks.File

#Swagger
dotnet add PDFOCRProcessor.API package Swashbuckle.AspNetCore

#Migration
dotnet ef migrations add InitialCreate --project PDFOCRProcessor.Infrastructure --startup-project PDFOCRProcessor.API
dotnet ef database update --project PDFOCRProcessor.Infrastructure --startup-project PDFOCRProcessor.API

#DbScripts
dotnet ef migrations script --project PDFOCRProcessor.Infrastructure --startup-project PDFOCRProcessor.API -o DbScripts\FreshSetupRepoDb20240213.sql
```

3. 
```
# Create React project
npx create-react-app pdf-ocr-frontend
cd pdf-ocr-frontend

# Install required packages
npm install axios react-router-dom bootstrap react-bootstrap @fortawesome/react-fontawesome @fortawesome/free-solid-svg-icons
```