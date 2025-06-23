using CodeGenerator.Models;

namespace CodeGenerator.Services;

public interface ITemplateEngine
{
    Task<string> ProcessTemplateAsync(string templateName, TemplateData data);
    Task<List<string>> GetAvailableTemplatesAsync();
    Task<bool> TemplateExistsAsync(string templateName);
}

public class TemplateEngine : ITemplateEngine
{
    private readonly string _templatesPath;

    public TemplateEngine()
    {
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
    }

    public async Task<string> ProcessTemplateAsync(string templateName, TemplateData data)
    {
        var templatePath = Path.Combine(_templatesPath, templateName);
        
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templatePath}");
        }

        var template = await File.ReadAllTextAsync(templatePath);
        return ProcessTemplate(template, data);
    }

    public async Task<List<string>> GetAvailableTemplatesAsync()
    {
        if (!Directory.Exists(_templatesPath))
        {
            return new List<string>();
        }

        var templates = Directory.GetFiles(_templatesPath, "*.template", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_templatesPath, f))
            .ToList();

        return await Task.FromResult(templates);
    }

    public async Task<bool> TemplateExistsAsync(string templateName)
    {
        var templatePath = Path.Combine(_templatesPath, templateName);
        return await Task.FromResult(File.Exists(templatePath));
    }

    private string ProcessTemplate(string template, TemplateData data)
    {
        var result = template;

        // Replace entity-specific placeholders
        result = result.Replace("{{EntityName}}", data.Entity.Name);
        result = result.Replace("{{EntityNameLower}}", data.Entity.Name.ToLowerInvariant());
        result = result.Replace("{{EntityNameCamel}}", ToCamelCase(data.Entity.Name));
        result = result.Replace("{{EntityNamePlural}}", data.Entity.PluralName);
        result = result.Replace("{{EntityNamePluralLower}}", data.Entity.PluralName.ToLowerInvariant());
        result = result.Replace("{{EntityNamePluralCamel}}", ToCamelCase(data.Entity.PluralName));

        // Replace namespace placeholders
        result = result.Replace("{{DomainNamespace}}", data.Options.Namespaces.Domain);
        result = result.Replace("{{ApplicationNamespace}}", data.Options.Namespaces.Application);
        result = result.Replace("{{InfrastructureNamespace}}", data.Options.Namespaces.Infrastructure);
        result = result.Replace("{{APINamespace}}", data.Options.Namespaces.API);

        // Replace ID property
        var idProperty = data.Entity.IdProperty;
        if (idProperty != null)
        {
            result = result.Replace("{{IdType}}", idProperty.Type);
            result = result.Replace("{{IdProperty}}", idProperty.Name);
        }

        // Process conditional blocks
        result = ProcessConditionalBlocks(result, data);

        // Process property loops
        result = ProcessPropertyLoops(result, data.Entity);

        // Process custom actions
        result = ProcessCustomActions(result, data);

        // Process custom data
        foreach (var kvp in data.AdditionalData)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
        }

        return result;
    }

    private string ProcessConditionalBlocks(string template, TemplateData data)
    {
        var result = template;

        // Process feature flags
        result = ProcessConditional(result, "HasCaching", data.Options.Features.AddCaching);
        result = ProcessConditional(result, "HasLogging", data.Options.Features.AddLogging);
        result = ProcessConditional(result, "HasValidation", data.Options.Features.AddValidation);
        result = ProcessConditional(result, "HasAudit", data.Options.Features.AddAudit);
        result = ProcessConditional(result, "HasPagination", data.Options.Features.AddPagination);
        result = ProcessConditional(result, "HasFiltering", data.Options.Features.AddFiltering);

        // Process entity-specific conditionals
        result = ProcessConditional(result, "HasAuditFields", data.Entity.HasAuditFields);
        result = ProcessConditional(result, "HasNavigationProperties", data.Entity.NavigationProperties.Any());
        result = ProcessConditional(result, "HasStringProperties", data.Entity.StringProperties.Any());

        return result;
    }

    private string ProcessConditional(string template, string conditionName, bool condition)
    {
        var startTag = $"{{{{#{conditionName}}}}}";
        var endTag = $"{{{{/{conditionName}}}}}";
        var elseTag = $"{{{{else {conditionName}}}}}";

        while (template.Contains(startTag))
        {
            var startIndex = template.IndexOf(startTag);
            var endIndex = template.IndexOf(endTag, startIndex);
            
            if (endIndex == -1) break;

            var content = template.Substring(startIndex + startTag.Length, endIndex - startIndex - startTag.Length);
            var elseIndex = content.IndexOf(elseTag);
            
            string replacement;
            if (condition)
            {
                replacement = elseIndex >= 0 ? content.Substring(0, elseIndex) : content;
            }
            else
            {
                replacement = elseIndex >= 0 ? content.Substring(elseIndex + elseTag.Length) : "";
            }

            template = template.Substring(0, startIndex) + replacement + template.Substring(endIndex + endTag.Length);
        }

        return template;
    }

    private string ProcessPropertyLoops(string template, EntityInfo entity)
    {
        // Process {{#Properties}}...{{/Properties}}
        template = ProcessPropertyLoop(template, "Properties", entity.Properties);
        template = ProcessPropertyLoop(template, "RequiredProperties", entity.RequiredProperties);
        template = ProcessPropertyLoop(template, "NavigationProperties", entity.NavigationProperties);
        template = ProcessPropertyLoop(template, "StringProperties", entity.StringProperties);
        template = ProcessPropertyLoop(template, "NumericProperties", entity.NumericProperties);
        template = ProcessPropertyLoop(template, "DateTimeProperties", entity.DateTimeProperties);

        return template;
    }

    private string ProcessPropertyLoop(string template, string loopName, List<Models.PropertyInfo> properties)
    {
        var startTag = $"{{{{#{loopName}}}}}";
        var endTag = $"{{{{/{loopName}}}}}";

        while (template.Contains(startTag))
        {
            var startIndex = template.IndexOf(startTag);
            var endIndex = template.IndexOf(endTag, startIndex);
            
            if (endIndex == -1) break;

            var loopTemplate = template.Substring(startIndex + startTag.Length, endIndex - startIndex - startTag.Length);
            var result = new List<string>();

            foreach (var property in properties)
            {
                var propertyContent = loopTemplate;
                propertyContent = propertyContent.Replace("{{PropertyName}}", property.Name);
                propertyContent = propertyContent.Replace("{{PropertyNameLower}}", property.Name.ToLowerInvariant());
                propertyContent = propertyContent.Replace("{{PropertyNameCamel}}", ToCamelCase(property.Name));
                propertyContent = propertyContent.Replace("{{PropertyType}}", property.Type);
                propertyContent = propertyContent.Replace("{{PropertyDisplayName}}", property.DisplayName);
                propertyContent = propertyContent.Replace("{{PropertyValidation}}", string.Join("\n    ", property.ValidationAttributes));
                propertyContent = propertyContent.Replace("{{PropertyMaxLength}}", property.MaxLength?.ToString() ?? "");
                propertyContent = propertyContent.Replace("{{PropertyRequired}}", property.IsRequired.ToString().ToLowerInvariant());
                propertyContent = propertyContent.Replace("{{PropertyNullable}}", property.IsNullable.ToString().ToLowerInvariant());
                
                result.Add(propertyContent);
            }

            var replacement = string.Join("", result);
            template = template.Substring(0, startIndex) + replacement + template.Substring(endIndex + endTag.Length);
        }

        return template;
    }

    private string ProcessCustomActions(string template, TemplateData data)
    {
        var startTag = "{{#CustomActions}}";
        var endTag = "{{/CustomActions}}";

        while (template.Contains(startTag))
        {
            var startIndex = template.IndexOf(startTag);
            var endIndex = template.IndexOf(endTag, startIndex);
            
            if (endIndex == -1) break;

            var actionTemplate = template.Substring(startIndex + startTag.Length, endIndex - startIndex - startTag.Length);
            var result = new List<string>();

            foreach (var action in data.CustomActions)
            {
                var actionContent = actionTemplate;
                
                // Replace action-specific placeholders
                actionContent = actionContent.Replace("{{Name}}", action.Name);
                actionContent = actionContent.Replace("{{Description}}", action.Description);
                actionContent = actionContent.Replace("{{HttpMethod}}", action.HttpMethod);
                actionContent = actionContent.Replace("{{Route}}", action.Route);
                actionContent = actionContent.Replace("{{CacheKeyPattern}}", action.CacheKeyPattern);
                
                // Process action conditionals
                actionContent = ProcessConditional(actionContent, "AddCaching", action.AddCaching);
                actionContent = ProcessConditional(actionContent, "AddLogging", action.AddLogging);
                actionContent = ProcessConditional(actionContent, "AddAudit", action.AddAudit);
                actionContent = ProcessConditional(actionContent, "AddPagination", action.AddPagination);
                
                // Process return type conditionals
                actionContent = ProcessConditional(actionContent, "IsVoid", action.ReturnType == "Void");
                actionContent = ProcessConditional(actionContent, "IsSingle", action.ReturnType == "Single");
                actionContent = ProcessConditional(actionContent, "IsList", action.ReturnType == "List");
                actionContent = ProcessConditional(actionContent, "IsCustom", action.ReturnType == "Custom");
                
                // Generate return type template
                var returnTypeTemplate = GenerateReturnTypeTemplate(action, data.Entity);
                actionContent = actionContent.Replace("{{ReturnTypeTemplate}}", returnTypeTemplate);
                
                // Process parameters
                actionContent = ProcessActionParameters(actionContent, action);
                
                result.Add(actionContent);
            }

            var replacement = string.Join("\n", result);
            template = template.Substring(0, startIndex) + replacement + template.Substring(endIndex + endTag.Length);
        }

        return template;
    }

    private string GenerateReturnTypeTemplate(CustomAction action, EntityInfo entity)
    {
        return action.ReturnType switch
        {
            "Void" => "Result",
            "Single" => $"Result<{entity.Name}ResponseDto>",
            "List" when action.AddPagination => $"Result<(IEnumerable<{entity.Name}ResponseDto>, int)>",
            "List" => $"Result<IEnumerable<{entity.Name}ResponseDto>>",
            "Custom" => $"Result<{action.CustomReturnType}>",
            _ => "Result"
        };
    }

    private string ProcessActionParameters(string template, CustomAction action)
    {
        var startTag = "{{#Parameters}}";
        var endTag = "{{/Parameters}}";

        while (template.Contains(startTag))
        {
            var startIndex = template.IndexOf(startTag);
            var endIndex = template.IndexOf(endTag, startIndex);
            
            if (endIndex == -1) break;

            var paramTemplate = template.Substring(startIndex + startTag.Length, endIndex - startIndex - startTag.Length);
            var result = new List<string>();

            for (int i = 0; i < action.Parameters.Count; i++)
            {
                var param = action.Parameters[i];
                var paramContent = paramTemplate;
                
                paramContent = paramContent.Replace("{{Name}}", param.Name);
                paramContent = paramContent.Replace("{{Type}}", param.Type);
                paramContent = paramContent.Replace("{{DefaultValue}}", param.DefaultValue);
                
                // Process parameter conditionals
                paramContent = ProcessConditional(paramContent, "FromRoute", param.FromRoute);
                paramContent = ProcessConditional(paramContent, "FromQuery", param.FromQuery);
                paramContent = ProcessConditional(paramContent, "FromBody", param.FromBody);
                paramContent = ProcessConditional(paramContent, "HasNext", i < action.Parameters.Count - 1);
                
                result.Add(paramContent);
            }

            var replacement = string.Join("", result);
            template = template.Substring(0, startIndex) + replacement + template.Substring(endIndex + endTag.Length);
        }

        return template;
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            return input;

        return char.ToLowerInvariant(input[0]) + input[1..];
    }
}