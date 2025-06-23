using Microsoft.Extensions.Logging;

namespace Common.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LogAttribute : Attribute
{
    public bool LogRequest { get; set; } = true;
    public bool LogResponse { get; set; } = true;
    public bool LogParameters { get; set; } = true;
    public bool LogExecutionTime { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string[]? SensitiveParameters { get; set; }
    public string[]? IgnoreParameters { get; set; }
    public bool LogOnlyOnError { get; set; } = false;
    public int MaxResponseLength { get; set; } = 1000;
    public string? CustomMessage { get; set; }

    public LogAttribute()
    {
    }

    public LogAttribute(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    public LogAttribute(bool logRequest, bool logResponse)
    {
        LogRequest = logRequest;
        LogResponse = logResponse;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LogPerformanceAttribute : Attribute
{
    public int SlowExecutionThresholdMs { get; set; } = 1000;
    public LogLevel SlowExecutionLogLevel { get; set; } = LogLevel.Warning;
    public bool LogMemoryUsage { get; set; } = false;
    public bool LogCpuUsage { get; set; } = false;

    public LogPerformanceAttribute()
    {
    }

    public LogPerformanceAttribute(int slowExecutionThresholdMs)
    {
        SlowExecutionThresholdMs = slowExecutionThresholdMs;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class LogAuditAttribute : Attribute
{
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public bool LogUserId { get; set; } = true;
    public bool LogUserAgent { get; set; } = true;
    public bool LogIpAddress { get; set; } = true;
    public string[]? AuditParameters { get; set; }

    public LogAuditAttribute()
    {
    }

    public LogAuditAttribute(string action, string resource)
    {
        Action = action;
        Resource = resource;
    }
}