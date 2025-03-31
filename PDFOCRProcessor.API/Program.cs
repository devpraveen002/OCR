using Amazon;
using Amazon.Textract;
using Microsoft.EntityFrameworkCore;
using PDFOCRProcessor.Core.Interfaces;
using PDFOCRProcessor.Core.Services;
using PDFOCRProcessor.Infrastructure.Data;
using PDFOCRProcessor.Infrastructure.Repositories;
using PDFOCRProcessor.Infrastructure.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pdfocr.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Replace SQL Server with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure AWS services
//builder.Services.AddSingleton<AmazonTextractClient>(_ =>
//    new AmazonTextractClient(
//        builder.Configuration["AWS:AccessKey"],
//        builder.Configuration["AWS:SecretKey"],
//        RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"] ?? "us-east-1")));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Register services
//builder.Services.AddScoped<IOcrService, TextractOcrService>();
builder.Services.AddScoped<IOcrService, MockOcrService>();
builder.Services.AddScoped<ITextProcessor, TextProcessor>();
builder.Services.AddScoped<IDocumentFormatter, DocumentFormatter>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentProcessorService, DocumentProcessorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();