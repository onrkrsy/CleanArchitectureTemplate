using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Castle.DynamicProxy;
using Common.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Interceptors;

public class LoggingInterceptor : IInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger, IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var logAttribute = method.GetCustomAttribute<LogAttribute>() ?? 
                          method.DeclaringType?.GetCustomAttribute<LogAttribute>();
        var performanceAttribute = method.GetCustomAttribute<LogPerformanceAttribute>() ?? 
                                  method.DeclaringType?.GetCustomAttribute<LogPerformanceAttribute>();
        var auditAttribute = method.GetCustomAttribute<LogAuditAttribute>();

        if (logAttribute == null && performanceAttribute == null && auditAttribute == null)
        {
            invocation.Proceed();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var methodName = $"{invocation.TargetType.Name}.{invocation.Method.Name}";
        Exception? exception = null;

        try
        {
            LogMethodEntry(logAttribute, auditAttribute, invocation, methodName);
            
            invocation.Proceed();
            
            stopwatch.Stop();
            
            LogMethodExit(logAttribute, invocation, methodName, stopwatch.ElapsedMilliseconds);
            LogPerformance(performanceAttribute, methodName, stopwatch.ElapsedMilliseconds);
            LogAudit(auditAttribute, invocation, methodName, true);
        }
        catch (Exception ex)
        {
            exception = ex;
            stopwatch.Stop();
            
            LogException(logAttribute, ex, methodName, stopwatch.ElapsedMilliseconds);
            LogAudit(auditAttribute, invocation, methodName, false, ex);
            
            throw;
        }
    }

    private void LogMethodEntry(LogAttribute? logAttribute, LogAuditAttribute? auditAttribute, 
        IInvocation invocation, string methodName)
    {
        if (logAttribute?.LogRequest != true && auditAttribute == null) return;

        var logLevel = logAttribute?.LogLevel ?? LogLevel.Information;
        var message = new StringBuilder();
        
        message.AppendLine($"Executing method: {methodName}");

        if (logAttribute?.LogParameters == true)
        {
            var parameters = BuildParametersLog(invocation, logAttribute.SensitiveParameters, logAttribute.IgnoreParameters);
            if (!string.IsNullOrEmpty(parameters))
            {
                message.AppendLine($"Parameters: {parameters}");
            }
        }

        if (auditAttribute != null)
        {
            var auditInfo = BuildAuditInfo(auditAttribute, invocation);
            message.AppendLine($"Audit: {auditInfo}");
        }

        var userInfo = GetUserInfo();
        if (!string.IsNullOrEmpty(userInfo))
        {
            message.AppendLine($"User: {userInfo}");
        }

        _logger.Log(logLevel, "{Message}", message.ToString().TrimEnd());
    }

    private void LogMethodExit(LogAttribute? logAttribute, IInvocation invocation, 
        string methodName, long elapsedMs)
    {
        if (logAttribute?.LogResponse != true) return;
        if (logAttribute.LogOnlyOnError) return;

        var logLevel = logAttribute.LogLevel;
        var message = new StringBuilder();
        
        message.AppendLine($"Completed method: {methodName}");
        
        if (logAttribute.LogExecutionTime)
        {
            message.AppendLine($"Execution time: {elapsedMs}ms");
        }

        if (logAttribute.LogResponse && invocation.ReturnValue != null)
        {
            var response = SerializeResponse(invocation.ReturnValue, logAttribute.MaxResponseLength);
            message.AppendLine($"Response: {response}");
        }

        _logger.Log(logLevel, "{Message}", message.ToString().TrimEnd());
    }

    private void LogException(LogAttribute? logAttribute, Exception exception, string methodName, long elapsedMs)
    {
        var message = new StringBuilder();
        message.AppendLine($"Exception in method: {methodName}");
        message.AppendLine($"Execution time: {elapsedMs}ms");
        message.AppendLine($"Exception: {exception.Message}");
        
        if (!string.IsNullOrEmpty(logAttribute?.CustomMessage))
        {
            message.AppendLine($"Custom: {logAttribute.CustomMessage}");
        }

        _logger.LogError(exception, "{Message}", message.ToString().TrimEnd());
    }

    private void LogPerformance(LogPerformanceAttribute? performanceAttribute, string methodName, long elapsedMs)
    {
        if (performanceAttribute == null) return;

        if (elapsedMs >= performanceAttribute.SlowExecutionThresholdMs)
        {
            var message = new StringBuilder();
            message.AppendLine($"Slow execution detected: {methodName}");
            message.AppendLine($"Execution time: {elapsedMs}ms (threshold: {performanceAttribute.SlowExecutionThresholdMs}ms)");

            if (performanceAttribute.LogMemoryUsage)
            {
                var memoryUsage = GC.GetTotalMemory(false);
                message.AppendLine($"Memory usage: {memoryUsage / 1024 / 1024}MB");
            }

            if (performanceAttribute.LogCpuUsage)
            {
                var process = Process.GetCurrentProcess();
                message.AppendLine($"CPU time: {process.TotalProcessorTime.TotalMilliseconds}ms");
            }

            _logger.Log(performanceAttribute.SlowExecutionLogLevel, "{Message}", message.ToString().TrimEnd());
        }
        else
        {
            _logger.LogDebug("Method {MethodName} executed in {ElapsedMs}ms", methodName, elapsedMs);
        }
    }

    private void LogAudit(LogAuditAttribute? auditAttribute, IInvocation invocation, 
        string methodName, bool success, Exception? exception = null)
    {
        if (auditAttribute == null) return;

        var message = new StringBuilder();
        message.AppendLine($"Audit log: {auditAttribute.Action} on {auditAttribute.Resource}");
        message.AppendLine($"Method: {methodName}");
        message.AppendLine($"Success: {success}");
        message.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        if (auditAttribute.LogUserId)
        {
            var userId = GetUserId();
            message.AppendLine($"UserId: {userId ?? "Anonymous"}");
        }

        if (auditAttribute.LogIpAddress)
        {
            var ipAddress = GetClientIpAddress();
            message.AppendLine($"IP Address: {ipAddress ?? "Unknown"}");
        }

        if (auditAttribute.LogUserAgent)
        {
            var userAgent = GetUserAgent();
            message.AppendLine($"User Agent: {userAgent ?? "Unknown"}");
        }

        if (auditAttribute.AuditParameters != null)
        {
            var auditParams = BuildAuditParameters(invocation, auditAttribute.AuditParameters);
            if (!string.IsNullOrEmpty(auditParams))
            {
                message.AppendLine($"Audit Parameters: {auditParams}");
            }
        }

        if (!success && exception != null)
        {
            message.AppendLine($"Error: {exception.Message}");
        }

        _logger.LogInformation("{Message}", message.ToString().TrimEnd());
    }

    private string BuildParametersLog(IInvocation invocation, string[]? sensitiveParams, string[]? ignoreParams)
    {
        var parameters = invocation.Method.GetParameters();
        var arguments = invocation.Arguments;
        var paramList = new List<string>();

        for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            var paramName = parameters[i].Name ?? $"param{i}";
            
            if (ignoreParams?.Contains(paramName) == true)
                continue;

            var paramValue = arguments[i];
            
            if (sensitiveParams?.Contains(paramName) == true)
            {
                paramList.Add($"{paramName}: [SENSITIVE]");
            }
            else
            {
                var value = SerializeParameter(paramValue);
                paramList.Add($"{paramName}: {value}");
            }
        }

        return string.Join(", ", paramList);
    }

    private string BuildAuditParameters(IInvocation invocation, string[] auditParameters)
    {
        var parameters = invocation.Method.GetParameters();
        var arguments = invocation.Arguments;
        var auditParams = new List<string>();

        for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            var paramName = parameters[i].Name ?? $"param{i}";
            
            if (auditParameters.Contains(paramName))
            {
                var paramValue = arguments[i];
                var value = SerializeParameter(paramValue);
                auditParams.Add($"{paramName}: {value}");
            }
        }

        return string.Join(", ", auditParams);
    }

    private string BuildAuditInfo(LogAuditAttribute auditAttribute, IInvocation invocation)
    {
        var info = new StringBuilder();
        info.Append($"Action: {auditAttribute.Action}");
        info.Append($", Resource: {auditAttribute.Resource}");
        
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            info.Append($", UserId: {userId}");
        }

        return info.ToString();
    }

    private string SerializeParameter(object? parameter)
    {
        if (parameter == null) return "null";
        
        if (parameter is string str)
        {
            return str.Length > 100 ? $"{str[..100]}..." : str;
        }

        if (parameter is int or long or bool or DateTime or Guid)
        {
            return parameter.ToString() ?? "null";
        }

        try
        {
            var json = JsonSerializer.Serialize(parameter);
            return json.Length > 200 ? $"{json[..200]}..." : json;
        }
        catch
        {
            return $"[{parameter.GetType().Name}]";
        }
    }

    private string SerializeResponse(object response, int maxLength)
    {
        try
        {
            var json = JsonSerializer.Serialize(response);
            return json.Length > maxLength ? $"{json[..maxLength]}..." : json;
        }
        catch
        {
            return $"[{response.GetType().Name}]";
        }
    }

    private string? GetUserId()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;
    }

    private string? GetUserInfo()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId();
            var userName = context.User.FindFirst(ClaimTypes.Name)?.Value ??
                          context.User.FindFirst("name")?.Value;
            
            return string.IsNullOrEmpty(userName) ? userId : $"{userName} ({userId})";
        }
        
        return null;
    }

    private string? GetClientIpAddress()
    {
        var context = _httpContextAccessor?.HttpContext;
        return context?.Connection?.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor?.HttpContext?.Request?.Headers?["User-Agent"].ToString();
    }
}