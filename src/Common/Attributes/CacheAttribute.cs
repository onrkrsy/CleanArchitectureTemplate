namespace Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CacheAttribute : Attribute
{
    public string? KeyPrefix { get; set; }
    public int ExpirationMinutes { get; set; } = 30;
    public bool SlidingExpiration { get; set; } = true;
    public string[]? KeyParameters { get; set; }
    public bool IgnoreNullValues { get; set; } = true;
    public string? Tags { get; set; }
    
    public CacheAttribute()
    {
    }

    public CacheAttribute(int expirationMinutes)
    {
        ExpirationMinutes = expirationMinutes;
    }

    public CacheAttribute(string keyPrefix, int expirationMinutes = 30)
    {
        KeyPrefix = keyPrefix;
        ExpirationMinutes = expirationMinutes;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class CacheEvictAttribute : Attribute
{
    public string[]? KeyPatterns { get; set; }
    public string[]? Tags { get; set; }
    public bool EvictAll { get; set; } = false;
    public bool BeforeInvocation { get; set; } = false;

    public CacheEvictAttribute()
    {
    }

    public CacheEvictAttribute(params string[] keyPatterns)
    {
        KeyPatterns = keyPatterns;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class CacheUpdateAttribute : Attribute
{
    public string? KeyPrefix { get; set; }
    public string[]? KeyParameters { get; set; }
    public int ExpirationMinutes { get; set; } = 30;

    public CacheUpdateAttribute()
    {
    }

    public CacheUpdateAttribute(string keyPrefix)
    {
        KeyPrefix = keyPrefix;
    }
}