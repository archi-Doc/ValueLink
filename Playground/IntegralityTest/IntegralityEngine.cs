using System.Collections;
using Arc.Collections;

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

    public void Integrate(T obj, BytePool.RentMemory difference, out BytePool.RentMemory integration)
    {
        integration = default;
    }

    public void Differentiate(T obj, BytePool.RentMemory integration, out BytePool.RentMemory difference)
    {
        difference = default;
    }
}
