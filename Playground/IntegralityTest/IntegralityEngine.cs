using Arc.Unit;

namespace Tinyhand.Integrality;

public class IntegralityEngine<T>
    where T : IStructualObject
{// Integrate/Differentiate
    private static IntegralityContext<T> defaultContext;

    static IntegralityEngine()
    {
        defaultContext = new();
    }

    public IntegralityEngine()
    {
        this.Context = defaultContext;
    }

    public IntegralityContext<T> Context { get; set; }

    public int Level { get; private set; }

    public void Integrate(T obj, ByteArrayPool.MemoryOwner difference, out ByteArrayPool.MemoryOwner integration)
    {
        integration = default;
    }

    public void Differentiate(T obj, ByteArrayPool.MemoryOwner integration, out ByteArrayPool.MemoryOwner difference)
    {
        difference = default;
    }
}
