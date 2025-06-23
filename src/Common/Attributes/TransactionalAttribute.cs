using System.Data;

namespace Common.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TransactionalAttribute : Attribute
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public int Timeout { get; set; } = 30; // seconds
    public bool ReadOnly { get; set; } = false;
    public string? Name { get; set; }

    public TransactionalAttribute()
    {
    }

    public TransactionalAttribute(IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
    }

    public TransactionalAttribute(IsolationLevel isolationLevel, int timeout)
    {
        IsolationLevel = isolationLevel;
        Timeout = timeout;
    }
}