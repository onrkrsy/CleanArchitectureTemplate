using CodeGenerator.Models;
using Microsoft.Extensions.Logging;

namespace CodeGenerator.Services;

public interface ICodeGenerator
{
    Task<List<GeneratedFile>> GenerateAllAsync(EntityInfo entity, GenerationOptions options);
    Task<GeneratedFile> GenerateFileAsync(string templateName, EntityInfo entity, GenerationOptions options);
}

public interface IFileGenerator
{
    Task WriteFilesAsync(List<GeneratedFile> files, bool overwrite = false);
    Task<string> GetOutputPathAsync(string fileName, string category, GenerationOptions options);
    Task EnsureDirectoryExistsAsync(string filePath);
}

public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Overwrite { get; set; } = false;
}

public class CodeGeneratorImpl : ICodeGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<CodeGeneratorImpl> _logger;

    public CodeGeneratorImpl(ITemplateEngine templateEngine, ILogger<CodeGeneratorImpl> logger)
    {
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task<List<GeneratedFile>> GenerateAllAsync(EntityInfo entity, GenerationOptions options)
    {
        var files = new List<GeneratedFile>();

        try
        {
            // Generate DTOs
            if (options.Features.GenerateDTOs)
            {
                files.Add(await GenerateFileAsync("CreateDto.template", entity, options));
                files.Add(await GenerateFileAsync("UpdateDto.template", entity, options));
                files.Add(await GenerateFileAsync("ResponseDto.template", entity, options));
            }

            // Generate Repository
            if (options.Features.GenerateRepository)
            {
                files.Add(await GenerateFileAsync("RepositoryInterface.template", entity, options));
                files.Add(await GenerateFileAsync("Repository.template", entity, options));
            }

            // Generate Service
            if (options.Features.GenerateService)
            {
                files.Add(await GenerateFileAsync("ServiceInterface.template", entity, options));
                files.Add(await GenerateFileAsync("Service.template", entity, options));
            }

            // Generate Controller
            if (options.Features.GenerateController)
            {
                files.Add(await GenerateFileAsync("Controller.template", entity, options));
            }

            // Generate Mapper
            if (options.Features.GenerateMapper)
            {
                files.Add(await GenerateFileAsync("MappingProfile.template", entity, options));
            }

            _logger.LogInformation("Generated {Count} files for entity {EntityName}", files.Count, entity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating files for entity {EntityName}", entity.Name);
            throw;
        }

        return files;
    }

    public async Task<GeneratedFile> GenerateFileAsync(string templateName, EntityInfo entity, GenerationOptions options)
    {
        var templateData = new TemplateData
        {
            Entity = entity,
            Options = options,
            CustomActions = options.CustomActions
        };

        var content = await _templateEngine.ProcessTemplateAsync(templateName, templateData);
        
        var fileName = GetFileName(templateName, entity);
        var category = GetFileCategory(templateName);

        return new GeneratedFile
        {
            FileName = fileName,
            Content = content,
            Category = category,
            Overwrite = options.OverwriteExisting
        };
    }

    private string GetFileName(string templateName, EntityInfo entity)
    {
        return templateName switch
        {
            "CreateDto.template" => $"Create{entity.Name}Dto.cs",
            "UpdateDto.template" => $"Update{entity.Name}Dto.cs",
            "ResponseDto.template" => $"{entity.Name}ResponseDto.cs",
            "RepositoryInterface.template" => $"I{entity.Name}Repository.cs",
            "Repository.template" => $"{entity.Name}Repository.cs",
            "ServiceInterface.template" => $"I{entity.Name}Service.cs",
            "Service.template" => $"{entity.Name}Service.cs",
            "Controller.template" => $"{entity.PluralName}Controller.cs",
            "MappingProfile.template" => $"{entity.Name}MappingProfile.cs",
            _ => $"{entity.Name}_{templateName.Replace(".template", ".cs")}"
        };
    }

    private string GetFileCategory(string templateName)
    {
        return templateName switch
        {
            "CreateDto.template" or "UpdateDto.template" or "ResponseDto.template" => "DTOs",
            "RepositoryInterface.template" => "Interfaces",
            "Repository.template" => "Repositories",
            "ServiceInterface.template" => "Services",
            "Service.template" => "Services",
            "Controller.template" => "Controllers",
            "MappingProfile.template" => "Mappings",
            _ => "Generated"
        };
    }
}

public class FileGenerator : IFileGenerator
{
    private readonly ILogger<FileGenerator> _logger;

    public FileGenerator(ILogger<FileGenerator> logger)
    {
        _logger = logger;
    }

    public async Task WriteFilesAsync(List<GeneratedFile> files, bool overwrite = false)
    {
        foreach (var file in files)
        {
            try
            {
                await EnsureDirectoryExistsAsync(file.FilePath);

                if (!overwrite && File.Exists(file.FilePath))
                {
                    _logger.LogWarning("File already exists, skipping: {FilePath}", file.FilePath);
                    continue;
                }

                await File.WriteAllTextAsync(file.FilePath, file.Content);
                _logger.LogInformation("Generated file: {FilePath}", file.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file: {FilePath}", file.FilePath);
                throw;
            }
        }
    }

    public async Task<string> GetOutputPathAsync(string fileName, string category, GenerationOptions options)
    {
        var basePath = category switch
        {
            "DTOs" => Path.Combine(options.ProjectRoot, "Application", "DTOs"),
            "Interfaces" => Path.Combine(options.ProjectRoot, "Application", "Interfaces"),
            "Services" => category == "ServiceInterface" ? 
                         Path.Combine(options.ProjectRoot, "Application", "Services") :
                         Path.Combine(options.ProjectRoot, "Infrastructure", "Services"),
            "Repositories" => Path.Combine(options.ProjectRoot, "Infrastructure", "Repositories"),
            "Controllers" => Path.Combine(options.ProjectRoot, "API", "Controllers"),
            "Mappings" => Path.Combine(options.ProjectRoot, "API", "Mappings"),
            _ => Path.Combine(options.ProjectRoot, "Generated", category)
        };

        return await Task.FromResult(Path.Combine(basePath, fileName));
    }

    public async Task EnsureDirectoryExistsAsync(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created directory: {Directory}", directory);
        }
        await Task.CompletedTask;
    }
}