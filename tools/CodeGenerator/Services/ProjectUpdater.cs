using CodeGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CodeGenerator.Services;

public interface IProjectUpdater
{
    Task UpdateProgramCsAsync(EntityInfo entity, GenerationOptions options);
    Task UpdateUnitOfWorkAsync(EntityInfo entity, GenerationOptions options);
    Task UpdateAutoMapperAsync(EntityInfo entity, GenerationOptions options);
    Task<bool> BackupFileAsync(string filePath);
}

public class ProjectUpdater : IProjectUpdater
{
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<ProjectUpdater> _logger;

    public ProjectUpdater(ITemplateEngine templateEngine, ILogger<ProjectUpdater> logger)
    {
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task UpdateProgramCsAsync(EntityInfo entity, GenerationOptions options)
    {
        var programPath = Path.Combine(options.ProjectRoot, "API", "Program.cs");
        
        if (!File.Exists(programPath))
        {
            _logger.LogWarning("Program.cs not found at: {Path}", programPath);
            return;
        }

        try
        {
            await BackupFileAsync(programPath);
            
            var content = await File.ReadAllTextAsync(programPath);
            var templateData = new TemplateData { Entity = entity, Options = options };
            templateData.AdditionalData["HasService"] = options.Features.GenerateService;

            var serviceRegistration = await _templateEngine.ProcessTemplateAsync("ProgramUpdate.template", templateData);

            // Find the right place to insert service registrations
            var pattern = @"(// Add UserService with interceptors for caching and logging\s*builder\.Services\.AddFullInterception<IUserService, UserService>\(\);)";
            var replacement = $"$1\n\n{serviceRegistration}";

            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
            }
            else
            {
                // Fallback: Add before health checks
                var fallbackPattern = @"(// Health Checks)";
                var fallbackReplacement = $"{serviceRegistration}\n\n$1";
                content = Regex.Replace(content, fallbackPattern, fallbackReplacement);
            }

            await File.WriteAllTextAsync(programPath, content);
            _logger.LogInformation("Updated Program.cs with {EntityName} service registrations", entity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Program.cs");
            throw;
        }
    }

    public async Task UpdateUnitOfWorkAsync(EntityInfo entity, GenerationOptions options)
    {
        var interfacePath = Path.Combine(options.ProjectRoot, "Application", "Interfaces", "IUnitOfWork.cs");
        var implementationPath = Path.Combine(options.ProjectRoot, "Infrastructure", "Repositories", "UnitOfWork.cs");

        await UpdateUnitOfWorkInterfaceAsync(interfacePath, entity, options);
        await UpdateUnitOfWorkImplementationAsync(implementationPath, entity, options);
    }

    private async Task UpdateUnitOfWorkInterfaceAsync(string filePath, EntityInfo entity, GenerationOptions options)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("IUnitOfWork.cs not found at: {Path}", filePath);
            return;
        }

        try
        {
            await BackupFileAsync(filePath);
            
            var content = await File.ReadAllTextAsync(filePath);
            var templateData = new TemplateData { Entity = entity, Options = options };
            var repositoryProperty = await _templateEngine.ProcessTemplateAsync("UnitOfWorkUpdate.template", templateData);

            // Add the repository property before the closing brace
            var pattern = @"(\s*IUserRepository Users \{ get; \}\s*)(\})";
            var replacement = $"$1{repositoryProperty}\n$2";

            content = Regex.Replace(content, pattern, replacement);

            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("Updated IUnitOfWork interface with {EntityName} repository", entity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IUnitOfWork interface");
            throw;
        }
    }

    private async Task UpdateUnitOfWorkImplementationAsync(string filePath, EntityInfo entity, GenerationOptions options)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("UnitOfWork.cs not found at: {Path}", filePath);
            return;
        }

        try
        {
            await BackupFileAsync(filePath);
            
            var content = await File.ReadAllTextAsync(filePath);
            var templateData = new TemplateData { Entity = entity, Options = options };
            var implementation = await _templateEngine.ProcessTemplateAsync("UnitOfWorkImplementation.template", templateData);

            // Add the repository implementation before the closing brace of the class
            var pattern = @"(\s*public I\w+Repository \w+ \{\s*get\s*\{\s*return.*?\}\s*\}\s*)(\})";
            var replacement = $"$1\n{implementation}\n$2";

            content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);

            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("Updated UnitOfWork implementation with {EntityName} repository", entity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating UnitOfWork implementation");
            throw;
        }
    }

    public async Task UpdateAutoMapperAsync(EntityInfo entity, GenerationOptions options)
    {
        var programPath = Path.Combine(options.ProjectRoot, "API", "Program.cs");
        
        if (!File.Exists(programPath))
        {
            _logger.LogWarning("Program.cs not found at: {Path}", programPath);
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(programPath);
            var templateData = new TemplateData { Entity = entity, Options = options };
            var mapperRegistration = await _templateEngine.ProcessTemplateAsync("AutoMapperUpdate.template", templateData);

            // Find AutoMapper configuration and add the new profile
            var pattern = @"(builder\.Services\.AddAutoMapper\(typeof\(UserMappingProfile\))(\);)";
            var replacement = $"$1,\n    {mapperRegistration.Trim()}$2";

            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
                await File.WriteAllTextAsync(programPath, content);
                _logger.LogInformation("Updated AutoMapper configuration with {EntityName}MappingProfile", entity.Name);
            }
            else
            {
                _logger.LogWarning("AutoMapper configuration not found in expected format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AutoMapper configuration");
            throw;
        }
    }

    public async Task<bool> BackupFileAsync(string filePath)
    {
        try
        {
            var backupPath = $"{filePath}.backup.{DateTime.Now:yyyyMMdd_HHmmss}";
            var content = await File.ReadAllTextAsync(filePath);
            await File.WriteAllTextAsync(backupPath, content);
            _logger.LogDebug("Created backup: {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup for: {FilePath}", filePath);
            return false;
        }
    }
}