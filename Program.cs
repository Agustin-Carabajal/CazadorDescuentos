using CazadorDescuentos.Features.Ingestion;

var builder = WebApplication.CreateBuilder(args);

// Register Razor Pages & Ingestion Services
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// Determine driver based on args or config
var initialConfig = BrandExtractorConfig.LoadFromEnvironment();
if (args.Contains("--simulate") || args.Contains("-s") || initialConfig.SimulateMode)
{
    builder.Services.AddSingleton<IAdbDriver, SimulatedAdbDriver>();
}
else
{
    builder.Services.AddSingleton<IAdbDriver>(sp => new AdbDriver(initialConfig.AdbPath));
}

builder.Services.AddScoped<BrandCouponExtractor>();

var app = builder.Build();

// CLI Execution: dotnet run -- --extract
if (args.Contains("--extract") || args.Contains("-e"))
{
    var simulate = args.Contains("--simulate") || args.Contains("-s");
    using var scope = app.Services.CreateScope();
    var extractor = scope.ServiceProvider.GetRequiredService<BrandCouponExtractor>();
    var config = BrandExtractorConfig.LoadFromEnvironment();
    if (simulate)
    {
        config.SimulateMode = true;
    }

    var result = await extractor.ExtractAllBrandsAsync(config);
    Environment.Exit(result.AllSucceeded ? 0 : 1);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
