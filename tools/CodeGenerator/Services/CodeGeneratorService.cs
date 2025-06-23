using CodeGenerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeGenerator.Services;

public class CodeGeneratorService
{
    private readonly IEntityAnalyzer _entityAnalyzer;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IFileGenerator _fileGenerator;
    private readonly IProjectUpdater _projectUpdater;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CodeGeneratorService> _logger;

    public CodeGeneratorService(
        IEntityAnalyzer entityAnalyzer,
        ICodeGenerator codeGenerator,
        IFileGenerator fileGenerator,
        IProjectUpdater projectUpdater,
        IConfiguration configuration,
        ILogger<CodeGeneratorService> logger)
    {
        _entityAnalyzer = entityAnalyzer;
        _codeGenerator = codeGenerator;
        _fileGenerator = fileGenerator;
        _projectUpdater = projectUpdater;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            
            if (options.DryRun)
            {
                _logger.LogInformation("Running in DRY RUN mode - no files will be written");
            }

            // Discover entities if no specific entity provided
            if (string.IsNullOrEmpty(options.EntityName))
            {
                await ShowAvailableEntitiesAsync();
                return;
            }

            // Load entity information
            var entityInfo = await LoadEntityInfoAsync(options.EntityName);
            if (entityInfo == null)
            {
                _logger.LogError("Entity '{EntityName}' not found", options.EntityName);
                return;
            }

            // Generate files
            _logger.LogInformation("Generating code for entity: {EntityName}", entityInfo.Name);
            var files = await _codeGenerator.GenerateAllAsync(entityInfo, options);

            // Set file paths
            foreach (var file in files)
            {
                file.FilePath = await _fileGenerator.GetOutputPathAsync(file.FileName, file.Category, options);
            }

            // Display generated files
            DisplayGeneratedFiles(files, options);

            // Write files if not dry run
            if (!options.DryRun)
            {
                await _fileGenerator.WriteFilesAsync(files, options.OverwriteExisting);
                _logger.LogInformation("Code generation completed successfully!");
                
                // Update project files automatically
                _logger.LogInformation("Updating project files...");
                await _projectUpdater.UpdateProgramCsAsync(entityInfo, options);
                await _projectUpdater.UpdateUnitOfWorkAsync(entityInfo, options);
                await _projectUpdater.UpdateAutoMapperAsync(entityInfo, options);
                _logger.LogInformation("Project files updated successfully!");
                
                // Show next steps
                ShowNextSteps(entityInfo, options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Code generation failed");
            throw;
        }
    }

    private GenerationOptions ParseArguments(string[] args)
    {
        var options = new GenerationOptions();
        
        // Load from configuration
        var config = _configuration.GetSection("CodeGeneration");
        options.ProjectRoot = config["ProjectRoot"] ?? "./src";
        
        // Convert relative path to absolute if needed
        if (!Path.IsPathRooted(options.ProjectRoot))
        {
            options.ProjectRoot = Path.GetFullPath(options.ProjectRoot);
        }
        options.Namespaces.Domain = config["Namespace:Domain"] ?? "Domain.Entities";
        options.Namespaces.Application = config["Namespace:Application"] ?? "Application";
        options.Namespaces.Infrastructure = config["Namespace:Infrastructure"] ?? "Infrastructure";
        options.Namespaces.API = config["Namespace:API"] ?? "API.Controllers";

        var features = config.GetSection("Features");
        options.Features.GenerateRepository = features.GetValue<bool>("GenerateRepository", true);
        options.Features.GenerateService = features.GetValue<bool>("GenerateService", true);
        options.Features.GenerateController = features.GetValue<bool>("GenerateController", true);
        options.Features.GenerateDTOs = features.GetValue<bool>("GenerateDTOs", true);
        options.Features.GenerateMapper = features.GetValue<bool>("GenerateMapper", true);
        options.Features.AddCaching = features.GetValue<bool>("AddCaching", true);
        options.Features.AddLogging = features.GetValue<bool>("AddLogging", true);
        options.Features.AddValidation = features.GetValue<bool>("AddValidation", true);
        options.Features.AddAudit = features.GetValue<bool>("AddAudit", true);

        // Load custom actions
        var customActionsSection = config.GetSection("CustomActions");
        if (customActionsSection.Exists())
        {
            foreach (var actionSection in customActionsSection.GetChildren())
            {
                var customAction = new CustomAction();
                actionSection.Bind(customAction);
                options.CustomActions.Add(customAction);
            }
        }

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-e" or "--entity":
                    if (i + 1 < args.Length) options.EntityName = args[++i];
                    break;
                case "-o" or "--output":
                    if (i + 1 < args.Length) options.ProjectRoot = args[++i];
                    break;
                case "--overwrite":
                    options.OverwriteExisting = true;
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
                case "-v" or "--verbose":
                    options.Verbose = true;
                    break;
                case "--no-cache":
                    options.Features.AddCaching = false;
                    break;
                case "--no-logging":
                    options.Features.AddLogging = false;
                    break;
                case "--no-audit":
                    options.Features.AddAudit = false;
                    break;
                case "--custom-actions":
                    if (i + 1 < args.Length) 
                    {
                        var actionNames = args[++i].Split(',');
                        options.CustomActions = options.CustomActions
                            .Where(a => actionNames.Contains(a.Name, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                    }
                    break;
                case "--actions-only":
                    options.Features.GenerateRepository = false;
                    options.Features.GenerateService = false;
                    options.Features.GenerateController = false;
                    options.Features.GenerateDTOs = false;
                    options.Features.GenerateMapper = false;
                    break;
                case "--controller-only":
                    options.Features.GenerateRepository = false;
                    options.Features.GenerateService = false;
                    options.Features.GenerateDTOs = false;
                    options.Features.GenerateMapper = false;
                    break;
                case "--service-only":
                    options.Features.GenerateController = false;
                    options.Features.GenerateRepository = false;
                    break;
                case "-h" or "--help":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    private async Task ShowAvailableEntitiesAsync()
    {
        try
        {
            var domainAssemblyPath = FindDomainAssembly();
            
            if (string.IsNullOrEmpty(domainAssemblyPath))
            {
                _logger.LogWarning("Domain assembly not found. Searched locations:");
                _logger.LogInformation("  • Current directory: Domain.dll");
                _logger.LogInformation("  • src/Domain/bin/Debug/net8.0/Domain.dll");
                _logger.LogInformation("  • src/Domain/bin/Release/net8.0/Domain.dll");
                _logger.LogInformation("Please ensure the Domain project is built.");
                return;
            }

            var entities = await _entityAnalyzer.GetAvailableEntitiesAsync(domainAssemblyPath);
            
            if (!entities.Any())
            {
                _logger.LogWarning("No entities found in the Domain assembly");
                return;
            }

            Console.WriteLine("\n📋 Available Entities:");
            //Console.WriteLine("=" * 50);
            
            foreach (var entity in entities)
            {
                Console.WriteLine($"  • {entity}");
            }
            
            Console.WriteLine("\n💡 Usage Examples:");
            Console.WriteLine($"  ca-generator --entity {entities.First()}");
            Console.WriteLine($"  ca-generator --entity {entities.First()} --overwrite");
            Console.WriteLine($"  ca-generator --entity {entities.First()} --dry-run");
            Console.WriteLine($"  ca-generator --entity {entities.First()} --controller-only");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available entities");
        }
    }

    private async Task<EntityInfo?> LoadEntityInfoAsync(string entityName)
    {
        try
        {
            var domainAssemblyPath = FindDomainAssembly();
            
            if (string.IsNullOrEmpty(domainAssemblyPath))
            {
                _logger.LogError("Domain assembly not found. Please ensure the Domain project is built.");
                return null;
            }

            var assembly = Assembly.LoadFrom(domainAssemblyPath);
            var entityType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

            if (entityType == null)
            {
                _logger.LogError("Entity type '{EntityName}' not found in assembly", entityName);
                return null;
            }

            return await _entityAnalyzer.AnalyzeEntityAsync(entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading entity information for {EntityName}", entityName);
            return null;
        }
    }

    private void DisplayGeneratedFiles(List<GeneratedFile> files, GenerationOptions options)
    {
        Console.WriteLine($"\n🎯 Generated Files for {files.First()?.Category ?? "Entity"}:");
        //Console.WriteLine("=" * 60);

        var groupedFiles = files.GroupBy(f => f.Category);
        
        foreach (var group in groupedFiles)
        {
            Console.WriteLine($"\n📁 {group.Key}:");
            foreach (var file in group)
            {
                var status = options.DryRun ? "[DRY RUN]" : 
                           File.Exists(file.FilePath) ? "[OVERWRITE]" : "[NEW]";
                Console.WriteLine($"  {status} {file.FilePath}");
            }
        }

        Console.WriteLine($"\n📊 Summary:");
        Console.WriteLine($"  • Total files: {files.Count}");
        Console.WriteLine($"  • Categories: {groupedFiles.Count()}");
        
        if (options.DryRun)
        {
            Console.WriteLine($"  • Mode: DRY RUN (no files written)");
        }
        else
        {
            var existingFiles = files.Count(f => File.Exists(f.FilePath));
            Console.WriteLine($"  • New files: {files.Count - existingFiles}");
            Console.WriteLine($"  • Overwriting: {existingFiles}");
        }
    }

    private void ShowNextSteps(EntityInfo entity, GenerationOptions options)
    {
        Console.WriteLine($"\n🚀 Next Steps:");
        //Console.WriteLine("=" * 40);
        Console.WriteLine($"✅ Project files have been automatically updated:");
        Console.WriteLine($"   • Program.cs - Service registrations added");
        Console.WriteLine($"   • UnitOfWork - {entity.Name} repository added");
        Console.WriteLine($"   • AutoMapper - {entity.Name}MappingProfile registered");
        Console.WriteLine();
        Console.WriteLine($"1. Build and run the application:");
        Console.WriteLine($"   dotnet build");
        Console.WriteLine($"   dotnet run --project src/API");
        Console.WriteLine();
        Console.WriteLine($"2. Test the generated endpoints:");
        Console.WriteLine($"   GET    /api/v1/{entity.PluralName.ToLowerInvariant()}");
        Console.WriteLine($"   GET    /api/v1/{entity.PluralName.ToLowerInvariant()}/{{id}}");
        Console.WriteLine($"   POST   /api/v1/{entity.PluralName.ToLowerInvariant()}");
        Console.WriteLine($"   PUT    /api/v1/{entity.PluralName.ToLowerInvariant()}/{{id}}");
        Console.WriteLine($"   DELETE /api/v1/{entity.PluralName.ToLowerInvariant()}/{{id}}");
        Console.WriteLine();
        Console.WriteLine($"3. Check Swagger UI for API documentation:");
        Console.WriteLine($"   https://localhost:7049/swagger");
    }

    private void ShowHelp()
    {
        Console.WriteLine("🔧 Clean Architecture Code Generator");
        //Console.WriteLine("=" * 50);
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  ca-generator [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -e, --entity <name>     Entity name to generate code for");
        Console.WriteLine("  -o, --output <path>     Output path (default: ./src)");
        Console.WriteLine("      --overwrite         Overwrite existing files");
        Console.WriteLine("      --dry-run           Show what would be generated without writing files");
        Console.WriteLine("  -v, --verbose           Verbose output");
        Console.WriteLine();
        Console.WriteLine("FEATURE FLAGS:");
        Console.WriteLine("      --no-cache          Disable caching attributes");
        Console.WriteLine("      --no-logging        Disable logging attributes");
        Console.WriteLine("      --no-audit          Disable audit attributes");
        Console.WriteLine("      --controller-only   Generate only controller");
        Console.WriteLine("      --service-only      Generate only service layer");
        Console.WriteLine("      --actions-only      Generate only custom actions (no CRUD)");
        Console.WriteLine("      --custom-actions <list> Generate specific custom actions (comma-separated)");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  ca-generator                              # List available entities");
        Console.WriteLine("  ca-generator --entity Product             # Generate all files for Product");
        Console.WriteLine("  ca-generator --entity Order --overwrite   # Overwrite existing files");
        Console.WriteLine("  ca-generator --entity User --dry-run      # Preview without writing");
        Console.WriteLine("  ca-generator --entity Category --controller-only");
        Console.WriteLine("  ca-generator --entity Product --custom-actions GetActive,Activate");
        Console.WriteLine("  ca-generator --entity Order --actions-only # Only custom actions, no CRUD");
    }

    private string? FindDomainAssembly()
    {
        var searchPaths = new[]
        {
            "Domain.dll", // Current directory
            Path.Combine("src", "Domain", "bin", "Debug", "net8.0", "Domain.dll"),
            Path.Combine("src", "Domain", "bin", "Release", "net8.0", "Domain.dll"),
            Path.Combine("..", "..", "src", "Domain", "bin", "Debug", "net8.0", "Domain.dll"), // CodeGenerator directory
            Path.Combine("..", "..", "src", "Domain", "bin", "Release", "net8.0", "Domain.dll")
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }
}