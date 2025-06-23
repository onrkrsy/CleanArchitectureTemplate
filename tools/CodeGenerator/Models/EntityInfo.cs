using System.Reflection;

namespace CodeGenerator.Models;

public class EntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string PluralName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public Type EntityType { get; set; } = null!;
    public List<PropertyInfo> Properties { get; set; } = new();
    public PropertyInfo? IdProperty { get; set; }
    public List<PropertyInfo> NavigationProperties { get; set; } = new();
    public List<PropertyInfo> RequiredProperties { get; set; } = new();
    public List<PropertyInfo> StringProperties { get; set; } = new();
    public List<PropertyInfo> NumericProperties { get; set; } = new();
    public List<PropertyInfo> DateTimeProperties { get; set; } = new();
    public bool HasAuditFields { get; set; }
}

public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; }
    public bool IsCollection { get; set; }
    public bool IsNavigation { get; set; }
    public bool IsId { get; set; }
    public bool IsAuditField { get; set; }
    public int? MaxLength { get; set; }
    public string? DefaultValue { get; set; }
    public List<string> ValidationAttributes { get; set; } = new();
}