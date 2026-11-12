using CazadorDescuentos.Modules.CatalogAndUserExperience.Services;
using CazadorDescuentos.Modules.IngestionAndAI.Services;
using CazadorDescuentos.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Database Connection (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost\\SQLEXPRESS;Database=CazadorDescuentosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Domain & Feature Services (Scoped for web requests as per conventions)
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INormalizationService, NormalizationService>();
builder.Services.AddScoped<IBenefitClassificationService, BenefitClassificationService>();
builder.Services.AddScoped<IAiInterpretationService, AiInterpretationService>();
builder.Services.AddScoped<IOcrExtractionService, OcrExtractionService>();
builder.Services.AddScoped<IScraperService, ScraperService>();

// Register HTTP Client for scraping and external AI calls
builder.Services.AddHttpClient();

// Register Hosted Services / Background Workers (using IServiceScopeFactory inside as per conventions)
builder.Services.AddHostedService<ScraperBackgroundService>();
builder.Services.AddHostedService<OfferCleanupBackgroundService>();

// Add Controllers with Views & configure feature folder view locations
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/Modules/CatalogAndUserExperience/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Modules/CatalogAndUserExperience/Views/Shared/{0}.cshtml");
        options.ViewLocationFormats.Add("/Modules/IngestionAndAI/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });

// CORS for Frontend SPA integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Catalog/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthorization();

// Route mappings
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapControllers();

// Initialize database schema and baseline seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.InitializeAsync(context, logger);
}

app.Run();
