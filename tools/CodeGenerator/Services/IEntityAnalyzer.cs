using CodeGenerator.Models;
using System.Reflection;

namespace CodeGenerator.Services;

public interface IEntityAnalyzer
{
    Task<List<EntityInfo>> DiscoverEntitiesAsync(string assemblyPath);
    Task<EntityInfo> AnalyzeEntityAsync(Type entityType);
    Task<List<string>> GetAvailableEntitiesAsync(string assemblyPath);
}

public class EntityAnalyzer : IEntityAnalyzer
{
    public async Task<List<EntityInfo>> DiscoverEntitiesAsync(string assemblyPath)
    {
        var entities = new List<EntityInfo>();
        
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && IsEntity(t))
                .ToList();

            foreach (var entityType in entityTypes)
            {
                var entityInfo = await AnalyzeEntityAsync(entityType);
                entities.Add(entityInfo);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover entities in assembly: {assemblyPath}", ex);
        }

        return entities;
    }

    public async Task<EntityInfo> AnalyzeEntityAsync(Type entityType)
    {
        var entityInfo = new EntityInfo
        {
            Name = entityType.Name,
            PluralName = GetPluralName(entityType.Name),
            Namespace = entityType.Namespace ?? "Domain.Entities",
            EntityType = entityType
        };

        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        foreach (var prop in properties)
        {
            var propertyInfo = AnalyzeProperty(prop);
            entityInfo.Properties.Add(propertyInfo);

            if (propertyInfo.IsId)
                entityInfo.IdProperty = propertyInfo;

            if (propertyInfo.IsNavigation)
                entityInfo.NavigationProperties.Add(propertyInfo);

            if (propertyInfo.IsRequired)
                entityInfo.RequiredProperties.Add(propertyInfo);

            if (propertyInfo.Type == "string")
                entityInfo.StringProperties.Add(propertyInfo);
            else if (IsNumericType(propertyInfo.Type))
                entityInfo.NumericProperties.Add(propertyInfo);
            else if (propertyInfo.Type.Contains("DateTime"))
                entityInfo.DateTimeProperties.Add(propertyInfo);

            if (propertyInfo.IsAuditField)
                entityInfo.HasAuditFields = true;
        }

        return await Task.FromResult(entityInfo);
    }

    public async Task<List<string>> GetAvailableEntitiesAsync(string assemblyPath)
    {
        var entities = await DiscoverEntitiesAsync(assemblyPath);
        return entities.Select(e => e.Name).OrderBy(name => name).ToList();
    }

    private bool IsEntity(Type type)
    {
        // Check if type has properties that look like entity properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        // Must have at least one property
        if (!properties.Any()) return false;

        // Should have an Id property or be in Entities namespace
        var hasIdProperty = properties.Any(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        var isInEntitiesNamespace = type.Namespace?.Contains("Entities") == true ||
                                   type.Namespace?.Contains("Domain") == true;

        return hasIdProperty || isInEntitiesNamespace;
    }

    private Models.PropertyInfo AnalyzeProperty(System.Reflection.PropertyInfo prop)
    {
        var propertyInfo = new Models.PropertyInfo
        {
            Name = prop.Name,
            Type = GetPropertyTypeName(prop.PropertyType),
            DisplayName = GetDisplayName(prop.Name),
            IsNullable = IsNullableType(prop.PropertyType),
            IsCollection = IsCollectionType(prop.PropertyType),
            IsNavigation = IsNavigationProperty(prop),
            IsId = IsIdProperty(prop),
            IsAuditField = IsAuditField(prop.Name)
        };

        // Check for validation attributes
        var attributes = prop.GetCustomAttributes(true);
        foreach (var attr in attributes)
        {
            if (attr is System.ComponentModel.DataAnnotations.RequiredAttribute)
            {
                propertyInfo.IsRequired = true;
                propertyInfo.ValidationAttributes.Add("[Required]");
            }
            else if (attr is System.ComponentModel.DataAnnotations.StringLengthAttribute stringLengthAttr)
            {
                propertyInfo.MaxLength = stringLengthAttr.MaximumLength;
                propertyInfo.ValidationAttributes.Add($"[StringLength({stringLengthAttr.MaximumLength})]");
            }
            else if (attr is System.ComponentModel.DataAnnotations.MaxLengthAttribute maxLengthAttr)
            {
                propertyInfo.MaxLength = maxLengthAttr.Length;
                propertyInfo.ValidationAttributes.Add($"[MaxLength({maxLengthAttr.Length})]");
            }
        }

        return propertyInfo;
    }

    private string GetPropertyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return GetPropertyTypeName(type.GetGenericArguments()[0]) + "?";
        }

        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Split('`')[0];
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetPropertyTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }

        return type.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Boolean" => "bool",
            "String" => "string",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "DateTime" => "DateTime",
            "Guid" => "Guid",
            _ => type.Name
        };
    }

    private bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private bool IsCollectionType(Type type)
    {
        return type != typeof(string) && 
               (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>));
    }

    private bool IsNavigationProperty(System.Reflection.PropertyInfo prop)
    {
        // Simple heuristic: if it's a complex type or collection, it's likely a navigation property
        var type = prop.PropertyType;
        
        if (IsCollectionType(type))
            return true;

        // Check if it's a custom class type (not primitive)
        return type.IsClass && 
               type != typeof(string) && 
               !type.IsPrimitive && 
               type.Namespace != "System";
    }

    private bool IsIdProperty(System.Reflection.PropertyInfo prop)
    {
        return prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
               prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAuditField(string propertyName)
    {
        var auditFields = new[] { "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy" };
        return auditFields.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsNumericType(string typeName)
    {
        var numericTypes = new[] { "int", "long", "decimal", "double", "float", "short", "byte" };
        return numericTypes.Contains(typeName.Replace("?", ""), StringComparer.OrdinalIgnoreCase);
    }

    private string GetDisplayName(string propertyName)
    {
        // Convert PascalCase to Display Name
        return System.Text.RegularExpressions.Regex.Replace(propertyName, @"(\B[A-Z])", " $1");
    }

    private string GetPluralName(string entityName)
    {
        // Simple pluralization rules
        if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("sh", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("z", StringComparison.OrdinalIgnoreCase))
        {
            return entityName + "es";
        }
        
        if (entityName.EndsWith("y", StringComparison.OrdinalIgnoreCase) && 
            entityName.Length > 1 && 
            !"aeiou".Contains(entityName[^2], StringComparison.OrdinalIgnoreCase))
        {
            return entityName[..^1] + "ies";
        }
        
        if (entityName.EndsWith("f", StringComparison.OrdinalIgnoreCase))
        {
            return entityName[..^1] + "ves";
        }
        
        if (entityName.EndsWith("fe", StringComparison.OrdinalIgnoreCase))
        {
            return entityName[..^2] + "ves";
        }

        return entityName + "s";
    }
}