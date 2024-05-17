namespace Tinyhand.Integrality;

public class IntegralityContext<T> : IntegralityContext
    where T : IStructualObject
{
    public IntegralityContext()
    {
    }

    public int RetryCount { get; init; } = 3;

    public int MaxLength { get; init; } = 4 * 1024 * 1024;
}

public abstract class IntegralityContext
{
}
