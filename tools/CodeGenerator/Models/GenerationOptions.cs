namespace CodeGenerator.Models;

public class GenerationOptions
{
    public string EntityName { get; set; } = string.Empty;
    public string ProjectRoot { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public NamespaceOptions Namespaces { get; set; } = new();
    public FeatureOptions Features { get; set; } = new();
    public List<CustomAction> CustomActions { get; set; } = new();
    public bool OverwriteExisting { get; set; } = false;
    public bool DryRun { get; set; } = false;
    public bool Verbose { get; set; } = false;
}

public class NamespaceOptions
{
    public string Domain { get; set; } = "Domain.Entities";
    public string Application { get; set; } = "Application";
    public string Infrastructure { get; set; } = "Infrastructure";
    public string API { get; set; } = "API.Controllers";
}

public class FeatureOptions
{
    public bool GenerateRepository { get; set; } = true;
    public bool GenerateService { get; set; } = true;
    public bool GenerateController { get; set; } = true;
    public bool GenerateDTOs { get; set; } = true;
    public bool GenerateMapper { get; set; } = true;
    public bool AddCaching { get; set; } = true;
    public bool AddLogging { get; set; } = true;
    public bool AddValidation { get; set; } = true;
    public bool AddAudit { get; set; } = true;
    public bool AddPagination { get; set; } = true;
    public bool AddFiltering { get; set; } = true;
    public bool AddSorting { get; set; } = true;
}

public class TemplateData
{
    public EntityInfo Entity { get; set; } = null!;
    public GenerationOptions Options { get; set; } = null!;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public List<CustomAction> CustomActions { get; set; } = new();
}

public class CustomAction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public string Route { get; set; } = string.Empty;
    public string ReturnType { get; set; } = "Single"; // Single, List, Void, Custom
    public List<ActionParameter> Parameters { get; set; } = new();
    public bool AddCaching { get; set; } = false;
    public bool AddLogging { get; set; } = false;
    public bool AddAudit { get; set; } = false;
    public bool AddPagination { get; set; } = false;
    public string CacheKeyPattern { get; set; } = string.Empty;
    public string CustomReturnType { get; set; } = string.Empty;
}

public class ActionParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string DefaultValue { get; set; } = string.Empty;
    public bool FromRoute { get; set; } = false;
    public bool FromQuery { get; set; } = false;
    public bool FromBody { get; set; } = false;
}