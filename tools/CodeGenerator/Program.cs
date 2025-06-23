using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CodeGenerator.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration - look in the app directory first, then current directory
var appDir = AppContext.BaseDirectory;
var currentDir = Directory.GetCurrentDirectory();

// Try different config file locations
var configPaths = new[]
{
    Path.Combine(appDir, "appsettings.json"), // Tool directory
    Path.Combine(currentDir, "ca-generator.json"), // Project root
    Path.Combine(currentDir, "tools", "CodeGenerator", "appsettings.json") // Local development
};

string? configPath = null;
foreach (var path in configPaths)
{
    if (File.Exists(path))
    {
        configPath = path;
        break;
    }
}

if (!string.IsNullOrEmpty(configPath))
{
    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
}
else
{
    // Default configuration if no file found
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["CodeGeneration:ProjectRoot"] = "./src",
        ["CodeGeneration:Namespace:Domain"] = "Domain.Entities",
        ["CodeGeneration:Namespace:Application"] = "Application",
        ["CodeGeneration:Namespace:Infrastructure"] = "Infrastructure",
        ["CodeGeneration:Namespace:API"] = "API.Controllers"
    });
}

// Services
builder.Services.AddScoped<IEntityAnalyzer, EntityAnalyzer>();
builder.Services.AddScoped<ITemplateEngine, TemplateEngine>();
builder.Services.AddScoped<ICodeGenerator, CodeGeneratorImpl>();
builder.Services.AddScoped<IFileGenerator, FileGenerator>();
builder.Services.AddScoped<IProjectUpdater, ProjectUpdater>();
builder.Services.AddScoped<CodeGeneratorService>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var host = builder.Build();

try
{
    var codeGeneratorService = host.Services.GetRequiredService<CodeGeneratorService>();
    await codeGeneratorService.RunAsync(args);
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during code generation");
    return 1;
}

return 0;