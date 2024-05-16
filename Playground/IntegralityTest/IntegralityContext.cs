namespace Tinyhand.Integrality;

public record class IntegralityContext
{
    public IntegralityContext()
    {
    }

    public int DefaultRetryCount { get; init; } = 3;
}
